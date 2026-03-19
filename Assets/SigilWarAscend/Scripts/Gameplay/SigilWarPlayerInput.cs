using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Local input accumulator for the Sigil War player.
	/// Mirrors the Fusion sample approach so input is stable across network ticks.
	/// </summary>
	public sealed class SigilWarPlayerInput : MonoBehaviour
	{
		public float InitialLookRotation = 18f;

		public SigilWarGameplayInput CurrentInput => _input;

		private SigilWarGameplayInput _input;

		public void ResetInput()
		{
			_input.MoveDirection = default;
			_input.Jump = false;
			_input.Sprint = false;
		}

		private void Start()
		{
			_input.LookRotation = new Vector2(InitialLookRotation, 0f);
		}

		private void Update()
		{
			if (Cursor.lockState != CursorLockMode.Locked)
				return;

			var lookRotationDelta = new Vector2(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"));
			_input.LookRotation = ClampLookRotation(_input.LookRotation + lookRotationDelta);

			var moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
			_input.MoveDirection = moveDirection.normalized;

			_input.Jump |= Input.GetButtonDown("Jump");
			_input.Sprint |= Input.GetButton("Sprint");
		}

		private static Vector2 ClampLookRotation(Vector2 lookRotation)
		{
			lookRotation.x = Mathf.Clamp(lookRotation.x, -30f, 70f);
			return lookRotation;
		}
	}
}
