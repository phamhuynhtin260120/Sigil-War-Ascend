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

		[Header("Input Forgiveness")]
		public float CoyoteTime = 0.12f;
		public float JumpBufferTime = 0.12f;
		public float AttackBufferTime = 0.18f;

		private Vector3 _moveVelocity;
		private float _coyoteTimer;
		private float _jumpBufferTimer;
		private float _attackBufferTimer;

		public void ResetState()
		{
			_moveVelocity = Vector3.zero;
			_coyoteTimer = 0f;
			_jumpBufferTimer = 0f;
			_attackBufferTimer = 0f;
		}

		public void Tick(SigilWarPlayer player, SigilWarGameplayInput input)
		{
			SimpleKCC kcc = player.KCC;
			if (kcc == null)
				return;

			float deltaTime = player.Runner.DeltaTime;
			UpdateInputGraceTimers(kcc.IsGrounded, input, deltaTime);

			float jumpImpulse = 0f;
			if (player.Health != null && player.Health.IsAlive && CanConsumeBufferedJump())
			{
				jumpImpulse = JumpImpulse * player.GetJumpMultiplier();
				player.IsJumpingValue = true;
				_jumpBufferTimer = 0f;
				_coyoteTimer = 0f;
			}

			kcc.SetGravity(kcc.RealVelocity.y >= 0f ? UpGravity : DownGravity);

			float speed = input.Sprint
				? SprintSpeed * player.GetSprintSpeedMultiplier()
				: WalkSpeed * player.GetWalkSpeedMultiplier();
			Quaternion lookRotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
			Vector3 moveDirection = lookRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
			Vector3 desiredMoveVelocity = moveDirection * speed;

			if (player.HasStateAuthority && player.Combat != null && CanConsumeBufferedAttack(kcc.IsGrounded))
			{
				player.Combat.TryStartAttack(player, input);
				_attackBufferTimer = player.IsAttackActive ? 0f : _attackBufferTimer;
			}

			float acceleration;
			if (player.IsAttackActive)
			{
				float attackMoveSpeedMultiplier = player.Combat != null
					? player.Combat.GetAttackMoveSpeedMultiplier(player)
					: 0f;
				desiredMoveVelocity *= attackMoveSpeedMultiplier;
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
					float rotationSpeed = RotationSpeed * player.GetRotationSpeedMultiplier();
					Quaternion nextRotation = Quaternion.Lerp(currentRotation, targetRotation, rotationSpeed * deltaTime);
					kcc.SetLookRotation(nextRotation.eulerAngles);
				}

				acceleration = kcc.IsGrounded ? GroundAcceleration : AirAcceleration;
			}

			acceleration *= player.GetAccelerationMultiplier();

			_moveVelocity = Vector3.MoveTowards(
				_moveVelocity,
				desiredMoveVelocity,
				acceleration * deltaTime);
			if (kcc.ProjectOnGround(_moveVelocity, out Vector3 projectedVector))
			{
				_moveVelocity = projectedVector;
			}

			Vector3 attackVelocity = player.Combat != null ? player.Combat.GetAttackVelocity(player) : Vector3.zero;
			kcc.Move(_moveVelocity + attackVelocity, jumpImpulse);
		}

		private void UpdateInputGraceTimers(bool isGrounded, SigilWarGameplayInput input, float deltaTime)
		{
			if (isGrounded)
			{
				_coyoteTimer = CoyoteTime;
			}
			else
			{
				_coyoteTimer = Mathf.Max(0f, _coyoteTimer - deltaTime);
			}

			if (input.Jump)
			{
				_jumpBufferTimer = JumpBufferTime;
			}
			else
			{
				_jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - deltaTime);
			}

			if (input.Attack)
			{
				_attackBufferTimer = AttackBufferTime;
			}
			else
			{
				_attackBufferTimer = Mathf.Max(0f, _attackBufferTimer - deltaTime);
			}
		}

		private bool CanConsumeBufferedJump()
		{
			return _jumpBufferTimer > 0f && _coyoteTimer > 0f;
		}

		private bool CanConsumeBufferedAttack(bool isGrounded)
		{
			if (_attackBufferTimer <= 0f)
				return false;

			return isGrounded;
		}
	}
}
