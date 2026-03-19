using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	public sealed class CoreObjective : MonoBehaviour
	{
		[Header("References")]
		public GameObject VisualRoot;
		public Collider TriggerCollider;

		[Header("Debug")]
		[SerializeField]
		private bool _isActive;

		public bool IsActive => _isActive;

		public void SetObjectiveActive(bool isActive)
		{
			_isActive = isActive;

			if (VisualRoot != null)
			{
				VisualRoot.SetActive(isActive);
			}

			if (TriggerCollider != null)
			{
				TriggerCollider.enabled = isActive;
			}
		}

		private void Awake()
		{
			SetObjectiveActive(_isActive);
		}
	}
}
