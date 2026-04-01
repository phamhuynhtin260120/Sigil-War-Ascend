using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Attach this to a player prefab when you want a movement/respawn-only test character.
	/// It keeps the existing SigilWarPlayer flow, but disables all attack logic.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerNoAttackTestMode : MonoBehaviour
	{
		private SigilWarPlayer _player;

		private void Awake()
		{
			ApplyNoAttackMode();
		}

		private void OnEnable()
		{
			ApplyNoAttackMode();
		}

		private void ApplyNoAttackMode()
		{
			if (_player == null)
			{
				_player = GetComponent<SigilWarPlayer>();
			}

			if (_player == null)
				return;

			_player.DisableCombatForTesting = true;

			if (_player.Combat != null)
			{
				_player.Combat.enabled = false;
			}
		}
	}
}
