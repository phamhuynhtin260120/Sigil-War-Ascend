using Fusion;
using Fusion.Addons.SimpleKCC;
using SigilWarAscend.UI;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Networked coordinator for the Sigil War player.
	/// Movement and combat live in dedicated components so this class can focus on shared state,
	/// rendering hooks and high-level lifecycle events.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class SigilWarPlayer : NetworkBehaviour, ISigilRespawnHandler, ISigilDamageable
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
		public SigilWarPlayerMovement Movement;
		public SigilWarPlayerCombat Combat;
		public SigilWarPlayerVfx Vfx;

		[Header("Sounds")]
		public AudioSource FootstepSound;
		public AudioClip JumpAudioClip;
		public AudioClip LandAudioClip;
		public AudioClip PickupAudioClip;

		[Header("VFX")]
		public ParticleSystem DustParticles;

		[Networked, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
		public string Nickname { get; set; }
		[Networked, Capacity(32)]
		public string SelectedCharacterId { get; set; }
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
		private int QueuedAttackStage { get; set; }
		[Networked]
		private TickTimer AttackStageTimer { get; set; }
		[Networked]
		private TickTimer AttackCooldownTimer { get; set; }
		[Networked]
		private Vector3 AttackDirection { get; set; }
		[Networked, OnChangedRender(nameof(OnAttackVisualChanged))]
		private int AttackVisualCounter { get; set; }
		[Networked]
		private int AttackVisualStage { get; set; }
		private SigilWarGameManager _gameManager;
		private bool _deathReported;
		private int _visibleAttackVisualCounter;
		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFreeFall;
		private int _animIDMotionSpeed;
		private int _animIDDead;
		private Camera _resolvedMainCamera;

		public PlayerRef OwnerPlayerRef => Object != null ? Object.StateAuthority : PlayerRef.None;
		public bool IsAlive => Health != null && Health.IsAlive;
		public int CurrentHealth => Health != null ? Health.CurrentHealth : 0;
		public int MaxHealth => Health != null ? Health.MaxHealth : 0;
		public float HealthNormalized => Health != null ? Health.HealthNormalized : 0f;
		public bool IsLocalPlayer => IsLocallyControlled;
		internal bool IsLocallyControlled => Object != null && (HasInputAuthority || HasStateAuthority);

		internal bool IsAttackActive
		{
			get => IsAttacking;
			set => IsAttacking = value;
		}

		internal int AttackStageValue
		{
			get => AttackStage;
			set => AttackStage = value;
		}

		internal int QueuedAttackStageValue
		{
			get => QueuedAttackStage;
			set => QueuedAttackStage = value;
		}

		internal TickTimer AttackStageTimerValue
		{
			get => AttackStageTimer;
			set => AttackStageTimer = value;
		}

		internal TickTimer AttackCooldownTimerValue
		{
			get => AttackCooldownTimer;
			set => AttackCooldownTimer = value;
		}

		internal Vector3 AttackDirectionValue
		{
			get => AttackDirection;
			set => AttackDirection = value;
		}

		internal int AttackVisualCounterValue
		{
			get => AttackVisualCounter;
			set => AttackVisualCounter = value;
		}

		internal int AttackVisualStageValue
		{
			get => AttackVisualStage;
			set => AttackVisualStage = value;
		}

		internal int VisibleAttackVisualCounter
		{
			get => _visibleAttackVisualCounter;
			set => _visibleAttackVisualCounter = value;
		}

		internal bool IsJumpingValue
		{
			get => IsJumping;
			set => IsJumping = value;
		}

		private void Awake()
		{
			AssignAnimationIDs();
			EnsurePlayerComponents();
		}

		public override void Spawned()
		{
			EnsurePlayerComponents();
			TryResolveMainCamera(forceRefresh: true);

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

			if (Health != null && Health.IsAlive && KCC != null && Movement != null && KCC.Position.y < Movement.FallDeathY)
			{
				Health.TakeHit(Health.CurrentHealth > 0 ? Health.CurrentHealth : 999, PlayerRef.None);
			}

			if (HasStateAuthority && Health != null)
			{
				ProcessDeathState();
				Combat?.TickStateAuthority(this);
			}

			SigilWarGameplayInput input = CanProcessGameplayInput() ? PlayerInput.CurrentInput : default;
			Movement?.Tick(this, input);

			if (KCC != null && KCC.IsGrounded)
			{
				IsJumping = false;
			}

			if (KCC != null && Health != null)
			{
				KCC.SetActive(Health.IsAlive);
			}

			if (PlayerInput != null)
			{
				PlayerInput.ResetInput();
			}
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
				Animator.SetBool(_animIDDead, Health != null && Health.IsAlive == false);
			}

			if (FootstepSound != null && KCC != null)
			{
				FootstepSound.enabled = Health != null && Health.IsAlive && KCC.IsGrounded && KCC.RealSpeed > 1f;
				float sprintSpeed = Movement != null ? Movement.SprintSpeed : 5f;
				FootstepSound.pitch = KCC.RealSpeed > sprintSpeed - 1f ? 1.5f : 1f;
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

		private void LateUpdate()
		{
			if (IsLocallyControlled == false)
				return;

			if (CanOwnCamera() == false)
				return;

			TryResolveMainCamera(forceRefresh: false);

			if (CameraPivot != null)
			{
				CameraPivot.rotation = Quaternion.Euler(PlayerInput.CurrentInput.LookRotation);
			}

			if (_resolvedMainCamera != null && CameraHandle != null)
			{
				_resolvedMainCamera.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
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

		public void HandleRespawn(Vector3 position, Quaternion rotation)
		{
			EnsurePlayerComponents();

			if (Health != null)
			{
				Health.Revive();
				Health.RefreshPresentation();
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

			Movement?.ResetState();
			Combat?.ResetState(this);
			ResetPresentationStateOnRespawn();
			_deathReported = false;
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

		// Animation Event helper. Prefer PlayCurrentAttackVfx() on the clip when VFX follows the configured stage slot.
		public void PlayAttackVfx(int slot)
		{
			Vfx?.PlayAttackVfxSlot(slot);
		}

		// Animation Event helper. Uses the current combo stage -> VFX slot mapping from combat config.
		public void PlayCurrentAttackVfx()
		{
			Vfx?.PlayCurrentAttackVfxSlot();
		}

		// Animation Event helper. Use this on the exact attack impact frame to play VFX and enable damage hitboxes together.
		public void PlayCurrentAttackVfxAndOpenDamageWindow()
		{
			PlayCurrentAttackVfx();
			Combat?.OpenCurrentDamageWindow(this);
		}

		// Animation Event helper. Use this when the impact frame is separate from the VFX frame.
		public void OpenCurrentAttackDamageWindow()
		{
			Combat?.OpenCurrentDamageWindow(this);
		}

		// Animation Event helper. Closes any active attack hitbox after the damage window ends.
		public void CloseAttackDamageWindow()
		{
			Combat?.CloseAllDamageWindows();
		}

		// Animation Event helper. Attack stage only ends when the clip says it is finished.
		public void CompleteCurrentAttackAnimation()
		{
			if (Object != null && HasStateAuthority == false)
				return;

			Combat?.CloseAllDamageWindows();
			Combat?.CompleteCurrentAttackStage(this);
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

			if (_gameManager != null && _gameManager.IsReadyUpActive)
				return false;

			return Cursor.lockState == CursorLockMode.Locked;
		}

		private bool CanProcessCamera()
		{
			if (CanOwnCamera() == false)
				return false;

			return Cursor.lockState == CursorLockMode.Locked;
		}

		private bool CanOwnCamera()
		{
			if (IsLocallyControlled == false)
				return false;

			if (Health == null || Health.IsAlive == false)
				return false;

			if (_gameManager != null && _gameManager.CurrentPhase == MatchPhase.MatchEnded)
				return false;

			if (_gameManager != null && _gameManager.IsReadyUpActive)
				return false;

			return true;
		}

		private void TryResolveMainCamera(bool forceRefresh)
		{
			if (forceRefresh == false && _resolvedMainCamera != null && _resolvedMainCamera.isActiveAndEnabled)
				return;

			_resolvedMainCamera = Camera.main;
			if (_resolvedMainCamera == null)
			{
				_resolvedMainCamera = FindFirstObjectByType<Camera>();
			}
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
			_animIDDead = Animator.StringToHash("Dead");
		}

		private void EnsurePlayerComponents()
		{
			if (Movement == null)
			{
				Movement = GetComponent<SigilWarPlayerMovement>();
				if (Movement == null)
				{
					Movement = gameObject.AddComponent<SigilWarPlayerMovement>();
				}
			}

			if (Combat == null)
			{
				Combat = GetComponent<SigilWarPlayerCombat>();
				if (Combat == null)
				{
					Combat = gameObject.AddComponent<SigilWarPlayerCombat>();
				}
			}

			if (Vfx == null)
			{
				Vfx = GetComponent<SigilWarPlayerVfx>();
				if (Vfx == null)
				{
					Vfx = gameObject.AddComponent<SigilWarPlayerVfx>();
				}
			}

			if (Vfx != null && Vfx.Player == null)
			{
				Vfx.Player = this;
			}
		}

		private void OnAttackVisualChanged()
		{
			Combat?.ApplyVisualTrigger(this);
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

		private void ResetPresentationStateOnRespawn()
		{
			IsJumpingValue = false;
			IsAttackActive = false;
			AttackStageValue = 0;
			QueuedAttackStageValue = 0;
			AttackStageTimerValue = default;
			AttackCooldownTimerValue = default;
			AttackDirectionValue = Vector3.zero;
			AttackVisualStageValue = 0;
			VisibleAttackVisualCounter = AttackVisualCounterValue;

			if (Hitbox != null)
			{
				Hitbox.enabled = Health != null && Health.IsAlive;
			}

			if (Animator != null)
			{
				Animator.Rebind();
				Animator.Update(0f);
				Animator.SetBool(_animIDDead, false);
				Animator.SetBool(_animIDJump, false);
				Animator.SetBool(_animIDFreeFall, false);
			}

			if (DustParticles != null)
			{
				DustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}

			if (FootstepSound != null)
			{
				FootstepSound.Stop();
			}

			OnNicknameChanged();

			SigilWarHealthBar[] healthBars = GetComponentsInChildren<SigilWarHealthBar>(true);
			for (int i = 0; i < healthBars.Length; i++)
			{
				healthBars[i].RefreshNow();
			}
		}
	}
}
