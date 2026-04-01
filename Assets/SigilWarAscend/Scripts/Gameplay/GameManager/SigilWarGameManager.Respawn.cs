using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	public sealed partial class SigilWarGameManager
	{
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
				LogRespawn($"Respawn timer expired for {FormatPlayer(_expiredRespawnBuffer[i])}");
				RespawnPlayer(_expiredRespawnBuffer[i]);
				_pendingRespawns.Remove(_expiredRespawnBuffer[i]);
			}
		}

		private void ScheduleRespawn(PlayerRef playerRef)
		{
			if (CanRespawnInCurrentPhase() == false)
			{
				LogRespawn($"Skip respawn for {FormatPlayer(playerRef)} | phase={CurrentPhase}");
				return;
			}

			float respawnDelay = GetRespawnDelayForCurrentPhase();
			_pendingRespawns[playerRef] = TickTimer.CreateFromSeconds(Runner, respawnDelay);
			var playerObject = Runner.GetPlayerObject(playerRef);
			if (playerObject != null)
			{
				var player = playerObject.GetComponent<SigilWarPlayer>();
				player?.StartRespawnCountdown(respawnDelay);
			}
			LogRespawn($"Schedule respawn for {FormatPlayer(playerRef)} | phase={CurrentPhase} | delay={FormatTime(respawnDelay)}");
		}

		private void RespawnPlayer(PlayerRef playerRef)
		{
			var playerObject = Runner.GetPlayerObject(playerRef);
			if (playerObject == null)
			{
				LogRespawn($"Cannot respawn {FormatPlayer(playerRef)} | player object missing");
				return;
			}

			_spawnCounters[playerRef] = GetSpawnCounter(playerRef) + 1;

			Vector3 position = GetSpawnPosition(playerRef);
			Quaternion rotation = GetSpawnRotation(playerRef);
			RPC_RespawnPlayer(playerRef, position, rotation.eulerAngles);
			_deadPlayers.Remove(playerRef);
			LogRespawn($"Respawn {FormatPlayer(playerRef)} at {position} | spawnCount={_spawnCounters[playerRef]}");
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

		private float GetRespawnDelayForCurrentPhase()
		{
			switch (CurrentPhase)
			{
				case MatchPhase.Preparation:
					return PreparationRespawnDelay;
				case MatchPhase.LanePhase:
					return LanePhaseRespawnDelay;
				case MatchPhase.PortalPhase:
					return PortalPhaseRespawnDelay;
				case MatchPhase.CorePhase:
					return CorePhaseRespawnDelay;
				default:
					return PreparationRespawnDelay;
			}
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
	}
}
