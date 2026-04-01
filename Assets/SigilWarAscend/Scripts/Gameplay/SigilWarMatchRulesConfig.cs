using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[CreateAssetMenu(
		fileName = "SigilWarMatchRulesConfig",
		menuName = "Sigil War Ascend/Config/Match Rules Config")]
	public sealed class SigilWarMatchRulesConfig : ScriptableObject
	{
		private const string DefaultResourcePath = "SigilWarMatchRulesConfig";

		private static SigilWarMatchRulesConfig _cachedDefault;

		[Header("Match Durations")]
		public float PreparationDuration = 10f;
		public float LanePhaseDuration = 120f;
		public float PortalPhaseDuration = 90f;
		public float CorePhaseDuration = 120f;
		public float CoreControlDuration = 20f;

		[Header("Respawn Delays")]
		public float PreparationRespawnDelay = 10f;
		public float LanePhaseRespawnDelay = 15f;
		public float PortalPhaseRespawnDelay = 20f;
		public float CorePhaseRespawnDelay = 5f;

		[Header("Respawn Rules")]
		public bool AllowRespawnBeforeCorePhase = true;
		public bool AllowRespawnDuringCorePhase = false;

		public static SigilWarMatchRulesConfig LoadDefault()
		{
			if (_cachedDefault == null)
			{
				_cachedDefault = Resources.Load<SigilWarMatchRulesConfig>(DefaultResourcePath);
			}

			return _cachedDefault;
		}
	}
}
