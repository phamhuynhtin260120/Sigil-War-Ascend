using Fusion;
using SigilWarAscend.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SigilWarAscend.UI
{
	/// <summary>
	/// Runtime-built gameplay HUD for the local player and match state.
	/// Kept separate from scene auth setup so the HUD can come online as soon as gameplay does.
	/// </summary>
	public sealed class SigilWarGameplayHudController : MonoBehaviour
	{
		private CanvasGroup _rootGroup;
		private TextMeshProUGUI _playerTitleText;
		private TextMeshProUGUI _playerStatsText;
		private Image _healthFillImage;
		private TextMeshProUGUI _healthText;
		private TextMeshProUGUI _matchTitleText;
		private TextMeshProUGUI _matchStatsText;

		private SigilWarGameManager _gameManager;
		private SigilWarPlayer _localPlayer;

		private void Awake()
		{
			EnsureOverlay();
			SetVisible(false);
		}

		private void Update()
		{
			ResolveReferences();
			if (_gameManager == null)
			{
				SetVisible(false);
				return;
			}

			SetVisible(true);
			RefreshLocalPlayerPanel();
			RefreshMatchPanel();
		}

		private void ResolveReferences()
		{
			if (_gameManager == null)
			{
				_gameManager = FindFirstObjectByType<SigilWarGameManager>();
			}

			if (_localPlayer != null && _localPlayer.gameObject.activeInHierarchy)
				return;

			if (_gameManager != null && _gameManager.Runner != null && _gameManager.Runner.LocalPlayer != PlayerRef.None)
			{
				NetworkObject playerObject = _gameManager.Runner.GetPlayerObject(_gameManager.Runner.LocalPlayer);
				if (playerObject != null)
				{
					_localPlayer = playerObject.GetComponent<SigilWarPlayer>();
					if (_localPlayer != null)
						return;
				}
			}

			SigilWarPlayer[] players = FindObjectsByType<SigilWarPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			for (int i = 0; i < players.Length; i++)
			{
				if (players[i] != null && players[i].IsLocalPlayer)
				{
					_localPlayer = players[i];
					return;
				}
			}
		}

		private void RefreshLocalPlayerPanel()
		{
			if (_playerTitleText != null)
			{
				_playerTitleText.text = _localPlayer != null && string.IsNullOrWhiteSpace(_localPlayer.Nickname) == false
					? _localPlayer.Nickname
					: "Dang tim local player...";
			}

			if (_playerStatsText != null)
			{
				if (_localPlayer == null)
				{
					_playerStatsText.text = "Kills: --\nPickups: --\nState: --";
				}
				else
				{
					_playerStatsText.text =
						$"Kills: {_localPlayer.PlayerKills}\n" +
						$"Pickups: {_localPlayer.CollectedPickups}\n" +
						$"State: {(_localPlayer.IsAlive ? "Alive" : "Dead")}";
				}
			}

			if (_healthText != null)
			{
				_healthText.text = _localPlayer == null
					? "HP: --/--"
					: $"HP: {_localPlayer.CurrentHealth}/{Mathf.Max(_localPlayer.MaxHealth, 0)}";
			}

			if (_healthFillImage != null)
			{
				float fill = _localPlayer != null ? Mathf.Clamp01(_localPlayer.HealthNormalized) : 0f;
				_healthFillImage.fillAmount = fill;
				_healthFillImage.color = Color.Lerp(new Color(0.82f, 0.2f, 0.18f), new Color(0.22f, 0.84f, 0.36f), fill);
			}
		}

		private void RefreshMatchPanel()
		{
			if (_matchTitleText != null)
			{
				_matchTitleText.text = $"Phase: {FormatPhase(_gameManager.CurrentPhase)}";
			}

			if (_matchStatsText == null)
				return;

			string localLane = "--";
			string coreHolder = "--";
			if (_gameManager.Runner != null && _gameManager.Runner.LocalPlayer != PlayerRef.None)
			{
				localLane = _gameManager.GetAssignedLane(_gameManager.Runner.LocalPlayer).ToString();
			}

			if (_gameManager.CurrentCoreHolder != PlayerRef.None)
			{
				coreHolder = $"Player{_gameManager.CurrentCoreHolder.PlayerId}";
			}

			_matchStatsText.text =
				$"Timer: {FormatTime(_gameManager.RemainingPhaseTime)}\n" +
				$"Ready: {_gameManager.ReadyPlayerCount}/{Mathf.Max(_gameManager.ActivePlayerCount, 1)}\n" +
				$"Lane: {localLane}\n" +
				$"Portals: {(_gameManager.ArePortalsOpen ? "Open" : "Closed")}\n" +
				$"Core: {(_gameManager.IsCoreSpawned ? "Active" : "Hidden")}\n" +
				$"Core Holder: {coreHolder}\n" +
				$"Core Timer: {FormatTime(_gameManager.RemainingCoreControlTime)}";
		}

		private void EnsureOverlay()
		{
			GameObject canvasObject = new GameObject("GameplayHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasObject.transform.SetParent(transform, false);

			Canvas canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 1500;

			CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0.5f;

			GameObject rootObject = CreateUiObject("GameplayHudRoot", canvasObject.transform);
			_rootGroup = rootObject.AddComponent<CanvasGroup>();
			RectTransform rootRect = rootObject.GetComponent<RectTransform>();
			StretchFull(rootRect);

			GameObject playerPanel = CreatePanel("LocalPlayerPanel", rootObject.transform, new Vector2(32f, -32f), new Vector2(420f, 220f), new Vector2(0f, 1f));
			_playerTitleText = CreateText("PlayerTitle", playerPanel.transform, 28f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
			AnchorBlock(_playerTitleText.rectTransform, new Vector2(20f, -20f), new Vector2(380f, 34f), new Vector2(0f, 1f));

			GameObject healthBarRoot = CreateUiObject("HealthBarRoot", playerPanel.transform);
			RectTransform healthBarRect = healthBarRoot.GetComponent<RectTransform>();
			AnchorBlock(healthBarRect, new Vector2(20f, -68f), new Vector2(380f, 28f), new Vector2(0f, 1f));
			Image healthBarBackground = healthBarRoot.AddComponent<Image>();
			healthBarBackground.color = new Color(0.09f, 0.1f, 0.12f, 0.92f);

			GameObject healthFillRoot = CreateUiObject("HealthFill", healthBarRoot.transform);
			RectTransform healthFillRect = healthFillRoot.GetComponent<RectTransform>();
			StretchFull(healthFillRect);
			_healthFillImage = healthFillRoot.AddComponent<Image>();
			_healthFillImage.type = Image.Type.Filled;
			_healthFillImage.fillMethod = Image.FillMethod.Horizontal;
			_healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
			_healthFillImage.color = new Color(0.22f, 0.84f, 0.36f, 1f);

			_healthText = CreateText("HealthText", playerPanel.transform, 20f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
			AnchorBlock(_healthText.rectTransform, new Vector2(20f, -104f), new Vector2(380f, 24f), new Vector2(0f, 1f));

			_playerStatsText = CreateText("PlayerStats", playerPanel.transform, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
			_playerStatsText.enableWordWrapping = true;
			AnchorBlock(_playerStatsText.rectTransform, new Vector2(20f, -136f), new Vector2(380f, 70f), new Vector2(0f, 1f));

			GameObject matchPanel = CreatePanel("MatchPanel", rootObject.transform, new Vector2(0f, -32f), new Vector2(520f, 260f), new Vector2(0.5f, 1f));
			_matchTitleText = CreateText("MatchTitle", matchPanel.transform, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
			AnchorBlock(_matchTitleText.rectTransform, new Vector2(0f, -20f), new Vector2(460f, 36f), new Vector2(0.5f, 1f));

			_matchStatsText = CreateText("MatchStats", matchPanel.transform, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
			_matchStatsText.enableWordWrapping = true;
			AnchorBlock(_matchStatsText.rectTransform, new Vector2(0f, -68f), new Vector2(460f, 150f), new Vector2(0.5f, 1f));
		}

		private void SetVisible(bool isVisible)
		{
			if (_rootGroup == null)
				return;

			_rootGroup.alpha = isVisible ? 1f : 0f;
			_rootGroup.interactable = isVisible;
			_rootGroup.blocksRaycasts = false;
		}

		private static string FormatPhase(MatchPhase phase)
		{
			return phase switch
			{
				MatchPhase.None => "Ready Up",
				MatchPhase.Preparation => "Preparation",
				MatchPhase.LanePhase => "Lane Phase",
				MatchPhase.PortalPhase => "Portal Phase",
				MatchPhase.CorePhase => "Core Phase",
				MatchPhase.MatchEnded => "Match Ended",
				_ => phase.ToString(),
			};
		}

		private static string FormatTime(float seconds)
		{
			int safeSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
			int minutes = safeSeconds / 60;
			int remainingSeconds = safeSeconds % 60;
			return $"{minutes:00}:{remainingSeconds:00}";
		}

		private static GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
		{
			GameObject panelObject = CreateUiObject(name, parent);
			Image image = panelObject.AddComponent<Image>();
			image.color = new Color(0.05f, 0.08f, 0.12f, 0.84f);

			RectTransform rect = panelObject.GetComponent<RectTransform>();
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = anchor;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			return panelObject;
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
			if (TMP_Settings.defaultFontAsset != null)
			{
				text.font = TMP_Settings.defaultFontAsset;
			}
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
		}

		private static void AnchorBlock(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchor)
		{
			rectTransform.anchorMin = anchor;
			rectTransform.anchorMax = anchor;
			rectTransform.pivot = anchor;
			rectTransform.anchoredPosition = anchoredPosition;
			rectTransform.sizeDelta = sizeDelta;
		}
	}
}
