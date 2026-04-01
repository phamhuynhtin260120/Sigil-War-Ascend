using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerCameraController : MonoBehaviour
	{
		public SigilWarPlayer Player;

		private Camera _resolvedMainCamera;
		private float _localAttackFeedbackTimer;
		private float _localHitFeedbackTimer;

		private void Reset()
		{
			Player = GetComponent<SigilWarPlayer>();
		}

		public void ForceRefreshCamera()
		{
			_resolvedMainCamera = Camera.main;
			if (_resolvedMainCamera == null)
			{
				_resolvedMainCamera = FindFirstObjectByType<Camera>();
			}
		}

		public void NotifyAttackStarted()
		{
			if (Player == null || Player.IsLocalPlayer == false)
				return;

			_localAttackFeedbackTimer = Mathf.Max(_localAttackFeedbackTimer, Player.AttackFeedbackDuration);
		}

		public void NotifyHit()
		{
			if (Player == null || Player.IsLocalPlayer == false)
				return;

			_localHitFeedbackTimer = Mathf.Max(_localHitFeedbackTimer, Player.HitFeedbackDuration);
		}

		public void NotifyPickup()
		{
			if (Player == null || Player.IsLocalPlayer == false)
				return;

			_localAttackFeedbackTimer = Mathf.Max(_localAttackFeedbackTimer, Player.AttackFeedbackDuration * 0.6f);
		}

		public void ResetFeedback()
		{
			_localAttackFeedbackTimer = 0f;
			_localHitFeedbackTimer = 0f;
		}

		public void TickLateUpdate()
		{
			if (Player == null || Player.IsLocalPlayer == false || Player.CanOwnLocalCamera() == false || Player.PlayerInput == null)
				return;

			if (_resolvedMainCamera == null || _resolvedMainCamera.isActiveAndEnabled == false)
			{
				ForceRefreshCamera();
			}

			if (Player.CameraPivot != null)
			{
				Player.CameraPivot.rotation = Quaternion.Euler(Player.PlayerInput.CurrentInput.LookRotation);
			}

			if (_resolvedMainCamera != null && Player.CameraHandle != null)
			{
				_resolvedMainCamera.transform.SetPositionAndRotation(Player.CameraHandle.position, Player.CameraHandle.rotation);
			}

			RefreshCameraFieldOfView();
		}

		private void RefreshCameraFieldOfView()
		{
			if (_resolvedMainCamera == null || Player == null)
				return;

			float deltaTime = Time.deltaTime;
			_localAttackFeedbackTimer = Mathf.Max(0f, _localAttackFeedbackTimer - deltaTime);
			_localHitFeedbackTimer = Mathf.Max(0f, _localHitFeedbackTimer - deltaTime);

			float targetFieldOfView = Player.BaseFieldOfView;
			if (Player.CharacterStats != null)
			{
				targetFieldOfView += Player.CharacterStats.CameraFieldOfViewOffset;
			}

			if (Player.PlayerInput != null && Player.PlayerInput.CurrentInput.Sprint && Player.PlayerInput.CurrentInput.MoveDirection.sqrMagnitude > 0.001f)
			{
				targetFieldOfView += Player.SprintFieldOfViewBoost;
			}

			if (_localAttackFeedbackTimer > 0f)
			{
				float normalized = Mathf.Clamp01(_localAttackFeedbackTimer / Mathf.Max(Player.AttackFeedbackDuration, 0.001f));
				targetFieldOfView += Player.AttackFieldOfViewBoost * normalized;
			}

			if (_localHitFeedbackTimer > 0f)
			{
				float normalized = Mathf.Clamp01(_localHitFeedbackTimer / Mathf.Max(Player.HitFeedbackDuration, 0.001f));
				targetFieldOfView -= Player.HitFieldOfViewPenalty * normalized;
			}

			_resolvedMainCamera.fieldOfView = Mathf.Lerp(
				_resolvedMainCamera.fieldOfView,
				targetFieldOfView,
				Player.FieldOfViewLerpSpeed * deltaTime);
		}
	}
}
