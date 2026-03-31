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
	}

	[CreateAssetMenu(
		fileName = "SigilWarCharacterRegistry",
		menuName = "Sigil War Ascend/Config/Character Registry")]
	public sealed class SigilWarCharacterRegistry : ScriptableObject
	{
		public SigilWarCharacterDefinition[] Characters;

		public NetworkObject ResolvePlayerPrefab(string characterId, NetworkObject fallbackPrefab)
		{
			if (string.IsNullOrWhiteSpace(characterId) == false && Characters != null)
			{
				for (int i = 0; i < Characters.Length; i++)
				{
					SigilWarCharacterDefinition definition = Characters[i];
					if (definition == null || definition.PlayerPrefab == null)
						continue;

					if (string.Equals(definition.CharacterId, characterId, StringComparison.Ordinal))
					{
						return definition.PlayerPrefab;
					}
				}
			}

			return fallbackPrefab;
		}
	}
}
