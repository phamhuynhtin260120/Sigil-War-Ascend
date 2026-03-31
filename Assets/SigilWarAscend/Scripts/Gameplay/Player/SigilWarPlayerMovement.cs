using Fusion.Addons.SimpleKCC;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerMovement : MonoBehaviour
	{
		[Header("Movement Setup")]
		public float WalkSpeed = 2f;
		public float SprintSpeed = 5f;
		public float JumpImpulse = 10f;
		public float UpGravity = 25f;
		public float DownGravity = 40f;
		public float RotationSpeed = 8f;
		public float FallDeathY = -15f;

		[Header("Movement Accelerations")]
		public float GroundAcceleration = 55f;
		public float GroundDeceleration = 25f;
		public float AirAcceleration = 25f;
		public float AirDeceleration = 1.3f;

		private Vector3 _moveVelocity;

		public void ResetState()
		{
			_moveVelocity = Vector3.zero;
		}

		public void Tick(SigilWarPlayer player, SigilWarGameplayInput input)
		{
			SimpleKCC kcc = player.KCC;
			if (kcc == null)
				return;

			float jumpImpulse = 0f;
			if (player.Health != null && player.Health.IsAlive && kcc.IsGrounded && input.Jump)
			{
				jumpImpulse = JumpImpulse;
				player.IsJumpingValue = true;
			}

			kcc.SetGravity(kcc.RealVelocity.y >= 0f ? UpGravity : DownGravity);

			float speed = input.Sprint ? SprintSpeed : WalkSpeed;
			Quaternion lookRotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
			Vector3 moveDirection = lookRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
			Vector3 desiredMoveVelocity = moveDirection * speed;

			bool canAttack = kcc.IsGrounded;
			if (player.HasStateAuthority && input.Attack && player.Combat != null && canAttack)
			{
				player.Combat.TryStartAttack(player, input);
			}

			float acceleration;
			if (player.IsAttackActive)
			{
				desiredMoveVelocity = Vector3.zero;
			}

			if (desiredMoveVelocity == Vector3.zero)
			{
				acceleration = kcc.IsGrounded ? GroundDeceleration : AirDeceleration;
			}
			else
			{
				if (player.IsAttackActive == false)
				{
					Quaternion currentRotation = kcc.TransformRotation;
					Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
					Quaternion nextRotation = Quaternion.Lerp(currentRotation, targetRotation, RotationSpeed * player.Runner.DeltaTime);
					kcc.SetLookRotation(nextRotation.eulerAngles);
				}

				acceleration = kcc.IsGrounded ? GroundAcceleration : AirAcceleration;
			}

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * player.Runner.DeltaTime);
			if (kcc.ProjectOnGround(_moveVelocity, out Vector3 projectedVector))
			{
				_moveVelocity = projectedVector;
			}

			Vector3 attackVelocity = player.Combat != null ? player.Combat.GetAttackVelocity(player) : Vector3.zero;
			kcc.Move(_moveVelocity + attackVelocity, jumpImpulse);
		}
	}
}
