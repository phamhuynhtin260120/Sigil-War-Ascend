using Fusion;

namespace SigilWarAscend.Gameplay
{
	public sealed partial class SigilWarGameManager
	{
		private void StartReadyUpPhase()
		{
			CurrentPhase = MatchPhase.None;
			PhaseTimer = default;
			Winner = PlayerRef.None;
			VictoryReason = VictoryType.None;
			CurrentCoreHolder = PlayerRef.None;
			CoreControlTimer = default;
			ArePortalsOpen = false;
			IsCoreSpawned = false;
			IsReadyUpActive = true;
			ReadyPlayerMask = 0;
			LogPhase("Enter ReadyUp | waiting for all players to confirm");
			NotifyEncounterPhaseStarted(CurrentPhase);
		}

		private void ProcessReadyUpState()
		{
			if (IsReadyUpActive == false)
				return;

			int activeMask = BuildActivePlayerMask();
			ReadyPlayerMask &= activeMask;

			if (_activePlayerBuffer.Count == 0)
				return;

			if (activeMask != 0 && ReadyPlayerMask == activeMask)
			{
				IsReadyUpActive = false;
				LogPhase($"All players ready | count={_activePlayerBuffer.Count}");
				StartPreparationPhase();
			}
		}

		private void ApplyPlayerReadyState(PlayerRef playerRef, bool isReady)
		{
			int bit = GetReadyBit(playerRef);
			if (bit == 0)
				return;

			if (isReady)
			{
				ReadyPlayerMask |= bit;
				LogPhase($"Player ready | player={FormatPlayer(playerRef)} | ready={CountReadyPlayers()}/{_activePlayerBuffer.Count}");
			}
			else
			{
				ReadyPlayerMask &= ~bit;
				LogPhase($"Player unready | player={FormatPlayer(playerRef)} | ready={CountReadyPlayers()}/{_activePlayerBuffer.Count}");
			}
		}

		private int BuildActivePlayerMask()
		{
			int mask = 0;
			for (int i = 0; i < _activePlayerBuffer.Count; i++)
			{
				mask |= GetReadyBit(_activePlayerBuffer[i]);
			}

			return mask;
		}

		private int CountReadyPlayers()
		{
			int count = 0;
			for (int i = 0; i < _activePlayerBuffer.Count; i++)
			{
				if (IsPlayerReady(_activePlayerBuffer[i]))
				{
					count++;
				}
			}

			return count;
		}

		private static int GetReadyBit(PlayerRef playerRef)
		{
			int bitIndex = playerRef.PlayerId - 1;
			if (bitIndex < 0 || bitIndex >= 30)
				return 0;

			return 1 << bitIndex;
		}
	}
}
