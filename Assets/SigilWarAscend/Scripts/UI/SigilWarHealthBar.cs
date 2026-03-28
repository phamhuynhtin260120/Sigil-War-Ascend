using SigilWarAscend.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SigilWarAscend.UI
{
	[DisallowMultipleComponent]
	public sealed class SigilWarHealthBar : MonoBehaviour
	{
		[Header("References")]
		public SigilWarHealth Health;
		public SigilWarPlayer Player;
		public Slider Slider;
		public Canvas Canvas;

		[Header("Display")]
		public bool InvertValue = true;
		public bool HideWhenFull;
		public bool HideWhenDead;
		public bool HideForLocalPlayer;
		public bool BillboardToCamera = true;

		private Camera _mainCamera;

		private void Reset()
		{
			Slider = GetComponentInChildren<Slider>(true);
			Canvas = GetComponentInChildren<Canvas>(true);
			Health = GetComponentInParent<SigilWarHealth>();
			Player = GetComponentInParent<SigilWarPlayer>();
		}

		private void Awake()
		{
			if (Slider == null)
			{
				Slider = GetComponentInChildren<Slider>(true);
			}

			if (Canvas == null)
			{
				Canvas = GetComponentInChildren<Canvas>(true);
			}

			if (Health == null)
			{
				Health = GetComponentInParent<SigilWarHealth>();
			}

			if (Player == null)
			{
				Player = GetComponentInParent<SigilWarPlayer>();
			}

			if (Slider != null)
			{
				Slider.minValue = 0f;
				Slider.maxValue = 1f;
			}
		}

		private void LateUpdate()
		{
			if (_mainCamera == null)
			{
				_mainCamera = Camera.main;
			}

			RefreshNow();
			UpdateBillboard();
		}

		public void RefreshNow()
		{
			UpdateValue();
			UpdateVisibility();
		}

		private void UpdateValue()
		{
			if (Slider == null || Health == null)
				return;

			float value = Mathf.Clamp01(Health.HealthNormalized);
			Slider.value = InvertValue ? 1f - value : value;
		}

		private void UpdateVisibility()
		{
			GameObject target = Canvas != null ? Canvas.gameObject : gameObject;
			if (target == null || Health == null)
				return;

			bool visible = true;

			if (HideWhenFull && Health.CurrentHealth >= Health.MaxHealth)
			{
				visible = false;
			}

			if (HideWhenDead && Health.IsAlive == false)
			{
				visible = false;
			}

			if (HideForLocalPlayer && Player != null && Player.Object != null && (Player.HasInputAuthority || Player.HasStateAuthority))
			{
				visible = false;
			}

			target.SetActive(visible);
		}

		private void UpdateBillboard()
		{
			if (BillboardToCamera == false || _mainCamera == null)
				return;

			Transform target = Canvas != null ? Canvas.transform : transform;
			Vector3 forward = target.position - _mainCamera.transform.position;
			if (forward.sqrMagnitude <= 0.0001f)
				return;

			target.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
		}
	}
}
