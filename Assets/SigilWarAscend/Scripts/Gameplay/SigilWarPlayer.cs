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
		public Transform ScalingRoot;
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

		private Vector3 _moveVelocity;
		private SigilWarGameManager _gameManager;
		private bool _deathReported;

		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFreeFall;
		private int _animIDMotionSpeed;

		public PlayerRef OwnerPlayerRef => Object != null ? Object.StateAuthority : PlayerRef.None;

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

			if (ScalingRoot != null)
			{
				ScalingRoot.localScale = Vector3.one;
			}
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
			if (HasStateAuthority)
			{
				_gameManager = FindObjectOfType<SigilWarGameManager>();
				Nickname = PlayerPrefs.GetString(SigilWarPlayerPrefsKeys.PlayerName);

				if (string.IsNullOrEmpty(Nickname))
				{
					Nickname = "Player" + Random.Range(10000, 100000);
				}
			}

			OnNicknameChanged();
		}

		public override void FixedUpdateNetwork()
		{
			if (_gameManager == null)
			{
				_gameManager = FindObjectOfType<SigilWarGameManager>();
			}

			if (Health != null && Health.IsAlive && KCC != null && KCC.Position.y < FallDeathY)
			{
				Health.TakeHit(Health.NetworkHealth > 0 ? Health.NetworkHealth : 999, PlayerRef.None);
			}

			if (HasStateAuthority && Health != null)
			{
				ProcessDeathState();
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

			if (ScalingRoot != null)
			{
				ScalingRoot.localScale = Vector3.Lerp(ScalingRoot.localScale, Vector3.one, Time.deltaTime * 8f);
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
			AssignAnimationIDs();
		}

		private void LateUpdate()
		{
			if (HasStateAuthority == false)
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

			float speed = input.Sprint ? SprintSpeed : WalkSpeed;

			var lookRotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
			var moveDirection = lookRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
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

			KCC.Move(_moveVelocity, jumpImpulse);
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
			if (HasStateAuthority == false)
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
		}

		private void OnJumpingChanged()
		{
			if (IsJumping)
			{
				if (JumpAudioClip != null && KCC != null)
				{
					AudioSource.PlayClipAtPoint(JumpAudioClip, KCC.Position, 1f);
				}

				if (ScalingRoot != null)
				{
					ScalingRoot.localScale = new Vector3(0.5f, 1.5f, 0.5f);
				}
			}
			else
			{
				if (LandAudioClip != null && KCC != null)
				{
					AudioSource.PlayClipAtPoint(LandAudioClip, KCC.Position, 1f);
				}

				if (ScalingRoot != null)
				{
					ScalingRoot.localScale = new Vector3(1.25f, 0.75f, 1.25f);
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

			if (HasStateAuthority)
			{
				Nameplate.gameObject.SetActive(false);
				return;
			}

			Nameplate.gameObject.SetActive(true);
			Nameplate.SetNickname(Nickname);
		}
	}
}
