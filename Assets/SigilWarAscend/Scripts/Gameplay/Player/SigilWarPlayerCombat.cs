using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerCombat : MonoBehaviour
	{
		[Header("Attack")]
		public string AttackStage1TriggerParameter = "Attack1";
		public string AttackStage2TriggerParameter = "Attack2";
		public string AttackStage3TriggerParameter = "Attack3";
		public float ComboInputWindow = 0.25f;
		public float AttackCooldown = 0.2f;
		public float AttackStage1Distance = 0.55f;
		public float AttackStage1Duration = 0.35f;
		public float AttackStage2Distance = 0.4f;
		public float AttackStage2Duration = 0.35f;
		public float AttackStage3Distance = 0.5f;
		public float AttackStage3Duration = 0.40f;

		public void ResetState(SigilWarPlayer player)
		{
			player.IsAttackActive = false;
			player.AttackStageValue = 0;
			player.QueuedAttackStageValue = 0;
			player.AttackStageTimerValue = default;
			player.AttackVisualStageValue = 0;
			player.AttackDirectionValue = Vector3.zero;
		}

		public void TryStartAttack(SigilWarPlayer player, SigilWarGameplayInput input)
		{
			if (player.AttackCooldownTimerValue.IsRunning && player.AttackCooldownTimerValue.Expired(player.Runner) == false)
				return;

			if (player.Health == null || player.Health.IsAlive == false)
				return;

			if (player.IsAttackActive)
			{
				QueueNextAttackStage(player);
				return;
			}

			Vector3 direction = Quaternion.Euler(0f, input.LookRotation.y, 0f) * Vector3.forward;
			direction.y = 0f;
			if (direction.sqrMagnitude <= 0.0001f)
			{
				direction = player.transform.forward;
			}

			player.AttackDirectionValue = direction.normalized;

			if (player.KCC != null)
			{
				player.KCC.SetLookRotation(Quaternion.LookRotation(player.AttackDirectionValue).eulerAngles);
			}

			TriggerAttackStage(player, 1);
		}

		public void TickStateAuthority(SigilWarPlayer player)
		{
			if (player.IsAttackActive == false)
				return;

			if (player.AttackStageTimerValue.IsRunning == false)
			{
				ResetState(player);
				return;
			}

			if (player.AttackStageTimerValue.Expired(player.Runner) == false)
				return;

			if (player.QueuedAttackStageValue > player.AttackStageValue && GetAttackStageDuration(player.QueuedAttackStageValue) > 0f)
			{
				TriggerAttackStage(player, player.QueuedAttackStageValue);
				return;
			}

			ResetState(player);
			if (AttackCooldown > 0f)
			{
				player.AttackCooldownTimerValue = TickTimer.CreateFromSeconds(player.Runner, AttackCooldown);
			}
		}

		public Vector3 GetAttackVelocity(SigilWarPlayer player)
		{
			if (player.IsAttackActive == false || player.AttackDirectionValue == Vector3.zero)
				return Vector3.zero;

			float duration = GetAttackStageDuration(player.AttackStageValue);
			if (duration <= 0f)
				return Vector3.zero;

			float distance = GetAttackStageDistance(player.AttackStageValue);
			if (distance <= 0f)
				return Vector3.zero;

			return player.AttackDirectionValue * (distance / duration);
		}

		public void ApplyVisualTrigger(SigilWarPlayer player)
		{
			if (player.Animator == null)
				return;

			if (player.VisibleAttackVisualCounter < player.AttackVisualCounterValue && player.Object != null && player.HasStateAuthority == false)
			{
				string triggerParameter = GetAttackTriggerParameter(player.AttackVisualStageValue);
				if (HasAnimatorParameter(player, triggerParameter, AnimatorControllerParameterType.Trigger))
				{
					player.Animator.SetTrigger(triggerParameter);
				}
			}

			player.VisibleAttackVisualCounter = player.AttackVisualCounterValue;
		}

		private void QueueNextAttackStage(SigilWarPlayer player)
		{
			if (player.AttackStageValue >= 3)
				return;

			float remainingTime = player.AttackStageTimerValue.RemainingTime(player.Runner) ?? 0f;
			if (remainingTime > Mathf.Max(0f, ComboInputWindow))
				return;

			player.QueuedAttackStageValue = player.AttackStageValue + 1;
		}

		private void TriggerAttackStage(SigilWarPlayer player, int stage)
		{
			string triggerParameter = GetAttackTriggerParameter(stage);
			if (player.Animator == null || HasAnimatorParameter(player, triggerParameter, AnimatorControllerParameterType.Trigger) == false)
				return;

			player.Animator.SetTrigger(triggerParameter);
			player.IsAttackActive = true;
			player.AttackStageValue = stage;
			player.AttackStageTimerValue = TickTimer.CreateFromSeconds(player.Runner, GetAttackStageDuration(stage));
			player.AttackVisualStageValue = stage;
			player.AttackVisualCounterValue++;
			player.VisibleAttackVisualCounter = player.AttackVisualCounterValue;
			player.QueuedAttackStageValue = 0;
		}

		private float GetAttackStageDuration(int stage)
		{
			switch (stage)
			{
				case 1: return AttackStage1Duration;
				case 2: return AttackStage2Duration;
				case 3: return AttackStage3Duration;
				default: return 0f;
			}
		}

		private float GetAttackStageDistance(int stage)
		{
			switch (stage)
			{
				case 1: return AttackStage1Distance;
				case 2: return AttackStage2Distance;
				case 3: return AttackStage3Distance;
				default: return 0f;
			}
		}

		private string GetAttackTriggerParameter(int stage)
		{
			switch (stage)
			{
				case 1: return AttackStage1TriggerParameter;
				case 2: return AttackStage2TriggerParameter;
				case 3: return AttackStage3TriggerParameter;
				default: return AttackStage1TriggerParameter;
			}
		}

		private static bool HasAnimatorParameter(SigilWarPlayer player, string parameterName, AnimatorControllerParameterType expectedType)
		{
			if (player.Animator == null || string.IsNullOrEmpty(parameterName))
				return false;

			var parameters = player.Animator.parameters;
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].name == parameterName && parameters[i].type == expectedType)
				{
					return true;
				}
			}

			return false;
		}
	}
}
