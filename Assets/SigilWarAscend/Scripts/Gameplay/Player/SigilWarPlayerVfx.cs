using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[System.Serializable]
	public struct SigilWarVfxSlot
	{
		public string Name;
		public GameObject[] Objects;
		public ParticleSystem[] ParticleSystems;
	}

	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerVfx : MonoBehaviour
	{
		[Header("References")]
		public SigilWarPlayer Player;
		public Transform VfxRoot;

		[Header("Attack VFX")]
		public SigilWarVfxSlot[] AttackVfxSlots;

		public void PlayAttackVfx(int slot)
		{
			if (slot < 0 || slot >= AttackVfxSlots.Length)
				return;

			PlaySlot(AttackVfxSlots[slot]);
		}

		// Animation Event helper when the clip should use the configured slot of the current combo stage.
		public void PlayCurrentAttackVfx()
		{
			if (Player == null || Player.Combat == null)
				return;

			PlayAttackVfx(Player.Combat.GetVfxSlotForStage(Player.AttackStageValue));
		}

		private void Reset()
		{
			Player = GetComponent<SigilWarPlayer>();
			if (VfxRoot == null)
			{
				Transform child = transform.Find("VFX");
				VfxRoot = child != null ? child : transform;
			}
		}

		private void PlaySlot(SigilWarVfxSlot slot)
		{
			if (slot.Objects != null)
			{
				for (int i = 0; i < slot.Objects.Length; i++)
				{
					GameObject target = slot.Objects[i];
					if (target == null)
						continue;

					target.SetActive(false);
					target.SetActive(true);
				}
			}

			if (slot.ParticleSystems != null)
			{
				for (int i = 0; i < slot.ParticleSystems.Length; i++)
				{
					ParticleSystem target = slot.ParticleSystems[i];
					if (target == null)
						continue;

					target.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
					target.Play(true);
				}
			}
		}
	}
}
