using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	public interface ISigilRespawnHandler
	{
		void HandleRespawn(Vector3 position, Quaternion rotation);
	}
}
