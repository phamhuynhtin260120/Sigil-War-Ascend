using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerCharacterStats : MonoBehaviour
	{
		public SigilWarPlayer Player;

		private SigilWarCharacterDefinition _resolvedCharacterDefinition;

		public float CameraFieldOfViewOffset => _resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.CameraFieldOfViewOffset : 0f;

		private void Reset()
		{
			Player = GetComponent<SigilWarPlayer>();
		}

		public void RefreshSelectedCharacter()
		{
			if (Player == null)
			{
				Player = GetComponent<SigilWarPlayer>();
			}

			SigilWarCharacterRegistry registry = Player != null && Player.GameManager != null && Player.GameManager.CharacterRegistry != null
				? Player.GameManager.CharacterRegistry
				: SigilWarCharacterRegistry.LoadDefault();
			_resolvedCharacterDefinition = registry != null && Player != null
				? registry.ResolveDefinition(Player.SelectedCharacterId)
				: null;
		}

		public float GetWalkSpeedMultiplier() => GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.WalkSpeedMultiplier : 1f);
		public float GetSprintSpeedMultiplier() => GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.SprintSpeedMultiplier : 1f);
		public float GetJumpMultiplier() => GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.JumpMultiplier : 1f);
		public float GetRotationSpeedMultiplier() => GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.RotationSpeedMultiplier : 1f);
		public float GetAccelerationMultiplier() => GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.AccelerationMultiplier : 1f);

		public int ResolveAttackDamage(int baseDamage)
		{
			float multiplier = GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.AttackDamageMultiplier : 1f);
			return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
		}

		public float ResolveAttackAnimationDuration(float baseDuration)
		{
			float speedMultiplier = GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.AttackAnimationSpeedMultiplier : 1f);
			return baseDuration / speedMultiplier;
		}

		public float ResolveAttackLungeDistance(float baseLungeDistance)
		{
			float multiplier = GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.AttackLungeMultiplier : 1f);
			return baseLungeDistance * multiplier;
		}

		public float ResolveAttackMoveSpeedMultiplier(float baseMoveSpeedMultiplier)
		{
			float multiplier = GetPositiveMultiplier(_resolvedCharacterDefinition != null ? _resolvedCharacterDefinition.AttackMoveSpeedMultiplier : 1f);
			return Mathf.Max(0f, baseMoveSpeedMultiplier * multiplier);
		}

		private static float GetPositiveMultiplier(float value)
		{
			return value > 0f ? value : 1f;
		}
	}
}
