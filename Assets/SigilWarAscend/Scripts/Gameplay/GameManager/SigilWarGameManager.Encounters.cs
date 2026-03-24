namespace SigilWarAscend.Gameplay
{
	public sealed partial class SigilWarGameManager
	{
		private void ProcessEncounterSpawners()
		{
			if (EnemySpawnController != null)
			{
				EnemySpawnController.Tick(CurrentPhase);
			}

			if (BossSpawnController != null)
			{
				BossSpawnController.Tick(CurrentPhase);
			}
		}

		private void NotifyEncounterPhaseStarted(MatchPhase phase)
		{
			if (EnemySpawnController != null)
			{
				EnemySpawnController.HandlePhaseStarted(phase);
			}

			if (BossSpawnController != null)
			{
				BossSpawnController.HandlePhaseStarted(phase);
			}
		}
	}
}
