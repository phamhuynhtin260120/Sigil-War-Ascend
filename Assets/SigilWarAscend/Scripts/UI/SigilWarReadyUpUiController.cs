using Fusion;
using SigilWarAscend.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SigilWarAscend.UI
{
	/// <summary>
	/// Scene-authored ready-up UI binder.
	/// Assign the UI references from the Gameplay scene so the layout stays editable in the editor.
	/// </summary>
	public sealed class SigilWarReadyUpUiController : MonoBehaviour
	{
		[Header("Scene UI")]
		[SerializeField] private CanvasGroup _rootGroup;
		[SerializeField] private TextMeshProUGUI _titleText;
		[SerializeField] private TextMeshProUGUI _bodyText;
		[SerializeField] private TextMeshProUGUI _statusText;
		[SerializeField] private Button _confirmButton;
		[SerializeField] private TextMeshProUGUI _confirmLabel;

		private SigilWarGameManager _gameManager;
		private bool _hasSubmittedReady;
		private bool _hasLockedGameplayCursor;

		private void Awake()
		{
			EnsureEventSystem();
			if (_confirmButton != null)
			{
				_confirmButton.onClick.RemoveListener(OnConfirmReadyClicked);
				_confirmButton.onClick.AddListener(OnConfirmReadyClicked);
			}
			SetOverlayVisible(false);
		}

		private void OnDestroy()
		{
			if (_confirmButton != null)
			{
				_confirmButton.onClick.RemoveListener(OnConfirmReadyClicked);
			}
		}

		private void Update()
		{
			if (_gameManager == null)
			{
				_gameManager = FindFirstObjectByType<SigilWarGameManager>();
			}

			if (CanReadGameplayState(_gameManager) == false)
			{
				SetOverlayVisible(false);
				return;
			}

			bool isReadyUpActive = _gameManager.IsReadyUpActive;
			if (isReadyUpActive == false)
			{
				SetOverlayVisible(false);
				LockGameplayCursorOnce();
				return;
			}

			_hasLockedGameplayCursor = false;
			SetGameplayCursorLocked(false);
			SetOverlayVisible(true);
			RefreshTexts();
			RefreshButtonState();
		}

		private void OnConfirmReadyClicked()
		{
			if (CanReadGameplayState(_gameManager) == false)
				return;

			if (_gameManager.IsReadyUpActive == false)
				return;

			NetworkRunner runner = _gameManager.Runner;
			if (runner == null || runner.LocalPlayer == PlayerRef.None)
				return;

			if (_gameManager.IsPlayerReady(runner.LocalPlayer))
			{
				_hasSubmittedReady = true;
				RefreshButtonState();
				return;
			}

			_gameManager.EnsureLocalPlayerSpawned();
			_gameManager.SetPlayerReady(runner.LocalPlayer, true);
			_hasSubmittedReady = true;
			RefreshButtonState();
		}

		private void RefreshTexts()
		{
			SigilWarGameplayTextConfig config = ResolveTextConfig();

			if (_titleText != null)
			{
				_titleText.text = config != null && string.IsNullOrWhiteSpace(config.ReadyUpTitle) == false
					? config.ReadyUpTitle
					: "Ready Up";
			}

			if (_bodyText != null)
			{
				_bodyText.text = _gameManager != null ? _gameManager.ResolvedReadyUpInstructions : string.Empty;
			}

			if (_statusText != null)
			{
				int readyCount = _gameManager != null ? _gameManager.ReadyPlayerCount : 0;
				int activeCount = _gameManager != null ? Mathf.Max(_gameManager.ActivePlayerCount, 1) : 1;
				string format = config != null && string.IsNullOrWhiteSpace(config.ReadyUpProgressFormat) == false
					? config.ReadyUpProgressFormat
					: "Ready: {0}/{1}";
				_statusText.text = string.Format(format, readyCount, activeCount);
			}

			if (_confirmLabel != null)
			{
				_confirmLabel.text = ResolveConfirmButtonLabel(config);
			}
		}

		private void RefreshButtonState()
		{
			if (_confirmButton == null)
				return;

			bool canInteract = false;
			if (CanReadGameplayState(_gameManager))
			{
				NetworkRunner runner = _gameManager.Runner;
				if (runner != null && runner.LocalPlayer != PlayerRef.None)
				{
					canInteract = _gameManager.IsPlayerReady(runner.LocalPlayer) == false;
				}
			}

			_confirmButton.interactable = canInteract;
		}

		private string ResolveConfirmButtonLabel(SigilWarGameplayTextConfig config)
		{
			if (CanReadGameplayState(_gameManager))
			{
				NetworkRunner runner = _gameManager.Runner;
				if (runner != null && runner.LocalPlayer != PlayerRef.None && _gameManager.IsPlayerReady(runner.LocalPlayer))
				{
					if (config != null && string.IsNullOrWhiteSpace(config.ReadyUpConfirmedLabel) == false)
						return config.ReadyUpConfirmedLabel;

					return "Ready";
				}
			}

			if (_hasSubmittedReady)
			{
				if (config != null && string.IsNullOrWhiteSpace(config.ReadyUpConfirmedLabel) == false)
					return config.ReadyUpConfirmedLabel;

				return "Ready";
			}

			if (config != null && string.IsNullOrWhiteSpace(config.ReadyUpConfirmLabel) == false)
				return config.ReadyUpConfirmLabel;

			return "Confirm";
		}

		private SigilWarGameplayTextConfig ResolveTextConfig()
		{
			if (_gameManager != null && _gameManager.GameplayTextConfig != null)
				return _gameManager.GameplayTextConfig;

			return SigilWarGameplayTextConfig.LoadDefault();
		}

		private void EnsureEventSystem()
		{
			if (FindFirstObjectByType<EventSystem>() != null)
				return;

			GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
			eventSystemObject.transform.SetParent(transform, false);
		}

		private void SetOverlayVisible(bool isVisible)
		{
			if (_rootGroup == null)
				return;

			_rootGroup.alpha = isVisible ? 1f : 0f;
			_rootGroup.interactable = isVisible;
			_rootGroup.blocksRaycasts = isVisible;
		}

		private void LockGameplayCursorOnce()
		{
			if (_hasLockedGameplayCursor)
				return;

			_hasLockedGameplayCursor = true;
			SetGameplayCursorLocked(true);
		}

		private static bool CanReadGameplayState(SigilWarGameManager gameManager)
		{
			return gameManager != null &&
				gameManager.Object != null &&
				gameManager.Object.IsValid &&
				gameManager.Runner != null;
		}

		private static void SetGameplayCursorLocked(bool isLocked)
		{
			Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !isLocked;
		}

	}
}
