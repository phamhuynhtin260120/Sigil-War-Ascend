using Fusion;

namespace SigilWarAscend.Gameplay
{
	public sealed partial class SigilWarGameManager
	{
		private void EndMatch(PlayerRef winner, VictoryType reason)
		{
			CurrentPhase = MatchPhase.MatchEnded;
			Winner = winner;
			VictoryReason = reason;
			PhaseTimer = default;
			CurrentCoreHolder = PlayerRef.None;
			CoreControlTimer = default;
			_pendingRespawns.Clear();
			LogVictory($"End match | winner={FormatPlayer(winner)}, reason={reason}");
			NotifyEncounterPhaseStarted(CurrentPhase);
		}

		private void EvaluateCoreVictory()
		{
			if (CurrentPhase != MatchPhase.CorePhase)
				return;

			if (CurrentCoreHolder == PlayerRef.None || CoreControlTimer.IsRunning == false)
				return;

			if (CoreControlTimer.Expired(Runner))
			{
				LogVictory($"Core control complete by {FormatPlayer(CurrentCoreHolder)}");
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
				LogVictory($"Last survivor detected: {FormatPlayer(alivePlayer)}");
				EndMatch(alivePlayer, VictoryType.LastSurvivor);
			}
		}
	}
}
