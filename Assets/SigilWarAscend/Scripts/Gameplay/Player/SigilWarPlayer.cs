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
		public SigilWarPlayerCharacterStats CharacterStats;
		public SigilWarPlayerCameraController CameraController;
		public SigilWarPlayerPresentation Presentation;
		public SigilWarPlayerAttackMotion AttackMotion;

		[Header("Testing")]
		public bool DisableCombatForTesting;

		[Header("Sounds")]
		public AudioSource FootstepSound;
		public AudioClip JumpAudioClip;
		public AudioClip LandAudioClip;
		public AudioClip PickupAudioClip;

		[Header("VFX")]
		public ParticleSystem DustParticles;

		[Header("Camera Feel")]
		public float BaseFieldOfView = 60f;
		public float SprintFieldOfViewBoost = 4f;
		public float AttackFieldOfViewBoost = 2.5f;
		public float HitFieldOfViewPenalty = 5f;
		public float FieldOfViewLerpSpeed = 10f;
		public float AttackFeedbackDuration = 0.12f;
		public float HitFeedbackDuration = 0.18f;

		[Networked, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
		public string Nickname { get; set; }
		[Networked, Capacity(32), OnChangedRender(nameof(OnSelectedCharacterChanged))]
		public string SelectedCharacterId { get; set; }
		[Networked, OnChangedRender(nameof(OnCollectedPickupsChanged))]
		public int CollectedPickups { get; set; }
		[Networked]
		public int PlayerKills { get; set; }
		[Networked]
		public int PlayerDeaths { get; set; }
		[Networked]
		private NetworkBool IsWaitingForRespawn { get; set; }
		[Networked]
		private TickTimer RespawnTimer { get; set; }
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

		public PlayerRef OwnerPlayerRef => Object != null ? Object.StateAuthority : PlayerRef.None;
		public bool IsAlive => Health != null && Health.IsAlive;
		public int CurrentHealth => Health != null ? Health.CurrentHealth : 0;
		public int MaxHealth => Health != null ? Health.MaxHealth : 0;
		public float HealthNormalized => Health != null ? Health.HealthNormalized : 0f;
		public bool IsLocalPlayer => IsLocallyControlled;
		public bool IsRespawnPending => IsWaitingForRespawn;
		public float RemainingRespawnTime => Runner != null && IsWaitingForRespawn ? Mathf.Max(0f, RespawnTimer.RemainingTime(Runner) ?? 0f) : 0f;
		internal bool IsLocallyControlled => Object != null && (HasInputAuthority || HasStateAuthority);
		internal bool IsCombatEnabled => DisableCombatForTesting == false && Combat != null;
		internal SigilWarGameManager GameManager => _gameManager;

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
			EnsurePlayerComponents();
		}

		public override void Spawned()
		{
			EnsurePlayerComponents();
			CameraController?.ForceRefreshCamera();

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
			OnSelectedCharacterChanged();
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
				if (IsCombatEnabled)
				{
					Combat.TickStateAuthority(this);
				}
			}

			bool canRunMovement = Health != null && Health.IsAlive;
			SigilWarGameplayInput input = canRunMovement && CanProcessGameplayInput() ? PlayerInput.CurrentInput : default;
			if (canRunMovement)
			{
				Movement?.Tick(this, input);
			}
			else
			{
				Movement?.ResetState();
			}

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
			Presentation?.Render();
		}

		private void LateUpdate()
		{
			if (IsLocallyControlled == false)
				return;

			if (CanOwnCamera() == false)
				return;

			CameraController?.TickLateUpdate();
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
			ClearRespawnCountdown();

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

			ResetTransientGameplayState();
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
			if (IsCombatEnabled)
			{
				Combat.OpenCurrentDamageWindow(this);
			}
		}

		// Animation Event helper. Use this when the impact frame is separate from the VFX frame.
		public void OpenCurrentAttackDamageWindow()
		{
			if (IsCombatEnabled)
			{
				Combat.OpenCurrentDamageWindow(this);
			}
		}

		// Animation Event helper. Closes any active attack hitbox after the damage window ends.
		public void CloseAttackDamageWindow()
		{
			if (IsCombatEnabled)
			{
				Combat.CloseAllDamageWindows();
			}
		}

		// Animation Event helper. Attack stage only ends when the clip says it is finished.
		public void CompleteCurrentAttackAnimation()
		{
			if (Object != null && HasStateAuthority == false)
				return;

			if (IsCombatEnabled)
			{
				Combat.CloseAllDamageWindows();
				Combat.CompleteCurrentAttackStage(this);
			}
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
			ResetGameplayStateOnDeath();

			if (_gameManager != null)
			{
				_gameManager.NotifyPlayerDied(OwnerPlayerRef, Health.LastDamageDealer);
			}

			PlayerDeaths++;

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

		internal float GetWalkSpeedMultiplier()
		{
			return CharacterStats != null ? CharacterStats.GetWalkSpeedMultiplier() : 1f;
		}

		internal float GetSprintSpeedMultiplier()
		{
			return CharacterStats != null ? CharacterStats.GetSprintSpeedMultiplier() : 1f;
		}

		internal float GetJumpMultiplier()
		{
			return CharacterStats != null ? CharacterStats.GetJumpMultiplier() : 1f;
		}

		internal float GetRotationSpeedMultiplier()
		{
			return CharacterStats != null ? CharacterStats.GetRotationSpeedMultiplier() : 1f;
		}

		internal float GetAccelerationMultiplier()
		{
			return CharacterStats != null ? CharacterStats.GetAccelerationMultiplier() : 1f;
		}

		internal int ResolveAttackDamage(int baseDamage)
		{
			return CharacterStats != null ? CharacterStats.ResolveAttackDamage(baseDamage) : Mathf.Max(1, baseDamage);
		}

		internal float ResolveAttackAnimationDuration(float baseDuration)
		{
			return CharacterStats != null ? CharacterStats.ResolveAttackAnimationDuration(baseDuration) : baseDuration;
		}

		internal float ResolveAttackLungeDistance(float baseLungeDistance)
		{
			return CharacterStats != null ? CharacterStats.ResolveAttackLungeDistance(baseLungeDistance) : baseLungeDistance;
		}

		internal float ResolveAttackMoveSpeedMultiplier(float baseMoveSpeedMultiplier)
		{
			return CharacterStats != null ? CharacterStats.ResolveAttackMoveSpeedMultiplier(baseMoveSpeedMultiplier) : Mathf.Max(0f, baseMoveSpeedMultiplier);
		}

		internal void NotifyAttackStarted(int stage)
		{
			CameraController?.NotifyAttackStarted();
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

		internal bool CanOwnLocalCamera()
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

		private bool CanOwnCamera()
		{
			return CanOwnLocalCamera();
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

			if (CharacterStats == null)
			{
				CharacterStats = GetComponent<SigilWarPlayerCharacterStats>();
				if (CharacterStats == null)
				{
					CharacterStats = gameObject.AddComponent<SigilWarPlayerCharacterStats>();
				}
			}

			if (CameraController == null)
			{
				CameraController = GetComponent<SigilWarPlayerCameraController>();
				if (CameraController == null)
				{
					CameraController = gameObject.AddComponent<SigilWarPlayerCameraController>();
				}
			}

			if (Presentation == null)
			{
				Presentation = GetComponent<SigilWarPlayerPresentation>();
				if (Presentation == null)
				{
					Presentation = gameObject.AddComponent<SigilWarPlayerPresentation>();
				}
			}

			if (AttackMotion == null)
			{
				AttackMotion = GetComponent<SigilWarPlayerAttackMotion>();
				if (AttackMotion == null)
				{
					AttackMotion = gameObject.AddComponent<SigilWarPlayerAttackMotion>();
				}
			}

			if (CharacterStats != null && CharacterStats.Player == null)
			{
				CharacterStats.Player = this;
			}

			if (CameraController != null && CameraController.Player == null)
			{
				CameraController.Player = this;
			}

			if (Presentation != null && Presentation.Player == null)
			{
				Presentation.Player = this;
			}

			if (AttackMotion != null && AttackMotion.Player == null)
			{
				AttackMotion.Player = this;
			}
		}

		private void ResetGameplayStateOnDeath()
		{
			IsJumping = false;
			ResetTransientGameplayState();
			CameraController?.ResetFeedback();
		}

		private void ResetTransientGameplayState()
		{
			Movement?.ResetState();
			if (IsCombatEnabled)
			{
				Combat.ResetState(this);
			}
		}

		private void OnAttackVisualChanged()
		{
			if (IsCombatEnabled)
			{
				Combat.ApplyVisualTrigger(this);
			}
		}

		private void OnJumpingChanged()
		{
			Presentation?.OnJumpingChanged();
		}

		private void OnCollectedPickupsChanged()
		{
			Presentation?.OnCollectedPickupsChanged();
		}

		internal void PlayHitReaction()
		{
			Presentation?.PlayHitReaction();
		}

		internal void StartRespawnCountdown(float respawnDelay)
		{
			if (Runner == null || respawnDelay <= 0f)
			{
				ClearRespawnCountdown();
				return;
			}

			IsWaitingForRespawn = true;
			RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);
		}

		internal void ClearRespawnCountdown()
		{
			IsWaitingForRespawn = false;
			RespawnTimer = default;
		}

		private void OnNicknameChanged()
		{
			Presentation?.OnNicknameChanged();
		}

		private void OnSelectedCharacterChanged()
		{
			CharacterStats?.RefreshSelectedCharacter();
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
			CameraController?.ResetFeedback();
			Presentation?.ResetOnRespawn();
		}
	}
}
