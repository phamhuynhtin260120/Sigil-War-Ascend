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

		private void Awake()
		{
			InitializeVfxState();
		}

		public void PlayAttackVfxSlot(int slot)
		{
			if (slot < 0 || slot >= AttackVfxSlots.Length)
			{
				Debug.Log($"[SigilWarPlayerVfx] PlayAttackVfxSlot SKIP: slot={slot} invalid (range 0-{AttackVfxSlots.Length - 1}), time={Time.time:F3}");
				return;
			}

			string slotName = AttackVfxSlots[slot].Name;
			Debug.Log($"[SigilWarPlayerVfx] PlayAttackVfxSlot: slot={slot}, name=\"{slotName}\", time={Time.time:F3}");
			PlaySlot(AttackVfxSlots[slot]);
		}

		public void PlayCurrentAttackVfxSlot()
		{
			if (Player == null || Player.Combat == null)
			{
				Debug.Log($"[SigilWarPlayerVfx] PlayCurrentAttackVfxSlot SKIP: Player={Player != null}, Combat={Player?.Combat != null}, time={Time.time:F3}");
				return;
			}

			int attackStage = Player.AttackStageValue;
			int slot = Player.Combat.GetVfxSlotForStage(attackStage);
			Debug.Log($"[SigilWarPlayerVfx] PlayCurrentAttackVfxSlot: AttackStage={attackStage} -> slot={slot}, time={Time.time:F3}");
			PlayAttackVfxSlot(slot);
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

		private void InitializeVfxState()
		{
			if (VfxRoot == null)
			{
				Transform child = transform.Find("VFX");
				VfxRoot = child != null ? child : transform;
			}

			Debug.Log($"[SigilWarPlayerVfx] InitializeVfxState: VfxRoot={VfxRoot?.name}, slots={AttackVfxSlots?.Length ?? 0}, time={Time.time:F3}");

			var particleSystems = VfxRoot.GetComponentsInChildren<ParticleSystem>(true);
			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem target = particleSystems[i];
				if (target == null)
					continue;

				var main = target.main;
				main.playOnAwake = false;
				target.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}

			for (int i = 0; i < AttackVfxSlots.Length; i++)
			{
				SigilWarVfxSlot slot = AttackVfxSlots[i];
				if (slot.Objects == null)
					continue;

				for (int j = 0; j < slot.Objects.Length; j++)
				{
					GameObject target = slot.Objects[j];
					if (target != null)
					{
						target.SetActive(false);
					}
				}
			}
		}

		private void PlaySlot(SigilWarVfxSlot slot)
		{
			int objectsCount = slot.Objects != null ? slot.Objects.Length : 0;
			int particlesCount = slot.ParticleSystems != null ? slot.ParticleSystems.Length : 0;
			Debug.Log($"[SigilWarPlayerVfx] PlaySlot: name=\"{slot.Name}\", objects={objectsCount}, particleSystems={particlesCount}, time={Time.time:F3}");

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
