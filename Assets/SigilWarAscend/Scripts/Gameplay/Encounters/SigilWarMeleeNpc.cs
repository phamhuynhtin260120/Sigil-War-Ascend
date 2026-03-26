using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarMeleeNpc : NetworkBehaviour
	{
		[Header("References")]
		public SigilWarDamageableActor DamageableActor;
		public SigilWarHealth Health;
		public Animator Animator;

		[Header("Movement")]
		public float MoveSpeed = 2.5f;
		public float RotationSpeed = 8f;
		public float ChaseRange = 12f;
		public float AttackRange = 2.2f;
		public float StopDistance = 1.4f;

		[Header("Attack")]
		public int AttackDamage = 12;
		public float AttackCooldown = 1.25f;
		public string AttackTriggerParameter = "Attack";

		[Header("Animation")]
		public string SpeedParameter = "Speed";
		public string DeadParameter = "Dead";

		[Header("Debug")]
		public bool EnableDebugLogs = true;

		[Networked]
		private TickTimer AttackCooldownTimer { get; set; }
		[Networked, OnChangedRender(nameof(OnAttackVisualChanged))]
		private int AttackVisualCounter { get; set; }
		[Networked]
		private float NetworkMoveSpeed { get; set; }

		private int _visibleAttackVisualCounter;

		public override void Spawned()
		{
			if (DamageableActor == null)
			{
				DamageableActor = GetComponent<SigilWarDamageableActor>();
			}

			if (Health == null)
			{
				Health = GetComponent<SigilWarHealth>();
			}

			if (Animator == null)
			{
				Animator = GetComponentInChildren<Animator>();
			}

			_visibleAttackVisualCounter = AttackVisualCounter;
		}

		public override void FixedUpdateNetwork()
		{
			if (HasStateAuthority == false || Health == null || Health.IsAlive == false)
			{
				NetworkMoveSpeed = 0f;
				return;
			}

			SigilWarPlayer target = FindClosestLivingPlayer();
			if (target == null || target.Health == null || target.Health.IsAlive == false)
			{
				NetworkMoveSpeed = 0f;
				return;
			}

			Vector3 targetPosition = target.transform.position;
			Vector3 currentPosition = transform.position;
			Vector3 flatDirection = targetPosition - currentPosition;
			flatDirection.y = 0f;

			float distance = flatDirection.magnitude;
			if (distance <= 0.001f)
			{
				NetworkMoveSpeed = 0f;
				return;
			}

			Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);
			transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, RotationSpeed * Runner.DeltaTime);

			if (distance > AttackRange && distance <= ChaseRange)
			{
				Vector3 destination = targetPosition - flatDirection.normalized * StopDistance;
				Vector3 nextPosition = Vector3.MoveTowards(currentPosition, destination, MoveSpeed * Runner.DeltaTime);
				transform.position = nextPosition;
				NetworkMoveSpeed = MoveSpeed;
				return;
			}

			NetworkMoveSpeed = 0f;

			if (distance > AttackRange)
				return;

			if (AttackCooldownTimer.IsRunning && AttackCooldownTimer.Expired(Runner) == false)
				return;

			AttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.05f, AttackCooldown));
			AttackVisualCounter++;
			target.TakeDamage(AttackDamage, PlayerRef.None);

			if (EnableDebugLogs)
			{
				Debug.Log($"[SigilWarMeleeNpc] Attack | npc={name}, target={target.name}, damage={AttackDamage}, time={Time.time:F3}");
			}
		}

		public override void Render()
		{
			if (Animator == null)
				return;

			if (HasAnimatorParameter(SpeedParameter, AnimatorControllerParameterType.Float))
			{
				Animator.SetFloat(SpeedParameter, NetworkMoveSpeed);
			}

			if (HasAnimatorParameter(DeadParameter, AnimatorControllerParameterType.Bool))
			{
				Animator.SetBool(DeadParameter, Health != null && Health.IsAlive == false);
			}
		}

		private void OnAttackVisualChanged()
		{
			if (Animator == null)
				return;

			if (_visibleAttackVisualCounter < AttackVisualCounter && HasAnimatorParameter(AttackTriggerParameter, AnimatorControllerParameterType.Trigger))
			{
				Animator.SetTrigger(AttackTriggerParameter);
			}

			_visibleAttackVisualCounter = AttackVisualCounter;
		}

		private SigilWarPlayer FindClosestLivingPlayer()
		{
			if (Runner == null)
				return null;

			SigilWarPlayer bestPlayer = null;
			float bestDistance = float.MaxValue;

			foreach (PlayerRef playerRef in Runner.ActivePlayers)
			{
				NetworkObject playerObject = Runner.GetPlayerObject(playerRef);
				if (playerObject == null)
					continue;

				SigilWarPlayer player = playerObject.GetComponent<SigilWarPlayer>();
				if (player == null || player.Health == null || player.Health.IsAlive == false)
					continue;

				float distance = Vector3.Distance(transform.position, player.transform.position);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestPlayer = player;
				}
			}

			return bestPlayer;
		}

		private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType expectedType)
		{
			if (Animator == null || string.IsNullOrEmpty(parameterName))
				return false;

			foreach (AnimatorControllerParameter parameter in Animator.parameters)
			{
				if (parameter.name == parameterName && parameter.type == expectedType)
					return true;
			}

			return false;
		}
	}
}
