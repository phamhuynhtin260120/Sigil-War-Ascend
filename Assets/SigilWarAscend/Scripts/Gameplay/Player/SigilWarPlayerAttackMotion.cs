using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerAttackMotion : MonoBehaviour
	{
		public SigilWarPlayer Player;

		private void Reset()
		{
			Player = GetComponent<SigilWarPlayer>();
		}

		public float GetAttackMoveSpeedMultiplier(SigilWarAttackStage attackStage)
		{
			if (Player == null || Player.IsAttackActive == false)
				return 0f;

			return Player.ResolveAttackMoveSpeedMultiplier(attackStage.MoveSpeedMultiplier);
		}

		public Vector3 GetAttackVelocity(SigilWarAttackStage attackStage)
		{
			if (Player == null || Player.IsAttackActive == false || Player.AttackDirectionValue == Vector3.zero)
				return Vector3.zero;

			float animationDuration = Player.ResolveAttackAnimationDuration(attackStage.AnimationDuration);
			float lungeDistance = Player.ResolveAttackLungeDistance(attackStage.LungeDistance);
			if (animationDuration <= 0f || lungeDistance <= 0f)
				return Vector3.zero;

			if (Player.AttackStageTimerValue.IsRunning == false || Player.AttackStageTimerValue.Expired(Player.Runner))
				return Vector3.zero;

			float elapsed = GetStageElapsedTime(attackStage, Player.AttackStageTimerValue, Player.Runner);
			if (elapsed < attackStage.LungeStartTime || elapsed > attackStage.LungeEndTime)
				return Vector3.zero;

			float lungeDuration = Mathf.Max(0.01f, attackStage.LungeEndTime - attackStage.LungeStartTime);
			return Player.AttackDirectionValue * (lungeDistance / lungeDuration);
		}

		public float GetStageElapsedTime(SigilWarAttackStage attackStage, TickTimer attackStageTimer, NetworkRunner runner)
		{
			float remainingTime = attackStageTimer.RemainingTime(runner) ?? 0f;
			float animationDuration = Player.ResolveAttackAnimationDuration(attackStage.AnimationDuration);
			return Mathf.Max(0f, animationDuration - remainingTime);
		}
	}
}
