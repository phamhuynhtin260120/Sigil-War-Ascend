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
	public sealed class SigilWarGameManager : NetworkBehaviour
	{
		[Header("Match Durations")]
		public float PreparationDuration = 10f;
		public float LanePhaseDuration = 120f;
		public float PortalPhaseDuration = 90f;
		public float CorePhaseDuration = 120f;
		public float CoreControlDuration = 20f;
		public float RespawnDelay = 5f;

		[Header("Rules")]
		public bool AllowRespawnBeforeCorePhase = true;
		public bool AllowRespawnDuringCorePhase = false;

		[Header("Players")]
		public NetworkObject PlayerPrefab;
		public LaneSpawnGroup[] LaneSpawnGroups;

		[Header("World")]
		public Portal[] Portals;
		public CoreObjective CoreObjective;

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

		private readonly Dictionary<PlayerRef, TickTimer> _pendingRespawns = new Dictionary<PlayerRef, TickTimer>();
		private readonly Dictionary<PlayerRef, int> _spawnCounters = new Dictionary<PlayerRef, int>();
		private readonly HashSet<PlayerRef> _deadPlayers = new HashSet<PlayerRef>();
		private readonly List<PlayerRef> _activePlayerBuffer = new List<PlayerRef>();
		private readonly List<PlayerRef> _expiredRespawnBuffer = new List<PlayerRef>();

		private MatchPhase _visiblePhase;
		private bool _visiblePortalState;
		private bool _visibleCoreState;

		public float RemainingPhaseTime => PhaseTimer.RemainingTime(Runner) ?? 0f;
		public float RemainingCoreControlTime => CoreControlTimer.RemainingTime(Runner) ?? 0f;

		public override void Spawned()
		{
			ApplyWorldState(force: true);

			if (PlayerPrefab != null && Runner.GetPlayerObject(Runner.LocalPlayer) == null)
			{
				SpawnLocalPlayer();
			}

			if (HasStateAuthority && CurrentPhase == MatchPhase.None)
			{
				StartPreparationPhase();
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (HasStateAuthority)
			{
				RefreshActivePlayers();
				ProcessPendingRespawns();

				if (CurrentPhase != MatchPhase.MatchEnded)
				{
					ProcessPhaseFlow();
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
				return;
			}

			if (CurrentCoreHolder == holder && CoreControlTimer.IsRunning)
				return;

			CurrentCoreHolder = holder;
			CoreControlTimer = TickTimer.CreateFromSeconds(Runner, CoreControlDuration);
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

		private void SpawnLocalPlayer()
		{
			var position = GetSpawnPosition(Runner.LocalPlayer);
			var rotation = GetSpawnRotation(Runner.LocalPlayer);
			var playerObject = Runner.Spawn(PlayerPrefab, position, rotation, Runner.LocalPlayer);

			Runner.SetPlayerObject(Runner.LocalPlayer, playerObject);
		}

		private void RefreshActivePlayers()
		{
			_activePlayerBuffer.Clear();
			foreach (var playerRef in Runner.ActivePlayers)
			{
				_activePlayerBuffer.Add(playerRef);
			}

			for (int i = _activePlayerBuffer.Count - 1; i >= 0; i--)
			{
				var playerRef = _activePlayerBuffer[i];
				if (Runner.GetPlayerObject(playerRef) == null)
					continue;

				if (_spawnCounters.ContainsKey(playerRef) == false)
				{
					_spawnCounters.Add(playerRef, 0);
				}
			}

			_deadPlayers.RemoveWhere(player => _activePlayerBuffer.Contains(player) == false);
		}

		private void ProcessPhaseFlow()
		{
			if (PhaseTimer.IsRunning == false || PhaseTimer.Expired(Runner) == false)
				return;

			switch (CurrentPhase)
			{
				case MatchPhase.Preparation:
					StartLanePhase();
					break;
				case MatchPhase.LanePhase:
					StartPortalPhase();
					break;
				case MatchPhase.PortalPhase:
					StartCorePhase();
					break;
				case MatchPhase.CorePhase:
					EndMatch(PlayerRef.None, VictoryType.TimeOut);
					break;
			}
		}

		private void ProcessPendingRespawns()
		{
			if (_pendingRespawns.Count == 0)
				return;

			_expiredRespawnBuffer.Clear();
			foreach (var pair in _pendingRespawns)
			{
				if (pair.Value.Expired(Runner))
				{
					_expiredRespawnBuffer.Add(pair.Key);
				}
			}

			for (int i = 0; i < _expiredRespawnBuffer.Count; i++)
			{
				RespawnPlayer(_expiredRespawnBuffer[i]);
				_pendingRespawns.Remove(_expiredRespawnBuffer[i]);
			}
		}

		private void StartPreparationPhase()
		{
			CurrentPhase = MatchPhase.Preparation;
			PhaseTimer = TickTimer.CreateFromSeconds(Runner, PreparationDuration);
			Winner = PlayerRef.None;
			VictoryReason = VictoryType.None;
			CurrentCoreHolder = PlayerRef.None;
			CoreControlTimer = default;
			ArePortalsOpen = false;
			IsCoreSpawned = false;
		}

		private void StartLanePhase()
		{
			CurrentPhase = MatchPhase.LanePhase;
			PhaseTimer = TickTimer.CreateFromSeconds(Runner, LanePhaseDuration);
			ArePortalsOpen = false;
			IsCoreSpawned = false;
		}

		private void StartPortalPhase()
		{
			CurrentPhase = MatchPhase.PortalPhase;
			PhaseTimer = TickTimer.CreateFromSeconds(Runner, PortalPhaseDuration);
			ArePortalsOpen = true;
		}

		private void StartCorePhase()
		{
			CurrentPhase = MatchPhase.CorePhase;
			PhaseTimer = TickTimer.CreateFromSeconds(Runner, CorePhaseDuration);
			ArePortalsOpen = true;
			IsCoreSpawned = true;
			CurrentCoreHolder = PlayerRef.None;
			CoreControlTimer = default;
		}

		private void EndMatch(PlayerRef winner, VictoryType reason)
		{
			CurrentPhase = MatchPhase.MatchEnded;
			Winner = winner;
			VictoryReason = reason;
			PhaseTimer = default;
			CurrentCoreHolder = PlayerRef.None;
			CoreControlTimer = default;
			_pendingRespawns.Clear();
		}

		private void EvaluateCoreVictory()
		{
			if (CurrentPhase != MatchPhase.CorePhase)
				return;

			if (CurrentCoreHolder == PlayerRef.None || CoreControlTimer.IsRunning == false)
				return;

			if (CoreControlTimer.Expired(Runner))
			{
				EndMatch(CurrentCoreHolder, VictoryType.CoreControl);
			}
		}

		private void EvaluateLastSurvivorVictory()
		{
			if (CurrentPhase != MatchPhase.CorePhase)
				return;

			if (AllowRespawnDuringCorePhase)
				return;

			int aliveCount = 0;
			PlayerRef alivePlayer = PlayerRef.None;

			for (int i = 0; i < _activePlayerBuffer.Count; i++)
			{
				var playerRef = _activePlayerBuffer[i];
				if (Runner.GetPlayerObject(playerRef) == null)
					continue;

				if (_deadPlayers.Contains(playerRef))
					continue;

				aliveCount++;
				alivePlayer = playerRef;
			}

			if (aliveCount == 1)
			{
				EndMatch(alivePlayer, VictoryType.LastSurvivor);
			}
		}

		private void ScheduleRespawn(PlayerRef playerRef)
		{
			if (CanRespawnInCurrentPhase() == false)
				return;

			_pendingRespawns[playerRef] = TickTimer.CreateFromSeconds(Runner, RespawnDelay);
		}

		private void RespawnPlayer(PlayerRef playerRef)
		{
			var playerObject = Runner.GetPlayerObject(playerRef);
			if (playerObject == null)
				return;

			_spawnCounters[playerRef] = GetSpawnCounter(playerRef) + 1;

			Vector3 position = GetSpawnPosition(playerRef);
			Quaternion rotation = GetSpawnRotation(playerRef);

			var behaviours = playerObject.GetComponentsInChildren<MonoBehaviour>(true);
			for (int i = 0; i < behaviours.Length; i++)
			{
				if (behaviours[i] is ISigilRespawnHandler respawnHandler)
				{
					respawnHandler.HandleRespawn(position, rotation);
					_deadPlayers.Remove(playerRef);
					return;
				}
			}

			var kcc = playerObject.GetComponentInChildren<SimpleKCC>();
			if (kcc != null)
			{
				kcc.SetPosition(position);
				kcc.SetLookRotation(rotation.eulerAngles);
			}
			else
			{
				playerObject.transform.SetPositionAndRotation(position, rotation);
			}

			_deadPlayers.Remove(playerRef);
		}

		private LaneSpawnGroup GetLaneSpawnGroup(PlayerRef playerRef)
		{
			if (LaneSpawnGroups == null || LaneSpawnGroups.Length == 0)
				return null;

			LaneType lane = GetAssignedLane(playerRef);
			for (int i = 0; i < LaneSpawnGroups.Length; i++)
			{
				if (LaneSpawnGroups[i] != null && LaneSpawnGroups[i].Lane == lane)
				{
					return LaneSpawnGroups[i];
				}
			}

			return LaneSpawnGroups[0];
		}

		private int GetSpawnCounter(PlayerRef playerRef)
		{
			return _spawnCounters.TryGetValue(playerRef, out int counter) ? counter : 0;
		}

		private bool CanRespawnInCurrentPhase()
		{
			switch (CurrentPhase)
			{
				case MatchPhase.Preparation:
				case MatchPhase.LanePhase:
				case MatchPhase.PortalPhase:
					return AllowRespawnBeforeCorePhase;
				case MatchPhase.CorePhase:
					return AllowRespawnDuringCorePhase;
				default:
					return false;
			}
		}

		private void ApplyWorldState(bool force)
		{
			if (force || _visiblePortalState != ArePortalsOpen)
			{
				_visiblePortalState = ArePortalsOpen;
				if (Portals != null)
				{
					for (int i = 0; i < Portals.Length; i++)
					{
						if (Portals[i] != null)
						{
							Portals[i].SetOpen(ArePortalsOpen);
						}
					}
				}
			}

			if (force || _visibleCoreState != IsCoreSpawned)
			{
				_visibleCoreState = IsCoreSpawned;
				if (CoreObjective != null)
				{
					CoreObjective.SetObjectiveActive(IsCoreSpawned);
				}
			}

			if (force || _visiblePhase != CurrentPhase)
			{
				_visiblePhase = CurrentPhase;
			}
		}
	}
}
