using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[System.Serializable]
	public struct SigilWarAttackStage
	{
		public string TriggerParameter;
		public float Distance;
		public float Duration;
		public int VfxSlot;
	}

	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerCombat : MonoBehaviour
	{
		[Header("Attack")]
		public SigilWarAttackStage[] AttackStages =
		{
			new SigilWarAttackStage { TriggerParameter = "Attack1", Distance = 0.55f, Duration = 0.35f, VfxSlot = 0 },
			new SigilWarAttackStage { TriggerParameter = "Attack2", Distance = 0.4f, Duration = 0.35f, VfxSlot = 1 },
			new SigilWarAttackStage { TriggerParameter = "Attack3", Distance = 0.5f, Duration = 0.40f, VfxSlot = 2 },
		};
		public float ComboInputWindow = 0.25f;
		public float AttackCooldown = 0.8f;

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

			if (player.QueuedAttackStageValue > player.AttackStageValue && GetAttackStage(player.QueuedAttackStageValue).Duration > 0f)
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

			SigilWarAttackStage attackStage = GetAttackStage(player.AttackStageValue);
			if (attackStage.Duration <= 0f || attackStage.Distance <= 0f)
				return Vector3.zero;

			return player.AttackDirectionValue * (attackStage.Distance / attackStage.Duration);
		}

		public int GetVfxSlotForStage(int stage)
		{
			return GetAttackStage(stage).VfxSlot;
		}

		public void ApplyVisualTrigger(SigilWarPlayer player)
		{
			if (player.Animator == null)
				return;

			if (player.VisibleAttackVisualCounter < player.AttackVisualCounterValue && player.Object != null && player.HasStateAuthority == false)
			{
				string triggerParameter = GetAttackStage(player.AttackVisualStageValue).TriggerParameter;
				if (HasAnimatorParameter(player, triggerParameter, AnimatorControllerParameterType.Trigger))
				{
					player.Animator.SetTrigger(triggerParameter);
				}
			}

			player.VisibleAttackVisualCounter = player.AttackVisualCounterValue;
		}

		private void QueueNextAttackStage(SigilWarPlayer player)
		{
			if (player.AttackStageValue >= AttackStages.Length)
				return;

			float remainingTime = player.AttackStageTimerValue.RemainingTime(player.Runner) ?? 0f;
			if (remainingTime > Mathf.Max(0f, ComboInputWindow))
				return;

			player.QueuedAttackStageValue = player.AttackStageValue + 1;
		}

		private void TriggerAttackStage(SigilWarPlayer player, int stage)
		{
			SigilWarAttackStage attackStage = GetAttackStage(stage);
			if (player.Animator == null || HasAnimatorParameter(player, attackStage.TriggerParameter, AnimatorControllerParameterType.Trigger) == false)
				return;

			player.Animator.SetTrigger(attackStage.TriggerParameter);
			player.IsAttackActive = true;
			player.AttackStageValue = stage;
			player.AttackStageTimerValue = TickTimer.CreateFromSeconds(player.Runner, attackStage.Duration);
			player.AttackVisualStageValue = stage;
			player.AttackVisualCounterValue++;
			player.VisibleAttackVisualCounter = player.AttackVisualCounterValue;
			player.QueuedAttackStageValue = 0;
		}

		private SigilWarAttackStage GetAttackStage(int stage)
		{
			int index = stage - 1;
			return index >= 0 && index < AttackStages.Length ? AttackStages[index] : default;
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
