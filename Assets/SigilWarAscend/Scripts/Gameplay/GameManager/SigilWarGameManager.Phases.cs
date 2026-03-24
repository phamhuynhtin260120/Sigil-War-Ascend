using Fusion;

namespace SigilWarAscend.Gameplay
{
	public sealed partial class SigilWarGameManager
	{
		private void ProcessPhaseFlow()
		{
			if (PhaseTimer.IsRunning == false || PhaseTimer.Expired(Runner) == false)
				return;

			LogPhase($"Phase timer expired for {CurrentPhase}");

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
			LogPhase($"Enter {CurrentPhase} | duration={FormatTime(PreparationDuration)}");
			NotifyEncounterPhaseStarted(CurrentPhase);
		}

		private void StartLanePhase()
		{
			CurrentPhase = MatchPhase.LanePhase;
			PhaseTimer = TickTimer.CreateFromSeconds(Runner, LanePhaseDuration);
			ArePortalsOpen = false;
			IsCoreSpawned = false;
			LogPhase($"Enter {CurrentPhase} | duration={FormatTime(LanePhaseDuration)}");
			NotifyEncounterPhaseStarted(CurrentPhase);
		}

		private void StartPortalPhase()
		{
			CurrentPhase = MatchPhase.PortalPhase;
			PhaseTimer = TickTimer.CreateFromSeconds(Runner, PortalPhaseDuration);
			ArePortalsOpen = true;
			LogPhase($"Enter {CurrentPhase} | duration={FormatTime(PortalPhaseDuration)}");
			NotifyEncounterPhaseStarted(CurrentPhase);
		}

		private void StartCorePhase()
		{
			CurrentPhase = MatchPhase.CorePhase;
			PhaseTimer = TickTimer.CreateFromSeconds(Runner, CorePhaseDuration);
			ArePortalsOpen = true;
			IsCoreSpawned = true;
			CurrentCoreHolder = PlayerRef.None;
			CoreControlTimer = default;
			LogPhase($"Enter {CurrentPhase} | duration={FormatTime(CorePhaseDuration)}");
			NotifyEncounterPhaseStarted(CurrentPhase);
		}
	}
}
