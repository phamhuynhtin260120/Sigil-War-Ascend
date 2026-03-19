using TMPro;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Simple billboard nameplate for remote players.
	/// </summary>
	public sealed class SigilWarNameplate : MonoBehaviour
	{
		public TextMeshProUGUI NicknameText;

		private Transform _cameraTransform;

		public void SetNickname(string nickname)
		{
			if (NicknameText != null)
			{
				NicknameText.text = nickname;
			}
		}

		private void Awake()
		{
			_cameraTransform = Camera.main != null ? Camera.main.transform : null;
			SetNickname(string.Empty);
		}

		private void LateUpdate()
		{
			if (_cameraTransform == null && Camera.main != null)
			{
				_cameraTransform = Camera.main.transform;
			}

			if (_cameraTransform != null)
			{
				transform.rotation = _cameraTransform.rotation;
			}
		}
	}
}
