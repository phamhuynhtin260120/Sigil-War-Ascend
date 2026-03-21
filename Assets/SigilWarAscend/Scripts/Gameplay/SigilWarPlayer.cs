using Fusion;
using Fusion.Addons.SimpleKCC;
using SigilWarAscend.UI;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Sigil War player controller built from the Fusion samples:
	/// movement follows ThirdPersonCharacter, while nickname, pickups and respawn hooks
	/// are prepared for Sigil War match rules.
	/// </summary>
	public sealed class SigilWarPlayer : NetworkBehaviour, ISigilRespawnHandler
	{
		[Header("References")]
		public SigilWarHealth Health;
		public SimpleKCC KCC;
		public SigilWarPlayerInput PlayerInput;
		public Animator Animator;
		public Transform CameraPivot;
		public Transform CameraHandle;
		public SigilWarNameplate Nameplate;
		public Collider Hitbox;

		[Header("Movement Setup")]
		public float WalkSpeed = 2f;
		public float SprintSpeed = 5f;
		public float JumpImpulse = 10f;
		public float UpGravity = 25f;
		public float DownGravity = 40f;
		public float RotationSpeed = 8f;
		public float FallDeathY = -15f;

		[Header("Movement Accelerations")]
		public float GroundAcceleration = 55f;
		public float GroundDeceleration = 25f;
		public float AirAcceleration = 25f;
		public float AirDeceleration = 1.3f;

		[Header("Attack Movement")]
		public bool DisableAnimatorRootMotion = true;
		public string AttackTriggerParameter = "Attack";
		public float AttackCooldown = 0.15f;
		public float AttackStage1Distance = 1.2f;
		public float AttackStage1Duration = 0.12f;
		public float AttackStage2Distance = 0.8f;
		public float AttackStage2Duration = 0.10f;
		public float AttackStage3Distance = 1.1f;
		public float AttackStage3Duration = 0.14f;

		[Header("Sounds")]
		public AudioSource FootstepSound;
		public AudioClip JumpAudioClip;
		public AudioClip LandAudioClip;
		public AudioClip PickupAudioClip;

		[Header("VFX")]
		public ParticleSystem DustParticles;

		[Networked, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
		public string Nickname { get; set; }
		[Networked, OnChangedRender(nameof(OnCollectedPickupsChanged))]
		public int CollectedPickups { get; set; }
		[Networked]
		public int PlayerKills { get; set; }
		[Networked, OnChangedRender(nameof(OnJumpingChanged))]
		private NetworkBool IsJumping { get; set; }
		[Networked]
		private NetworkBool IsAttacking { get; set; }
		[Networked]
		private int AttackStage { get; set; }
		[Networked]
		private TickTimer AttackStageTimer { get; set; }
		[Networked]
		private TickTimer AttackCooldownTimer { get; set; }
		[Networked]
		private Vector3 AttackDirection { get; set; }
		[Networked, OnChangedRender(nameof(OnAttackVisualChanged))]
		private int AttackVisualCounter { get; set; }

		private Vector3 _moveVelocity;
		private SigilWarGameManager _gameManager;
		private bool _deathReported;
		private int _visibleAttackVisualCounter;
		private int _animIDAttack;
		private bool _hasAttackParameter;

		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFreeFall;
		private int _animIDMotionSpeed;

		public PlayerRef OwnerPlayerRef => Object != null ? Object.StateAuthority : PlayerRef.None;
		private bool IsLocallyControlled => Object != null && (HasInputAuthority || HasStateAuthority);

		public void HandleRespawn(Vector3 position, Quaternion rotation)
		{
			if (Health != null)
			{
				Health.Revive();
			}

			if (KCC != null)
			{
				KCC.SetPosition(position);
				KCC.SetLookRotation(rotation.eulerAngles);
				KCC.SetActive(true);
			}
			else
			{
				transform.SetPositionAndRotation(position, rotation);
			}

			_moveVelocity = Vector3.zero;
			_deathReported = false;
			IsAttacking = false;
			AttackStage = 0;
			AttackStageTimer = default;
			AttackCooldownTimer = default;
		}

		public void RegisterPickup()
		{
			if (HasStateAuthority == false)
				return;

			CollectedPickups++;
		}

		public void TakeDamage(int damage, PlayerRef damageDealer = default)
		{
			if (Health != null)
			{
				Health.TakeHit(damage, damageDealer);
			}
		}

		public override void Spawned()
		{
			ConfigureAnimator();

			if (IsLocallyControlled)
			{
				_gameManager = FindObjectOfType<SigilWarGameManager>();
				Nickname = PlayerPrefs.GetString(SigilWarPlayerPrefsKeys.PlayerName);

				if (string.IsNullOrEmpty(Nickname))
				{
					Nickname = "Player" + Random.Range(10000, 100000);
				}
			}

			OnNicknameChanged();
			_visibleAttackVisualCounter = AttackVisualCounter;
		}

		public override void FixedUpdateNetwork()
		{
			if (_gameManager == null)
			{
				_gameManager = FindObjectOfType<SigilWarGameManager>();
			}

			if (Health != null && Health.IsAlive && KCC != null && KCC.Position.y < FallDeathY)
			{
				Health.TakeHit(Health.CurrentHealth > 0 ? Health.CurrentHealth : 999, PlayerRef.None);
			}

			if (HasStateAuthority && Health != null)
			{
				ProcessDeathState();
			}

			if (HasStateAuthority)
			{
				ProcessAttackState();
			}

			SigilWarGameplayInput input = CanProcessGameplayInput() ? PlayerInput.CurrentInput : default;
			ProcessInput(input);

			if (KCC != null && KCC.IsGrounded)
			{
				IsJumping = false;
			}

			if (KCC != null && Health != null)
			{
				KCC.SetActive(Health.IsAlive);
			}

			PlayerInput.ResetInput();
		}

		public override void Render()
		{
			if (Animator != null && KCC != null)
			{
				Animator.SetFloat(_animIDSpeed, KCC.RealSpeed, 0.15f, Time.deltaTime);
				Animator.SetFloat(_animIDMotionSpeed, 1f);
				Animator.SetBool(_animIDJump, IsJumping);
				Animator.SetBool(_animIDGrounded, KCC.IsGrounded);
				Animator.SetBool(_animIDFreeFall, KCC.RealVelocity.y < -10f);
			}

			if (FootstepSound != null && KCC != null)
			{
				FootstepSound.enabled = Health != null && Health.IsAlive && KCC.IsGrounded && KCC.RealSpeed > 1f;
				FootstepSound.pitch = KCC.RealSpeed > SprintSpeed - 1f ? 1.5f : 1f;
			}

			if (DustParticles != null && KCC != null)
			{
				var emission = DustParticles.emission;
				emission.enabled = Health != null && Health.IsAlive && KCC.IsGrounded && KCC.RealSpeed > 1f;
			}

			if (Hitbox != null && Health != null)
			{
				Hitbox.enabled = Health.IsAlive;
			}
		}

		private void Awake()
		{
			ConfigureAnimator();
			AssignAnimationIDs();
		}

		private void LateUpdate()
		{
			if (IsLocallyControlled == false)
				return;

			if (CanProcessCamera() == false)
				return;

			if (CameraPivot != null)
			{
				CameraPivot.rotation = Quaternion.Euler(PlayerInput.CurrentInput.LookRotation);
			}

			if (Camera.main != null && CameraHandle != null)
			{
				Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (HasStateAuthority == false)
				return;

			var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
			for (int i = 0; i < behaviours.Length; i++)
			{
				if (behaviours[i] is ISigilCollectible collectible)
				{
					collectible.Collect(this);
					return;
				}
			}
		}

		private void ProcessInput(SigilWarGameplayInput input)
		{
			if (KCC == null)
				return;

			float jumpImpulse = 0f;

			if (Health != null && Health.IsAlive && KCC.IsGrounded && input.Jump)
			{
				jumpImpulse = JumpImpulse;
				IsJumping = true;
			}

			KCC.SetGravity(KCC.RealVelocity.y >= 0f ? UpGravity : DownGravity);

			if (HasStateAuthority && input.Attack)
			{
				TryStartAttack(input);
			}

			float speed = IsAttacking ? 0f : (input.Sprint ? SprintSpeed : WalkSpeed);

			var lookRotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
			var moveDirection = IsAttacking ? Vector3.zero : lookRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
			var desiredMoveVelocity = moveDirection * speed;

			float acceleration;
			if (desiredMoveVelocity == Vector3.zero)
			{
				acceleration = KCC.IsGrounded ? GroundDeceleration : AirDeceleration;
			}
			else
			{
				var currentRotation = KCC.TransformRotation;
				var targetRotation = Quaternion.LookRotation(moveDirection);
				var nextRotation = Quaternion.Lerp(currentRotation, targetRotation, RotationSpeed * Runner.DeltaTime);

				KCC.SetLookRotation(nextRotation.eulerAngles);

				acceleration = KCC.IsGrounded ? GroundAcceleration : AirAcceleration;
			}

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);

			if (KCC.ProjectOnGround(_moveVelocity, out var projectedVector))
			{
				_moveVelocity = projectedVector;
			}

			Vector3 attackVelocity = GetAttackVelocity();
			KCC.Move(_moveVelocity + attackVelocity, jumpImpulse);
		}

		private void ProcessDeathState()
		{
			if (Health.IsAlive)
			{
				_deathReported = false;
				return;
			}

			if (_deathReported)
				return;

			_deathReported = true;

			if (_gameManager != null)
			{
				_gameManager.NotifyPlayerDied(OwnerPlayerRef, Health.LastDamageDealer);
			}

			if (Health.LastDamageDealer != PlayerRef.None && Health.LastDamageDealer != OwnerPlayerRef)
			{
				var killerObject = Runner.GetPlayerObject(Health.LastDamageDealer);
				if (killerObject != null)
				{
					var killerPlayer = killerObject.GetComponent<SigilWarPlayer>();
					if (killerPlayer != null)
					{
						killerPlayer.RPC_AwardKill();
					}
				}
			}
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_AwardKill()
		{
			PlayerKills++;
		}

		private bool CanProcessGameplayInput()
		{
			if (IsLocallyControlled == false)
				return false;

			if (PlayerInput == null || KCC == null || Health == null)
				return false;

			if (Health.IsAlive == false)
				return false;

			if (_gameManager != null && _gameManager.CurrentPhase == MatchPhase.MatchEnded)
				return false;

			return Cursor.lockState == CursorLockMode.Locked;
		}

		private bool CanProcessCamera()
		{
			if (IsLocallyControlled == false)
				return false;

			if (Health == null || Health.IsAlive == false)
				return false;

			if (_gameManager != null && _gameManager.CurrentPhase == MatchPhase.MatchEnded)
				return false;

			return Cursor.lockState == CursorLockMode.Locked;
		}

		private void AssignAnimationIDs()
		{
			if (Animator == null)
				return;

			_animIDSpeed = Animator.StringToHash("Speed");
			_animIDGrounded = Animator.StringToHash("Grounded");
			_animIDJump = Animator.StringToHash("Jump");
			_animIDFreeFall = Animator.StringToHash("FreeFall");
			_animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
			_animIDAttack = Animator.StringToHash(AttackTriggerParameter);
		}

		private void ConfigureAnimator()
		{
			if (Animator == null)
				return;

			if (DisableAnimatorRootMotion)
			{
				Animator.applyRootMotion = false;
			}

			_hasAttackParameter = HasAnimatorParameter(AttackTriggerParameter, AnimatorControllerParameterType.Trigger);
		}

		private void OnJumpingChanged()
		{
			if (IsJumping)
			{
				if (JumpAudioClip != null && KCC != null)
				{
					AudioSource.PlayClipAtPoint(JumpAudioClip, KCC.Position, 1f);
				}
			}
			else
			{
				if (LandAudioClip != null && KCC != null)
				{
					AudioSource.PlayClipAtPoint(LandAudioClip, KCC.Position, 1f);
				}
			}
		}

		private void OnCollectedPickupsChanged()
		{
			if (CollectedPickups <= 0)
				return;

			if (PickupAudioClip != null && KCC != null)
			{
				AudioSource.PlayClipAtPoint(PickupAudioClip, KCC.Position, 1f);
			}
		}

		private void OnNicknameChanged()
		{
			if (Nameplate == null)
				return;

			if (IsLocallyControlled)
			{
				Nameplate.enabled = false;
				return;
			}

			Nameplate.enabled = true;
			Nameplate.SetNickname(Nickname);
		}

		private void TryStartAttack(SigilWarGameplayInput input)
		{
			if (IsAttacking)
				return;

			if (AttackCooldownTimer.IsRunning && AttackCooldownTimer.Expired(Runner) == false)
				return;

			if (Health == null || Health.IsAlive == false)
				return;

			Vector3 direction = Quaternion.Euler(0f, input.LookRotation.y, 0f) * Vector3.forward;
			direction.y = 0f;
			if (direction.sqrMagnitude <= 0.0001f)
			{
				direction = transform.forward;
			}

			AttackDirection = direction.normalized;
			IsAttacking = true;
			AttackStage = 1;
			AttackStageTimer = TickTimer.CreateFromSeconds(Runner, AttackStage1Duration);
			AttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, AttackStage1Duration + AttackStage2Duration + AttackStage3Duration + AttackCooldown);
			AttackVisualCounter++;

			if (KCC != null)
			{
				KCC.SetLookRotation(Quaternion.LookRotation(AttackDirection).eulerAngles);
			}
		}

		private void ProcessAttackState()
		{
			if (IsAttacking == false)
				return;

			if (AttackStageTimer.IsRunning == false)
			{
				FinishAttack();
				return;
			}

			if (AttackStageTimer.Expired(Runner) == false)
				return;

			switch (AttackStage)
			{
				case 1:
					AttackStage = 2;
					AttackStageTimer = TickTimer.CreateFromSeconds(Runner, AttackStage2Duration);
					AttackVisualCounter++;
					break;
				case 2:
					AttackStage = 3;
					AttackStageTimer = TickTimer.CreateFromSeconds(Runner, AttackStage3Duration);
					AttackVisualCounter++;
					break;
				default:
					FinishAttack();
					break;
			}
		}

		private Vector3 GetAttackVelocity()
		{
			if (IsAttacking == false || AttackDirection == Vector3.zero)
				return Vector3.zero;

			float duration = GetCurrentAttackStageDuration();
			if (duration <= 0f)
				return Vector3.zero;

			float distance = GetCurrentAttackStageDistance();
			float speed = distance / duration;
			return AttackDirection * speed;
		}

		private float GetCurrentAttackStageDuration()
		{
			switch (AttackStage)
			{
				case 1: return AttackStage1Duration;
				case 2: return AttackStage2Duration;
				case 3: return AttackStage3Duration;
				default: return 0f;
			}
		}

		private float GetCurrentAttackStageDistance()
		{
			switch (AttackStage)
			{
				case 1: return AttackStage1Distance;
				case 2: return AttackStage2Distance;
				case 3: return AttackStage3Distance;
				default: return 0f;
			}
		}

		private void FinishAttack()
		{
			IsAttacking = false;
			AttackStage = 0;
			AttackStageTimer = default;
		}

		private void OnAttackVisualChanged()
		{
			if (Animator == null || _hasAttackParameter == false)
				return;

			if (_visibleAttackVisualCounter < AttackVisualCounter)
			{
				Animator.SetTrigger(_animIDAttack);
			}

			_visibleAttackVisualCounter = AttackVisualCounter;
		}

		private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType expectedType)
		{
			if (Animator == null || string.IsNullOrEmpty(parameterName))
				return false;

			var parameters = Animator.parameters;
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].name == parameterName && parameters[i].type == expectedType)
				{
					return true;
				}
			}

			return false;
		}
	}
}
