using Fusion;
using SigilWarAscend.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SigilWarAscend.UI
{
	/// <summary>
	/// Runtime-built ready-up overlay for the Gameplay scene.
	/// Kept self-contained so we can rebuild the in-game UI flow step by step.
	/// </summary>
	public sealed class SigilWarReadyUpUiController : MonoBehaviour
	{
		private Canvas _canvas;
		private CanvasGroup _rootGroup;
		private TextMeshProUGUI _titleText;
		private TextMeshProUGUI _bodyText;
		private TextMeshProUGUI _statusText;
		private Button _confirmButton;
		private TextMeshProUGUI _confirmLabel;
		private SigilWarGameManager _gameManager;
		private bool _hasSubmittedReady;
		private bool _hasLockedGameplayCursor;

		private void Awake()
		{
			EnsureEventSystem();
			EnsureOverlay();
			SetOverlayVisible(false);
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

		private void EnsureOverlay()
		{
			GameObject canvasObject = new GameObject("ReadyUpCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasObject.transform.SetParent(transform, false);

			_canvas = canvasObject.GetComponent<Canvas>();
			_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			_canvas.sortingOrder = 2000;

			CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0.5f;

			GameObject rootObject = CreateUiObject("ReadyUpRoot", canvasObject.transform);
			Image backdrop = rootObject.AddComponent<Image>();
			backdrop.color = new Color(0.04f, 0.07f, 0.12f, 0.86f);
			_rootGroup = rootObject.AddComponent<CanvasGroup>();

			RectTransform rootRect = rootObject.GetComponent<RectTransform>();
			StretchFull(rootRect);

			GameObject panelObject = CreateUiObject("Panel", rootObject.transform);
			Image panelImage = panelObject.AddComponent<Image>();
			panelImage.color = new Color(0.09f, 0.14f, 0.2f, 0.97f);
			RectTransform panelRect = panelObject.GetComponent<RectTransform>();
			panelRect.anchorMin = new Vector2(0.5f, 0.5f);
			panelRect.anchorMax = new Vector2(0.5f, 0.5f);
			panelRect.pivot = new Vector2(0.5f, 0.5f);
			panelRect.sizeDelta = new Vector2(860f, 620f);
			panelRect.anchoredPosition = Vector2.zero;

			VerticalLayoutGroup panelLayout = panelObject.AddComponent<VerticalLayoutGroup>();
			panelLayout.padding = new RectOffset(36, 36, 30, 30);
			panelLayout.spacing = 18f;
			panelLayout.childControlHeight = false;
			panelLayout.childControlWidth = true;
			panelLayout.childForceExpandHeight = false;
			panelLayout.childForceExpandWidth = true;

			ContentSizeFitter panelFitter = panelObject.AddComponent<ContentSizeFitter>();
			panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			_titleText = CreateText("Title", panelObject.transform, 32f, FontStyles.Bold, TextAlignmentOptions.Center);
			SetPreferredHeight(_titleText.rectTransform, 54f);

			_bodyText = CreateText("Body", panelObject.transform, 22f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
			_bodyText.enableWordWrapping = true;
			_bodyText.overflowMode = TextOverflowModes.Overflow;
			SetPreferredHeight(_bodyText.rectTransform, 360f);

			_statusText = CreateText("Status", panelObject.transform, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
			SetPreferredHeight(_statusText.rectTransform, 42f);

			GameObject buttonObject = CreateUiObject("ConfirmButton", panelObject.transform);
			Image buttonImage = buttonObject.AddComponent<Image>();
			buttonImage.color = new Color(0.84f, 0.65f, 0.24f, 1f);
			_confirmButton = buttonObject.AddComponent<Button>();
			_confirmButton.targetGraphic = buttonImage;
			_confirmButton.onClick.AddListener(OnConfirmReadyClicked);

			RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
			buttonRect.sizeDelta = new Vector2(0f, 62f);
			LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
			buttonLayout.preferredHeight = 62f;

			_confirmLabel = CreateText("Label", buttonObject.transform, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
			StretchFull(_confirmLabel.rectTransform);
			_confirmLabel.color = new Color(0.1f, 0.08f, 0.03f, 1f);
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

		private static GameObject CreateUiObject(string name, Transform parent)
		{
			GameObject gameObject = new GameObject(name, typeof(RectTransform));
			gameObject.transform.SetParent(parent, false);
			return gameObject;
		}

		private static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
		{
			GameObject textObject = CreateUiObject(name, parent);
			TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
			text.fontSize = fontSize;
			text.fontStyle = fontStyle;
			text.alignment = alignment;
			text.color = Color.white;
			return text;
		}

		private static void StretchFull(RectTransform rectTransform)
		{
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			rectTransform.anchoredPosition = Vector2.zero;
		}

		private static void SetPreferredHeight(RectTransform rectTransform, float preferredHeight)
		{
			LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
			layout.preferredHeight = preferredHeight;
		}
	}
}
