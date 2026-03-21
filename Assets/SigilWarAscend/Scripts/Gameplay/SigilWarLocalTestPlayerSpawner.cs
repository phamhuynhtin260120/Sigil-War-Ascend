using System;
using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// One player prefab option for local testing. Tick <see cref="UseForLocalTest"/> to pick that prefab;
	/// otherwise <see cref="SigilWarLocalTestPlayerSpawner.LocalTestPlayerPrefabIndex"/> selects the slot.
	/// </summary>
	[Serializable]
	public sealed class SigilWarPlayerPrefabSlot
	{
		public NetworkObject Prefab;
		[Tooltip("If enabled, this prefab wins for the resolved pick. Only one slot should be enabled; first in list wins if several are.")]
		public bool UseForLocalTest;
	}

	/// <summary>
	/// Pure helper to choose a <see cref="NetworkObject"/> from slots, index, or fallback — no Unity lifecycle.
	/// </summary>
	public static class SigilWarPlayerPrefabResolver
	{
		public static NetworkObject Resolve(
			SigilWarPlayerPrefabSlot[] slots,
			int localTestPlayerPrefabIndex,
			NetworkObject fallbackPrefab)
		{
			if (slots != null && slots.Length > 0)
			{
				for (int i = 0; i < slots.Length; i++)
				{
					var slot = slots[i];
					if (slot != null && slot.UseForLocalTest && slot.Prefab != null)
						return slot.Prefab;
				}

				int idx = Mathf.Clamp(localTestPlayerPrefabIndex, 0, slots.Length - 1);
				var atIndex = slots[idx];
				if (atIndex != null && atIndex.Prefab != null)
					return atIndex.Prefab;
			}

			return fallbackPrefab;
		}
	}

	/// <summary>
	/// Assigns <see cref="SigilWarGameManager.PlayerPrefab"/> at runtime before the manager spawns the local player.
	/// Add this next to <see cref="SigilWarGameManager"/> and wire references; does not modify game manager code.
	/// </summary>
	[DefaultExecutionOrder(-500)]
	public sealed class SigilWarLocalTestPlayerSpawner : MonoBehaviour
	{
		[Header("Target")]
		[Tooltip("Manager whose PlayerPrefab will be overwritten when Apply On Awake is enabled.")]
		public SigilWarGameManager GameManager;

		[Header("Pick prefab")]
		[Tooltip("Each prefab must be listed in Fusion Network Project → Prefabs.")]
		public SigilWarPlayerPrefabSlot[] PlayerPrefabSlots;
		[Tooltip("Used when no slot has Use For Local Test. 0 = first element.")]
		public int LocalTestPlayerPrefabIndex;
		[Tooltip("If false, this component does nothing (GameManager keeps its inspector PlayerPrefab).")]
		public bool ApplyOnAwake = true;

		private void Awake()
		{
			if (ApplyOnAwake == false || GameManager == null)
				return;

			var resolved = SigilWarPlayerPrefabResolver.Resolve(
				PlayerPrefabSlots,
				LocalTestPlayerPrefabIndex,
				GameManager.PlayerPrefab);

			if (resolved != null)
				GameManager.PlayerPrefab = resolved;
		}
	}
}
