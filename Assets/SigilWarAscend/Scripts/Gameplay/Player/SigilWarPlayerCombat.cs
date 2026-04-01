using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[System.Serializable]
	public struct SigilWarAttackStage
	{
		public string TriggerParameter;
		public int Damage;
		public float AnimationDuration;
		public float LungeDistance;
		public float LungeStartTime;
		public float LungeEndTime;
		public float ComboBufferOpenTime;
		public float ComboBufferCloseTime;
		public int VfxSlot;
		public SigilWarAttackHitbox[] Hitboxes;
	}

	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerCombat : MonoBehaviour
	{
		[Header("Attack")]
		public SigilWarAttackStage[] AttackStages =
		{
			new SigilWarAttackStage { TriggerParameter = "Attack1", Damage = 15, AnimationDuration = 1.63f, LungeDistance = 0.55f, LungeStartTime = 0.10f, LungeEndTime = 0.28f, ComboBufferOpenTime = 0.25f, ComboBufferCloseTime = 1.50f, VfxSlot = 0 },
			new SigilWarAttackStage { TriggerParameter = "Attack2", Damage = 20, AnimationDuration = 1.88f, LungeDistance = 0.40f, LungeStartTime = 0.18f, LungeEndTime = 0.40f, ComboBufferOpenTime = 0.30f, ComboBufferCloseTime = 1.75f, VfxSlot = 1 },
			new SigilWarAttackStage { TriggerParameter = "Attack3", Damage = 30, AnimationDuration = 1.87f, LungeDistance = 0.50f, LungeStartTime = 0.12f, LungeEndTime = 0.36f, ComboBufferOpenTime = 0f, ComboBufferCloseTime = 0f, VfxSlot = 2 },
		};
		public float ComboResetDelay = 0.8f;
		[Tooltip("Fallback timeout if an attack animation is missing its end event.")]
		public float AttackAnimationSafetyTimeout = 2.5f;

		public void ResetState(SigilWarPlayer player)
		{
			CloseAllDamageWindows();
			player.IsAttackActive = false;
			player.AttackStageValue = 0;
			player.QueuedAttackStageValue = 0;
			player.AttackStageTimerValue = default;
			player.AttackCooldownTimerValue = default;
			player.AttackVisualStageValue = 0;
			player.AttackDirectionValue = Vector3.zero;
		}

		public void TryStartAttack(SigilWarPlayer player, SigilWarGameplayInput input)
		{
			if (player.Health == null || player.Health.IsAlive == false)
				return;

			TryResetCombo(player);

			if (player.IsAttackActive)
			{
				TryQueueNextAttackStage(player);
				return;
			}

			Vector3 direction = GetAttackDirection(player, input);
			player.AttackDirectionValue = direction;
			player.KCC?.SetLookRotation(Quaternion.LookRotation(direction).eulerAngles);

			int nextStage = GetNextStageFromIdle(player);
			TriggerAttackStage(player, nextStage);
		}

		public void TickStateAuthority(SigilWarPlayer player)
		{
			if (player.IsAttackActive == false)
			{
				TryResetCombo(player);
				return;
			}

			if (player.AttackCooldownTimerValue.IsRunning && player.AttackCooldownTimerValue.Expired(player.Runner))
			{
				FinishAttackStage(player);
			}
		}

		public Vector3 GetAttackVelocity(SigilWarPlayer player)
		{
			if (player.IsAttackActive == false || player.AttackDirectionValue == Vector3.zero)
				return Vector3.zero;

			SigilWarAttackStage attackStage = GetAttackStage(player.AttackStageValue);
			if (attackStage.AnimationDuration <= 0f || attackStage.LungeDistance <= 0f)
				return Vector3.zero;

			if (player.AttackStageTimerValue.IsRunning == false || player.AttackStageTimerValue.Expired(player.Runner))
				return Vector3.zero;

			float elapsed = GetStageElapsedTime(player, attackStage);
			if (elapsed < attackStage.LungeStartTime || elapsed > attackStage.LungeEndTime)
				return Vector3.zero;

			float lungeDuration = Mathf.Max(0.01f, attackStage.LungeEndTime - attackStage.LungeStartTime);
			return player.AttackDirectionValue * (attackStage.LungeDistance / lungeDuration);
		}

		public int GetVfxSlotForStage(int stage)
		{
			return GetAttackStage(stage).VfxSlot;
		}

		public void CompleteCurrentAttackStage(SigilWarPlayer player)
		{
			if (player == null || player.IsAttackActive == false)
				return;

			CloseAllDamageWindows();

			if (player.QueuedAttackStageValue > player.AttackStageValue && player.QueuedAttackStageValue <= AttackStages.Length)
			{
				AdvanceToQueuedAttackStage(player, player.QueuedAttackStageValue);
				return;
			}

			FinishAttackStage(player);
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

		public void OpenCurrentDamageWindow(SigilWarPlayer player)
		{
			if (player == null || player.HasStateAuthority == false || player.IsAttackActive == false)
				return;

			CloseAllDamageWindows();

			SigilWarAttackStage attackStage = GetAttackStage(player.AttackStageValue);
			Debug.Log($"[SigilWarPlayerCombat] OpenDamageWindow | player={player.name}, stage={player.AttackStageValue}, trigger={attackStage.TriggerParameter}, damage={attackStage.Damage}, time={Time.time:F3}");
			if (attackStage.Hitboxes == null)
				return;

			for (int i = 0; i < attackStage.Hitboxes.Length; i++)
			{
				SigilWarAttackHitbox hitbox = attackStage.Hitboxes[i];
				if (hitbox == null)
					continue;

				hitbox.OpenWindow(player, attackStage.Damage);
			}
		}

		public void CloseAllDamageWindows()
		{
			Debug.Log($"[SigilWarPlayerCombat] CloseAllDamageWindows | time={Time.time:F3}");
			for (int i = 0; i < AttackStages.Length; i++)
			{
				SigilWarAttackStage attackStage = AttackStages[i];
				if (attackStage.Hitboxes == null)
					continue;

				for (int j = 0; j < attackStage.Hitboxes.Length; j++)
				{
					SigilWarAttackHitbox hitbox = attackStage.Hitboxes[j];
					if (hitbox != null)
					{
						hitbox.CloseWindow();
					}
				}
			}
		}

		private void TriggerAttackStage(SigilWarPlayer player, int stage)
		{
			ActivateAttackStage(player, stage, triggerAnimator: true, syncVisuals: true);
		}

		private void FinishAttackStage(SigilWarPlayer player)
		{
			CloseAllDamageWindows();
			player.IsAttackActive = false;
			player.AttackStageTimerValue = default;
			player.QueuedAttackStageValue = 0;

			bool shouldResetCombo = player.AttackStageValue >= AttackStages.Length || ComboResetDelay <= 0f;
			if (shouldResetCombo)
			{
				player.AttackStageValue = 0;
				player.AttackCooldownTimerValue = default;
				return;
			}

			player.AttackCooldownTimerValue = TickTimer.CreateFromSeconds(player.Runner, ComboResetDelay);
		}

		private void AdvanceToQueuedAttackStage(SigilWarPlayer player, int stage)
		{
			bool animatorAlreadyBuffered = IsStageAlreadyBuffered(player, stage);
			ActivateAttackStage(player, stage, triggerAnimator: animatorAlreadyBuffered == false, syncVisuals: animatorAlreadyBuffered == false);
		}

		private void TryQueueNextAttackStage(SigilWarPlayer player)
		{
			if (player.AttackStageValue <= 0 || player.AttackStageValue >= AttackStages.Length)
				return;

			SigilWarAttackStage attackStage = GetAttackStage(player.AttackStageValue);
			if (attackStage.ComboBufferCloseTime <= attackStage.ComboBufferOpenTime)
			{
				return;
			}

			if (player.AttackStageTimerValue.IsRunning == false || player.AttackStageTimerValue.Expired(player.Runner))
			{
				return;
			}

			float elapsed = GetStageElapsedTime(player, attackStage);
			if (elapsed < attackStage.ComboBufferOpenTime || elapsed > attackStage.ComboBufferCloseTime)
			{
				return;
			}

			int queuedStage = player.AttackStageValue + 1;
			if (player.QueuedAttackStageValue == queuedStage)
			{
				return;
			}

			player.QueuedAttackStageValue = queuedStage;
			TriggerBufferedStage(player, queuedStage);
		}

		private void TryResetCombo(SigilWarPlayer player)
		{
			if (player.AttackStageValue <= 0)
				return;

			if (player.AttackCooldownTimerValue.IsRunning == false || player.AttackCooldownTimerValue.Expired(player.Runner) == false)
				return;

			player.AttackStageValue = 0;
			player.AttackCooldownTimerValue = default;
			player.QueuedAttackStageValue = 0;
		}

		private int GetNextStageFromIdle(SigilWarPlayer player)
		{
			if (player.AttackStageValue <= 0 || player.AttackStageValue >= AttackStages.Length)
				return 1;

			int nextStage = player.AttackStageValue + 1;
			if (CanContinueComboFromCurrentAnimatorState(player, nextStage))
				return nextStage;

			return 1;
		}

		private static Vector3 GetAttackDirection(SigilWarPlayer player, SigilWarGameplayInput input)
		{
			Vector3 dir = Quaternion.Euler(0f, input.LookRotation.y, 0f) * Vector3.forward;
			dir.y = 0f;
			return (dir.sqrMagnitude > 0.0001f ? dir : player.transform.forward).normalized;
		}

		private static float GetStageElapsedTime(SigilWarPlayer player, SigilWarAttackStage attackStage)
		{
			float remainingTime = player.AttackStageTimerValue.RemainingTime(player.Runner) ?? 0f;
			return Mathf.Max(0f, attackStage.AnimationDuration - remainingTime);
		}

		private void TriggerBufferedStage(SigilWarPlayer player, int stage)
		{
			SigilWarAttackStage attackStage = GetAttackStage(stage);
			if (player.Animator == null || HasAnimatorParameter(player, attackStage.TriggerParameter, AnimatorControllerParameterType.Trigger) == false)
				return;

			player.Animator.SetTrigger(attackStage.TriggerParameter);
			player.AttackVisualStageValue = stage;
			player.AttackVisualCounterValue++;
			player.VisibleAttackVisualCounter = player.AttackVisualCounterValue;
		}

		private void ActivateAttackStage(SigilWarPlayer player, int stage, bool triggerAnimator, bool syncVisuals)
		{
			SigilWarAttackStage attackStage = GetAttackStage(stage);
			if (player.Animator == null || HasAnimatorParameter(player, attackStage.TriggerParameter, AnimatorControllerParameterType.Trigger) == false)
				return;

			if (triggerAnimator)
			{
				player.Animator.SetTrigger(attackStage.TriggerParameter);
			}

			player.IsAttackActive = true;
			player.AttackStageValue = stage;
			player.AttackStageTimerValue = attackStage.AnimationDuration > 0f
				? TickTimer.CreateFromSeconds(player.Runner, attackStage.AnimationDuration)
				: default;
			player.AttackCooldownTimerValue = AttackAnimationSafetyTimeout > 0f
				? TickTimer.CreateFromSeconds(player.Runner, AttackAnimationSafetyTimeout)
				: default;
			player.QueuedAttackStageValue = 0;

			if (syncVisuals)
			{
				player.AttackVisualStageValue = stage;
				player.AttackVisualCounterValue++;
				player.VisibleAttackVisualCounter = player.AttackVisualCounterValue;
			}
		}

		private static bool IsStageAlreadyBuffered(SigilWarPlayer player, int stage)
		{
			return player.AttackVisualStageValue == stage &&
				player.VisibleAttackVisualCounter == player.AttackVisualCounterValue &&
				player.AttackVisualCounterValue > 0;
		}

		private static bool CanContinueComboFromCurrentAnimatorState(SigilWarPlayer player, int nextStage)
		{
			if (nextStage <= 1 || player.Animator == null)
				return true;

			string requiredState = nextStage switch
			{
				2 => "Attack_Combo_01_01_Anim",
				3 => "Attack_Combo_01_02_Anim",
				_ => string.Empty,
			};

			if (string.IsNullOrEmpty(requiredState))
				return true;

			AnimatorStateInfo currentState = player.Animator.GetCurrentAnimatorStateInfo(0);
			if (currentState.IsName(requiredState))
				return true;

			if (player.Animator.IsInTransition(0))
			{
				AnimatorStateInfo nextState = player.Animator.GetNextAnimatorStateInfo(0);
				if (nextState.IsName(requiredState))
					return true;
			}

			return false;
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

			foreach (var p in player.Animator.parameters)
			{
				if (p.name == parameterName && p.type == expectedType)
					return true;
			}
			return false;
		}
	}
}
