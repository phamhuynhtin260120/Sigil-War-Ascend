namespace SigilWarAscend.Gameplay
{
	public sealed partial class SigilWarGameManager
	{
		private void ApplyWorldState(bool force)
		{
			if (force || _visiblePortalState != ArePortalsOpen)
			{
				_visiblePortalState = ArePortalsOpen;
				LogWorld($"Portals {(ArePortalsOpen ? "opened" : "closed")}");
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
				LogWorld($"Core objective {(IsCoreSpawned ? "spawned" : "hidden")}");
				if (CoreObjective != null)
				{
					CoreObjective.SetObjectiveActive(IsCoreSpawned);
				}
			}

			if (force || _visiblePhase != CurrentPhase)
			{
				LogWorld($"Visible phase -> {CurrentPhase}");
				_visiblePhase = CurrentPhase;
			}
		}
	}
}
