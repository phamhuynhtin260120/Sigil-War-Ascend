using System;
using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[Serializable]
	public sealed class SigilWarCharacterDefinition
	{
		public string CharacterId;
		public string DisplayName;
		[Tooltip("Each prefab must exist in Fusion Network Project Config.")]
		public NetworkObject PlayerPrefab;
		[Header("Movement")]
		[Min(0f)] public float WalkSpeedMultiplier = 1f;
		[Min(0f)] public float SprintSpeedMultiplier = 1f;
		[Min(0f)] public float JumpMultiplier = 1f;
		[Min(0f)] public float RotationSpeedMultiplier = 1f;
		[Min(0f)] public float AccelerationMultiplier = 1f;
		[Header("Combat")]
		[Min(0f)] public float AttackDamageMultiplier = 1f;
		[Min(0f)] public float AttackAnimationSpeedMultiplier = 1f;
		[Min(0f)] public float AttackLungeMultiplier = 1f;
		[Min(0f)] public float AttackMoveSpeedMultiplier = 1f;
		[Header("Feedback")]
		public float CameraFieldOfViewOffset;
	}

	[CreateAssetMenu(
		fileName = "SigilWarCharacterRegistry",
		menuName = "Sigil War Ascend/Config/Character Registry")]
	public sealed class SigilWarCharacterRegistry : ScriptableObject
	{
		private const string DefaultResourcePath = "SigilWarCharacterRegistry_Default";
		private static SigilWarCharacterRegistry _cachedDefault;

		public SigilWarCharacterDefinition[] Characters;

		public static SigilWarCharacterRegistry LoadDefault()
		{
			if (_cachedDefault == null)
			{
				_cachedDefault = Resources.Load<SigilWarCharacterRegistry>(DefaultResourcePath);
			}

			return _cachedDefault;
		}

		public SigilWarCharacterDefinition ResolveDefinition(string characterId)
		{
			if (string.IsNullOrWhiteSpace(characterId) || Characters == null)
				return null;

			for (int i = 0; i < Characters.Length; i++)
			{
				SigilWarCharacterDefinition definition = Characters[i];
				if (definition == null)
					continue;

				if (string.Equals(definition.CharacterId, characterId, StringComparison.Ordinal))
				{
					return definition;
				}
			}

			return null;
		}

		public NetworkObject ResolvePlayerPrefab(string characterId, NetworkObject fallbackPrefab)
		{
			SigilWarCharacterDefinition definition = ResolveDefinition(characterId);
			if (definition != null && definition.PlayerPrefab != null)
				return definition.PlayerPrefab;

			return fallbackPrefab;
		}
	}
}
