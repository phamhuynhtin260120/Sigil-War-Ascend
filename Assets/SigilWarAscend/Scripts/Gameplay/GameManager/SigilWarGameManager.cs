using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Match director for Sigil War: Ascend.
	/// It owns match phases, timed world events, player spawns and win conditions.
	/// </summary>
	public sealed partial class SigilWarGameManager : NetworkBehaviour
	{
		private const float DefaultPreparationDuration = 10f;
		private const float DefaultLanePhaseDuration = 120f;
		private const float DefaultPortalPhaseDuration = 90f;
		private const float DefaultCorePhaseDuration = 120f;
		private const float DefaultCoreControlDuration = 20f;
		private const float DefaultRespawnDelay = 5f;
		private const bool DefaultAllowRespawnBeforeCorePhase = true;
		private const bool DefaultAllowRespawnDuringCorePhase = false;

		[Header("Config")]
		[SerializeField] private SigilWarMatchRulesConfig _matchRulesConfig;
		public SigilWarGameplayTextConfig GameplayTextConfig;

		[Header("Players")]
		public NetworkObject PlayerPrefab;
		public SigilWarCharacterRegistry CharacterRegistry;
		public LaneSpawnGroup[] LaneSpawnGroups;

		[Header("World")]
		public Portal[] Portals;
		public CoreObjective CoreObjective;

		[Header("Encounters")]
		public EnemySpawnController EnemySpawnController;
		public BossSpawnController BossSpawnController;
		public ItemSpawnController ItemSpawnController;

		[Header("Fallback Spawn")]
		public Transform DefaultSpawnPoint;

		[Networked]
		public MatchPhase CurrentPhase { get; set; }
		[Networked]
		public TickTimer PhaseTimer { get; set; }
		[Networked]
		public NetworkBool ArePortalsOpen { get; set; }
		[Networked]
		public NetworkBool IsCoreSpawned { get; set; }
		[Networked]
		public PlayerRef Winner { get; set; }
		[Networked]
		public VictoryType VictoryReason { get; set; }
		[Networked]
		public PlayerRef CurrentCoreHolder { get; set; }
		[Networked]
		public TickTimer CoreControlTimer { get; set; }
		[Networked]
		public NetworkBool IsReadyUpActive { get; set; }
		[Networked]
		public int ReadyPlayerMask { get; set; }

		private readonly Dictionary<PlayerRef, TickTimer> _pendingRespawns = new Dictionary<PlayerRef, TickTimer>();
		private readonly Dictionary<PlayerRef, int> _spawnCounters = new Dictionary<PlayerRef, int>();
		private readonly HashSet<PlayerRef> _deadPlayers = new HashSet<PlayerRef>();
		private readonly List<PlayerRef> _activePlayerBuffer = new List<PlayerRef>();
		private readonly List<PlayerRef> _expiredRespawnBuffer = new List<PlayerRef>();

		private MatchPhase _visiblePhase;
		private bool _visiblePortalState;
		private bool _visibleCoreState;

		public SigilWarMatchRulesConfig MatchRulesConfig => ResolveMatchRulesConfig();
		public float PreparationDuration => MatchRulesConfig != null ? MatchRulesConfig.PreparationDuration : DefaultPreparationDuration;
		public float LanePhaseDuration => MatchRulesConfig != null ? MatchRulesConfig.LanePhaseDuration : DefaultLanePhaseDuration;
		public float PortalPhaseDuration => MatchRulesConfig != null ? MatchRulesConfig.PortalPhaseDuration : DefaultPortalPhaseDuration;
		public float CorePhaseDuration => MatchRulesConfig != null ? MatchRulesConfig.CorePhaseDuration : DefaultCorePhaseDuration;
		public float CoreControlDuration => MatchRulesConfig != null ? MatchRulesConfig.CoreControlDuration : DefaultCoreControlDuration;
		public float RespawnDelay => MatchRulesConfig != null ? MatchRulesConfig.RespawnDelay : DefaultRespawnDelay;
		public bool AllowRespawnBeforeCorePhase => MatchRulesConfig != null ? MatchRulesConfig.AllowRespawnBeforeCorePhase : DefaultAllowRespawnBeforeCorePhase;
		public bool AllowRespawnDuringCorePhase => MatchRulesConfig != null ? MatchRulesConfig.AllowRespawnDuringCorePhase : DefaultAllowRespawnDuringCorePhase;
		public float RemainingPhaseTime => PhaseTimer.RemainingTime(Runner) ?? 0f;
		public float RemainingCoreControlTime => CoreControlTimer.RemainingTime(Runner) ?? 0f;
		public int ReadyPlayerCount => CountReadyPlayers();
		public int ActivePlayerCount => _activePlayerBuffer.Count;
		public bool IsRespawnAllowedInCurrentPhase => CanRespawnInCurrentPhase();
		public string ResolvedReadyUpInstructions => ResolveReadyUpInstructions();

		public string ResolveReadyUpInstructions()
		{
			SigilWarGameplayTextConfig config = GameplayTextConfig != null ? GameplayTextConfig : SigilWarGameplayTextConfig.LoadDefault();
			if (config != null && string.IsNullOrWhiteSpace(config.ReadyUpInstructions) == false)
			{
				return config.ReadyUpInstructions;
			}

			return string.Empty;
		}

		private SigilWarMatchRulesConfig ResolveMatchRulesConfig()
		{
			if (_matchRulesConfig != null)
			{
				return _matchRulesConfig;
			}

			_matchRulesConfig = SigilWarMatchRulesConfig.LoadDefault();
			return _matchRulesConfig;
		}

		public override void Spawned()
		{
			BindSpawnControllers();
			ApplyWorldState(force: true);
			LogWorld($"Spawned | phase={CurrentPhase}, portalsOpen={ArePortalsOpen}, coreSpawned={IsCoreSpawned}");

			if (HasStateAuthority && CurrentPhase == MatchPhase.None && IsReadyUpActive == false)
			{
				StartReadyUpPhase();
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (HasStateAuthority)
			{
				RefreshActivePlayers();
				ProcessReadyUpState();
				ProcessPendingRespawns();

				if (CurrentPhase != MatchPhase.MatchEnded && IsReadyUpActive == false)
				{
					ProcessPhaseFlow();
					ProcessEncounterSpawners();
					EvaluateCoreVictory();
					EvaluateLastSurvivorVictory();
				}
			}
		}

		public override void Render()
		{
			ApplyWorldState(force: false);
		}

		public LaneType GetAssignedLane(PlayerRef playerRef)
		{
			if (LaneSpawnGroups == null || LaneSpawnGroups.Length == 0)
				return LaneType.Mid;

			int laneIndex = Mathf.Abs(playerRef.PlayerId - 1) % LaneSpawnGroups.Length;
			return LaneSpawnGroups[laneIndex].Lane;
		}

		public Vector3 GetSpawnPosition(PlayerRef playerRef)
		{
			var spawnGroup = GetLaneSpawnGroup(playerRef);
			if (spawnGroup != null && spawnGroup.SpawnPoints != null && spawnGroup.SpawnPoints.Length > 0)
			{
				int counter = GetSpawnCounter(playerRef);
				int index = Mathf.Abs(playerRef.PlayerId - 1 + counter) % spawnGroup.SpawnPoints.Length;
				var spawnPoint = spawnGroup.SpawnPoints[index];
				if (spawnPoint != null)
				{
					return spawnPoint.position;
				}
			}

			return DefaultSpawnPoint != null ? DefaultSpawnPoint.position : transform.position;
		}

		public Quaternion GetSpawnRotation(PlayerRef playerRef)
		{
			var spawnGroup = GetLaneSpawnGroup(playerRef);
			if (spawnGroup != null && spawnGroup.SpawnPoints != null && spawnGroup.SpawnPoints.Length > 0)
			{
				int counter = GetSpawnCounter(playerRef);
				int index = Mathf.Abs(playerRef.PlayerId - 1 + counter) % spawnGroup.SpawnPoints.Length;
				var spawnPoint = spawnGroup.SpawnPoints[index];
				if (spawnPoint != null)
				{
					return spawnPoint.rotation;
				}
			}

			return DefaultSpawnPoint != null ? DefaultSpawnPoint.rotation : Quaternion.identity;
		}

		public void RequestRespawn(PlayerRef playerRef)
		{
			if (playerRef == PlayerRef.None)
				return;

			LogRespawn($"Request respawn for {FormatPlayer(playerRef)} | authority={HasStateAuthority}");

			if (HasStateAuthority)
			{
				ScheduleRespawn(playerRef);
			}
			else
			{
				RPC_RequestRespawn(playerRef);
			}
		}

		public void NotifyPlayerDied(PlayerRef victim, PlayerRef killer = default)
		{
			if (victim == PlayerRef.None || CurrentPhase == MatchPhase.MatchEnded)
				return;

			if (HasStateAuthority == false)
			{
				RPC_NotifyPlayerDied(victim, killer);
				return;
			}

			_deadPlayers.Add(victim);
			LogRespawn($"Player died | victim={FormatPlayer(victim)}, killer={FormatPlayer(killer)}, phase={CurrentPhase}");

			if (CanRespawnInCurrentPhase())
			{
				ScheduleRespawn(victim);
			}
		}

		public void NotifyCoreHolderChanged(PlayerRef holder)
		{
			if (HasStateAuthority == false)
			{
				RPC_NotifyCoreHolderChanged(holder);
				return;
			}

			if (CurrentPhase != MatchPhase.CorePhase || Winner != PlayerRef.None)
				return;

			if (holder == PlayerRef.None)
			{
				CurrentCoreHolder = PlayerRef.None;
				CoreControlTimer = default;
				LogWorld("Core holder cleared");
				return;
			}

			if (CurrentCoreHolder == holder && CoreControlTimer.IsRunning)
				return;

			CurrentCoreHolder = holder;
			CoreControlTimer = TickTimer.CreateFromSeconds(Runner, CoreControlDuration);
			LogWorld($"Core holder set to {FormatPlayer(holder)} | controlDuration={FormatTime(CoreControlDuration)}");
		}

		public bool IsPlayerReady(PlayerRef playerRef)
		{
			if (playerRef == PlayerRef.None)
				return false;

			int bit = GetReadyBit(playerRef);
			return bit != 0 && (ReadyPlayerMask & bit) != 0;
		}

		public void SetPlayerReady(PlayerRef playerRef, bool isReady)
		{
			if (playerRef == PlayerRef.None)
				return;

			if (HasStateAuthority)
			{
				ApplyPlayerReadyState(playerRef, isReady);
			}
			else
			{
				RPC_SetPlayerReady(playerRef, isReady);
			}
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_RequestRespawn(PlayerRef playerRef)
		{
			ScheduleRespawn(playerRef);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_NotifyPlayerDied(PlayerRef victim, PlayerRef killer = default)
		{
			NotifyPlayerDied(victim, killer);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_NotifyCoreHolderChanged(PlayerRef holder)
		{
			NotifyCoreHolderChanged(holder);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_SetPlayerReady(PlayerRef playerRef, NetworkBool isReady)
		{
			ApplyPlayerReadyState(playerRef, isReady);
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
		private void RPC_RespawnPlayer([RpcTarget] PlayerRef playerRef, Vector3 position, Vector3 rotationEuler)
		{
			var playerObject = Runner.GetPlayerObject(playerRef);
			if (playerObject == null)
				return;

			var behaviours = playerObject.GetComponentsInChildren<MonoBehaviour>(true);
			for (int i = 0; i < behaviours.Length; i++)
			{
				if (behaviours[i] is ISigilRespawnHandler respawnHandler)
				{
					respawnHandler.HandleRespawn(position, Quaternion.Euler(rotationEuler));
					return;
				}
			}

			var kcc = playerObject.GetComponentInChildren<SimpleKCC>();
			if (kcc != null)
			{
				kcc.SetPosition(position);
				kcc.SetLookRotation(rotationEuler);
			}
			else
			{
				playerObject.transform.SetPositionAndRotation(position, Quaternion.Euler(rotationEuler));
			}
		}

		private void SpawnLocalPlayer()
		{
			var position = GetSpawnPosition(Runner.LocalPlayer);
			var rotation = GetSpawnRotation(Runner.LocalPlayer);
			string selectedCharacterId = SigilWarAscend.UI.SigilWarSessionData.LaunchData.SelectedCharacterId;
			NetworkObject playerPrefab = CharacterRegistry != null
				? CharacterRegistry.ResolvePlayerPrefab(selectedCharacterId, PlayerPrefab)
				: PlayerPrefab;
			var playerObject = Runner.Spawn(playerPrefab, position, rotation, Runner.LocalPlayer);

			if (playerObject != null)
			{
				SigilWarPlayer player = playerObject.GetComponent<SigilWarPlayer>();
				if (player != null)
				{
					player.SelectedCharacterId = selectedCharacterId;
				}
			}

			Runner.SetPlayerObject(Runner.LocalPlayer, playerObject);
			LogRespawn($"Spawn local player {FormatPlayer(Runner.LocalPlayer)} at {position} | character={selectedCharacterId}");
		}

		public bool EnsureLocalPlayerSpawned()
		{
			if (Runner == null || PlayerPrefab == null)
				return false;

			if (Runner.GetPlayerObject(Runner.LocalPlayer) != null)
				return true;

			SpawnLocalPlayer();
			return true;
		}

		private void BindSpawnControllers()
		{
			if (EnemySpawnController == null)
			{
				EnemySpawnController = GetComponentInChildren<EnemySpawnController>(true);
			}

			if (BossSpawnController == null)
			{
				BossSpawnController = GetComponentInChildren<BossSpawnController>(true);
			}

			if (ItemSpawnController == null)
			{
				ItemSpawnController = GetComponentInChildren<ItemSpawnController>(true);
			}

			if (EnemySpawnController != null)
			{
				EnemySpawnController.Initialize(this);
			}

			if (BossSpawnController != null)
			{
				BossSpawnController.Initialize(this);
			}

			if (ItemSpawnController != null)
			{
				ItemSpawnController.Initialize(this);
			}
		}
	}
}
