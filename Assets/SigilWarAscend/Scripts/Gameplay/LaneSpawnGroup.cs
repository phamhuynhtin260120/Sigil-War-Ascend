using System;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[Serializable]
	public sealed class LaneSpawnGroup
	{
		public LaneType Lane;
		public Transform[] SpawnPoints;
	}
}
