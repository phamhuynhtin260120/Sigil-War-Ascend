using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	public sealed class Portal : MonoBehaviour
	{
		[Header("References")]
		public GameObject VisualRoot;
		public Collider[] PortalColliders;

		[Header("Debug")]
		[SerializeField]
		private bool _isOpen;

		public bool IsOpen => _isOpen;

		public void SetOpen(bool isOpen)
		{
			_isOpen = isOpen;

			if (VisualRoot != null)
			{
				VisualRoot.SetActive(isOpen);
			}

			if (PortalColliders == null)
				return;

			for (int i = 0; i < PortalColliders.Length; i++)
			{
				if (PortalColliders[i] != null)
				{
					PortalColliders[i].enabled = isOpen;
				}
			}
		}

		private void Awake()
		{
			SetOpen(_isOpen);
		}
	}
}
