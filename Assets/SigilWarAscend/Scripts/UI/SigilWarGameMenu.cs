using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using SigilWarAscend.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SigilWarAscend.UI
{
	/// <summary>
	/// Sigil War specific version of the Fusion sample game menu.
	/// It owns connect/disconnect flow, nickname persistence and cursor locking.
	/// </summary>
	public sealed class SigilWarGameMenu : MonoBehaviour
	{
		[Header("Start Game Setup")]
		[Tooltip("Used by Fusion session properties so this game mode stays isolated from other sample rooms.")]
		public string GameModeIdentifier = "SigilWarAscend";
		public NetworkRunner RunnerPrefab;
		public int MaxPlayerCount = 4;

		[Header("Debug")]
		[Tooltip("For local iteration in editor without shared multiplayer.")]
		public bool ForceSinglePlayer;

		[Header("UI Setup")]
		public CanvasGroup PanelGroup;
		public TMP_InputField RoomText;
		public TMP_InputField NicknameText;
		public TextMeshProUGUI StatusText;
		public GameObject StartGroup;
		public GameObject DisconnectGroup;
		[Tooltip("Non-UI objects that should hide/show together with the menu, such as VideoPlayer objects.")]
		public GameObject[] AdditionalMenuObjects;

		private NetworkRunner _runnerInstance;
		private static string _shutdownStatus;
		[Header("Optional Designer References")]
		[SerializeField] private Canvas _parentCanvas;
		[SerializeField] private Button _resumeButton;
		[SerializeField] private RectTransform _menuExtrasRoot;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _tutorialButton;
		[SerializeField] private Button _creditsButton;
		[SerializeField] private GameObject _settingsPanel;
		[SerializeField] private GameObject _tutorialPanel;
		[SerializeField] private TextMeshProUGUI _tutorialBodyText;
		[SerializeField] private GameObject _creditsPanel;
		[SerializeField] private Slider _masterVolumeSlider;
		[SerializeField] private TextMeshProUGUI _volumeValueText;
		[SerializeField] private RectTransform _overlayRoot;
		[SerializeField] private CanvasGroup _hudGroup;
		[SerializeField] private RectTransform _hudPanel;
		[SerializeField] private GameObject _timerRoot;
		[SerializeField] private TextMeshProUGUI _phaseText;
		[SerializeField] private TextMeshProUGUI _objectiveText;
		[SerializeField] private TextMeshProUGUI _timerText;
		[SerializeField] private TextMeshProUGUI _statsText;
		[SerializeField] private TextMeshProUGUI _hintText;
		[SerializeField] private GameObject _matchEndRoot;
		[SerializeField] private TextMeshProUGUI _matchEndTitleText;
		[SerializeField] private TextMeshProUGUI _matchEndBodyText;
		[SerializeField] private Button _matchEndBackButton;
		[SerializeField] private GameObject _readyUpRoot;
		[SerializeField] private TextMeshProUGUI _readyUpBodyText;
		[SerializeField] private TextMeshProUGUI _readyUpStatusText;
		[SerializeField] private Button _readyUpConfirmButton;
		[SerializeField] private TextMeshProUGUI _readyUpConfirmLabel;
		[SerializeField] private GameObject _eliminationRoot;
		[SerializeField] private Button _returnToRoomButton;
		private bool _isMatchEndVisible;

		private const string MasterVolumePrefsKey = "SigilWar.MasterVolume";
		private static readonly string SharedTutorialText = SigilWarGameManager.DefaultReadyUpInstructions;

		public bool IsMenuOpen => PanelGroup != null && PanelGroup.alpha > 0.001f && PanelGroup.blocksRaycasts;
		public bool IsConnected => _runnerInstance != null;

		private void Awake()
		{
			EnsureRuntimeUi();
			BindUiCallbacks();
			ApplyStaticUiTexts();
		}

		public async void StartGame()
		{
			await Disconnect();

			PlayerPrefs.SetString(SigilWarPlayerPrefsKeys.PlayerName, NicknameText.text);
			PlayerPrefs.Save();

			_runnerInstance = Instantiate(RunnerPrefab);

			var events = _runnerInstance.GetComponent<NetworkEvents>();
			if (events != null)
			{
				events.OnShutdown.AddListener(OnShutdown);
			}

			var sceneInfo = new NetworkSceneInfo();
			sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

			var startArguments = new StartGameArgs()
			{
				GameMode = Application.isEditor && ForceSinglePlayer ? GameMode.Single : GameMode.Shared,
				SessionName = RoomText.text,
				PlayerCount = MaxPlayerCount,
				SessionProperties = new Dictionary<string, SessionProperty>
				{
					["GameMode"] = GameModeIdentifier,
				},
				Scene = sceneInfo,
			};

			StatusText.text = startArguments.GameMode == GameMode.Single ? "Starting single-player..." : "Connecting...";

			var startTask = _runnerInstance.StartGame(startArguments);
			await startTask;

			if (startTask.Result.Ok)
			{
				StatusText.text = string.Empty;
				SetMenuVisible(false);
			}
			else
			{
				StatusText.text = $"Connection Failed: {startTask.Result.ShutdownReason}";
			}
		}

		public async void DisconnectClicked()
		{
			await Disconnect();
		}

		public async void BackToMenu()
		{
			await Disconnect();
			SceneManager.LoadScene(0);
		}

		public void TogglePanelVisibility()
		{
			if (PanelGroup == null)
				return;

			if (_isMatchEndVisible)
				return;

			if (IsMenuOpen && _runnerInstance == null)
				return;

			SetMenuVisible(!IsMenuOpen);
		}

		private void OnEnable()
		{
			EnsureRuntimeUi();
			BindUiCallbacks();
			ApplyStaticUiTexts();

			string nickname = PlayerPrefs.GetString(SigilWarPlayerPrefsKeys.PlayerName);
			if (string.IsNullOrEmpty(nickname))
			{
				nickname = "Player" + Random.Range(10000, 100000);
			}

			if (NicknameText != null)
			{
				NicknameText.text = nickname;
			}

			if (StatusText != null)
			{
				StatusText.text = _shutdownStatus ?? string.Empty;
			}

			_shutdownStatus = null;
			SetMenuVisible(true);
		}

		private void Update()
		{
			if ((Input.GetKeyDown(KeyCode.Return) && IsConnected == false) || Input.GetKeyDown(KeyCode.Escape))
			{
				TogglePanelVisibility();
			}

			RefreshPanelState();
			RefreshGameplayOverlay();
		}

		public async Task Disconnect()
		{
			if (_runnerInstance == null)
				return;

			if (StatusText != null)
			{
				StatusText.text = "Disconnecting...";
			}

			if (PanelGroup != null)
			{
				PanelGroup.interactable = false;
			}

			var events = _runnerInstance.GetComponent<NetworkEvents>();
			if (events != null)
			{
				events.OnShutdown.RemoveListener(OnShutdown);
			}

			await _runnerInstance.Shutdown();
			_runnerInstance = null;

			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
		{
			_shutdownStatus = $"Shutdown: {reason}";
			Debug.LogWarning(_shutdownStatus);

			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		private void RefreshPanelState()
		{
			if (PanelGroup == null)
			{
				SetCursorState(_isMatchEndVisible == false);
				return;
			}

			bool isPanelVisible = IsMenuOpen;

			if (StartGroup != null)
			{
				StartGroup.SetActive(_runnerInstance == null);
			}

			if (DisconnectGroup != null)
			{
				DisconnectGroup.SetActive(_runnerInstance != null);
			}

			if (RoomText != null)
			{
				RoomText.interactable = _runnerInstance == null;
			}

			if (NicknameText != null)
			{
				NicknameText.interactable = _runnerInstance == null;
			}

			if (StatusText != null && _runnerInstance != null)
			{
				StatusText.text = isPanelVisible && _isMatchEndVisible == false
					? "Paused"
					: string.Empty;
			}

			if (_resumeButton != null)
			{
				_resumeButton.gameObject.SetActive(_runnerInstance != null);
			}

			SetCursorState(isPanelVisible == false && _isMatchEndVisible == false);
		}

		private void SetMenuVisible(bool isVisible)
		{
			if (PanelGroup == null)
			{
				SetCursorState(!isVisible);
				return;
			}

			PanelGroup.alpha = isVisible ? 1f : 0f;
			PanelGroup.interactable = isVisible;
			PanelGroup.blocksRaycasts = isVisible;

			if (AdditionalMenuObjects != null)
			{
				for (int i = 0; i < AdditionalMenuObjects.Length; i++)
				{
					if (AdditionalMenuObjects[i] != null)
					{
						AdditionalMenuObjects[i].SetActive(isVisible);
					}
				}
			}

			SetCursorState(!isVisible);
		}

		private static void SetCursorState(bool lockCursor)
		{
			Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !lockCursor;
		}

		private void EnsureRuntimeUi()
		{
			if (_parentCanvas == null)
			{
				_parentCanvas = GetComponentInParent<Canvas>();
			}

			EnsureResumeButton();
			EnsureMenuExtraButtons();
			EnsureGameplayOverlay();
			EnsureReadyUpOverlay();
			EnsureEliminationOverlay();
		}

		private void BindUiCallbacks()
		{
			if (_resumeButton != null)
			{
				_resumeButton.onClick.RemoveListener(ClosePauseMenu);
				_resumeButton.onClick.AddListener(ClosePauseMenu);
			}

			if (_settingsButton != null)
			{
				_settingsButton.onClick.RemoveListener(OpenSettingsPanel);
				_settingsButton.onClick.AddListener(OpenSettingsPanel);
			}

			if (_tutorialButton != null)
			{
				_tutorialButton.onClick.RemoveListener(OpenTutorialPanel);
				_tutorialButton.onClick.AddListener(OpenTutorialPanel);
			}

			if (_creditsButton != null)
			{
				_creditsButton.onClick.RemoveListener(OpenCreditsPanel);
				_creditsButton.onClick.AddListener(OpenCreditsPanel);
			}

			if (_readyUpConfirmButton != null)
			{
				_readyUpConfirmButton.onClick.RemoveListener(ConfirmReadyUp);
				_readyUpConfirmButton.onClick.AddListener(ConfirmReadyUp);
			}

			if (_matchEndBackButton != null)
			{
				_matchEndBackButton.onClick.RemoveListener(BackToMenu);
				_matchEndBackButton.onClick.AddListener(BackToMenu);
			}

			if (_returnToRoomButton != null)
			{
				_returnToRoomButton.onClick.RemoveListener(BackToMenu);
				_returnToRoomButton.onClick.AddListener(BackToMenu);
			}

			if (_masterVolumeSlider != null)
			{
				_masterVolumeSlider.onValueChanged.RemoveListener(ApplyMasterVolumeFromSlider);
				_masterVolumeSlider.onValueChanged.AddListener(ApplyMasterVolumeFromSlider);
			}
		}

		private void ApplyStaticUiTexts()
		{
			if (_tutorialBodyText != null)
			{
				_tutorialBodyText.text = SharedTutorialText;
			}

			float initialVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefsKey, 0.8f));
			if (_masterVolumeSlider != null)
			{
				_masterVolumeSlider.SetValueWithoutNotify(initialVolume);
			}

			ApplyMasterVolume(initialVolume);
		}

		private void ClosePauseMenu()
		{
			SetMenuVisible(false);
		}

		private void OpenSettingsPanel()
		{
			ToggleMenuPanel(_settingsPanel);
		}

		private void OpenTutorialPanel()
		{
			ToggleMenuPanel(_tutorialPanel);
		}

		private void OpenCreditsPanel()
		{
			ToggleMenuPanel(_creditsPanel);
		}

		private bool HasAssignedGameplayOverlay()
		{
			return _hudPanel != null
				&& _phaseText != null
				&& _objectiveText != null
				&& _timerText != null
				&& _statsText != null
				&& _matchEndRoot != null
				&& _matchEndTitleText != null
				&& _matchEndBodyText != null;
		}

		private bool HasAssignedReadyUpOverlay()
		{
			return _readyUpRoot != null
				&& _readyUpBodyText != null
				&& _readyUpStatusText != null
				&& _readyUpConfirmButton != null
				&& _readyUpConfirmLabel != null;
		}

		private bool HasAssignedEliminationOverlay()
		{
			return _eliminationRoot != null && _returnToRoomButton != null;
		}

		private void EnsureResumeButton()
		{
			if (_resumeButton != null || PanelGroup == null)
				return;

			var buttonRect = CreateButton(
				PanelGroup.transform as RectTransform,
				"ResumeButtonRuntime",
				"Resume",
				new Vector2(0.5f, 0f),
				new Vector2(0.5f, 0f),
				new Vector2(0f, 112f),
				new Vector2(220f, 56f),
				new Color(0.17f, 0.48f, 0.33f, 0.92f));

			_resumeButton = buttonRect.GetComponent<Button>();
			_resumeButton.gameObject.SetActive(false);
		}

		private void EnsureGameplayOverlay()
		{
			if (HasAssignedGameplayOverlay() || _parentCanvas == null)
				return;

			_overlayRoot = CreateStretchRect("GameplayOverlayRuntime", _parentCanvas.transform as RectTransform);

			var hudRoot = CreatePanel(
				_overlayRoot,
				"GameplayHudPanel",
				new Vector2(0f, 1f),
				new Vector2(0f, 1f),
				new Vector2(28f, -28f),
				new Vector2(420f, 184f),
				new Color(0.07f, 0.10f, 0.15f, 0.80f));
			_hudPanel = hudRoot;
			_hudGroup = hudRoot.gameObject.AddComponent<CanvasGroup>();
			_hudGroup.blocksRaycasts = false;
			_hudGroup.interactable = false;

			_phaseText = CreateText(
				hudRoot,
				"PhaseText",
				"Phase: Waiting",
				26,
				FontStyles.Bold,
				TextAlignmentOptions.TopLeft,
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
				new Vector2(18f, -16f),
				new Vector2(-18f, 52f));

			_objectiveText = CreateText(
				hudRoot,
				"ObjectiveText",
				"Connect to start the match.",
				19,
				FontStyles.Normal,
				TextAlignmentOptions.TopLeft,
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
				new Vector2(18f, -58f),
				new Vector2(-18f, 76f));

			_statsText = CreateText(
				hudRoot,
				"StatsText",
				string.Empty,
				18,
				FontStyles.Normal,
				TextAlignmentOptions.BottomLeft,
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(18f, 18f),
				new Vector2(-18f, 52f));

			var timerPanel = CreatePanel(
				_overlayRoot,
				"GameplayTimerPanel",
				new Vector2(0.5f, 1f),
				new Vector2(0.5f, 1f),
				new Vector2(0f, -28f),
				new Vector2(280f, 74f),
				new Color(0.05f, 0.08f, 0.12f, 0.82f));

			_timerText = CreateText(
				timerPanel,
				"TimerText",
				"00:00",
				32,
				FontStyles.Bold,
				TextAlignmentOptions.Center,
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				new Vector2(16f, 10f),
				new Vector2(-16f, -10f));
			_timerRoot = timerPanel.gameObject;

			_hintText = CreateText(
				_overlayRoot,
				"HintText",
				"ESC: pause menu",
				18,
				FontStyles.Normal,
				TextAlignmentOptions.BottomLeft,
				new Vector2(0f, 0f),
				new Vector2(0f, 0f),
				new Vector2(20f, 20f),
				new Vector2(320f, 32f));
			_hintText.color = new Color(1f, 1f, 1f, 0.82f);

			_matchEndRoot = CreatePanel(
				_overlayRoot,
				"MatchEndPanel",
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				Vector2.zero,
				Vector2.zero,
				new Color(0f, 0f, 0f, 0.68f)).gameObject;
			_matchEndRoot.SetActive(false);

			var endCard = CreatePanel(
				_matchEndRoot.transform as RectTransform,
				"MatchEndCard",
				new Vector2(0.5f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new Vector2(0f, 0f),
				new Vector2(560f, 300f),
				new Color(0.09f, 0.12f, 0.17f, 0.95f));

			_matchEndTitleText = CreateText(
				endCard,
				"MatchEndTitle",
				"Match Ended",
				34,
				FontStyles.Bold,
				TextAlignmentOptions.Center,
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
				new Vector2(24f, -32f),
				new Vector2(-24f, 70f));

			_matchEndBodyText = CreateText(
				endCard,
				"MatchEndBody",
				string.Empty,
				22,
				FontStyles.Normal,
				TextAlignmentOptions.Center,
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				new Vector2(28f, 78f),
				new Vector2(-28f, -104f));

			var backButton = CreateButton(
				endCard,
				"ReturnToMenuButton",
				"Return To Menu",
				new Vector2(0.5f, 0f),
				new Vector2(0.5f, 0f),
				new Vector2(0f, 34f),
				new Vector2(230f, 56f),
				new Color(0.22f, 0.45f, 0.73f, 0.95f));
			_matchEndBackButton = backButton.GetComponent<Button>();
		}

		private void EnsureReadyUpOverlay()
		{
			if (HasAssignedReadyUpOverlay() || _parentCanvas == null)
				return;

			var root = CreatePanel(
				_overlayRoot != null ? _overlayRoot : CreateStretchRect("GameplayOverlayRuntime", _parentCanvas.transform as RectTransform),
				"ReadyUpOverlayRuntime",
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				Vector2.zero,
				Vector2.zero,
				new Color(0f, 0f, 0f, 0.72f)).gameObject;
			_readyUpRoot = root;
			_readyUpRoot.SetActive(false);

			var card = CreatePanel(
				root.transform as RectTransform,
				"ReadyUpCard",
				new Vector2(0.5f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new Vector2(0f, 0f),
				new Vector2(860f, 620f),
				new Color(0.08f, 0.11f, 0.16f, 0.97f));

			CreateText(
				card,
				"ReadyUpTitle",
				"Hướng Dẫn Trước Khi Vào Trận",
				34,
				FontStyles.Bold,
				TextAlignmentOptions.Center,
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
				new Vector2(28f, -30f),
				new Vector2(-28f, 84f));

			_readyUpBodyText = CreateText(
				card,
				"ReadyUpBody",
				string.Empty,
				22,
				FontStyles.Normal,
				TextAlignmentOptions.TopLeft,
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				new Vector2(32f, 134f),
				new Vector2(-32f, -150f));

			_readyUpStatusText = CreateText(
				card,
				"ReadyUpStatus",
				"Đang chờ người chơi sẵn sàng...",
				20,
				FontStyles.Bold,
				TextAlignmentOptions.Center,
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(28f, 88f),
				new Vector2(-28f, 124f));

			var confirmRect = CreateButton(
				card,
				"ReadyUpConfirmButton",
				"Đã hiểu / Sẵn sàng",
				new Vector2(0.5f, 0f),
				new Vector2(0.5f, 0f),
				new Vector2(0f, 42f),
				new Vector2(280f, 60f),
				new Color(0.20f, 0.56f, 0.36f, 0.96f));
			_readyUpConfirmButton = confirmRect.GetComponent<Button>();
			_readyUpConfirmLabel = confirmRect.GetComponentInChildren<TextMeshProUGUI>(true);
		}

		private void EnsureEliminationOverlay()
		{
			if (HasAssignedEliminationOverlay() || _parentCanvas == null)
				return;

			var root = CreatePanel(
				_overlayRoot != null ? _overlayRoot : CreateStretchRect("GameplayOverlayRuntime", _parentCanvas.transform as RectTransform),
				"EliminationOverlayRuntime",
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				Vector2.zero,
				Vector2.zero,
				new Color(0f, 0f, 0f, 0.72f)).gameObject;
			_eliminationRoot = root;
			_eliminationRoot.SetActive(false);

			var card = CreatePanel(
				root.transform as RectTransform,
				"EliminationCard",
				new Vector2(0.5f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new Vector2(0f, 0f),
				new Vector2(620f, 280f),
				new Color(0.10f, 0.11f, 0.16f, 0.97f));

			CreateText(
				card,
				"EliminationTitle",
				"Bạn Đã Bị Loại Khỏi Trận",
				30,
				FontStyles.Bold,
				TextAlignmentOptions.Center,
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
				new Vector2(24f, -28f),
				new Vector2(-24f, 72f));

			CreateText(
				card,
				"EliminationBody",
				"Giai đoạn hiện tại không cho phép hồi sinh. Bạn có thể rời trận và quay về phòng tạo room để bắt đầu lại.",
				22,
				FontStyles.Normal,
				TextAlignmentOptions.Center,
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				new Vector2(28f, 86f),
				new Vector2(-28f, -96f));

			var returnButton = CreateButton(
				card,
				"ReturnToRoomButton",
				"Quay Về Phòng Tạo Room",
				new Vector2(0.5f, 0f),
				new Vector2(0.5f, 0f),
				new Vector2(0f, 34f),
				new Vector2(300f, 58f),
				new Color(0.58f, 0.30f, 0.20f, 0.96f));
			_returnToRoomButton = returnButton.GetComponent<Button>();
		}

		private void EnsureMenuExtraButtons()
		{
			if ((_menuExtrasRoot != null || _settingsButton != null || _tutorialButton != null || _creditsButton != null || _settingsPanel != null || _tutorialPanel != null || _creditsPanel != null) || PanelGroup == null)
				return;

			_menuExtrasRoot = CreateStretchRect("MenuExtrasRuntime", PanelGroup.transform as RectTransform);

			_settingsButton = CreateButton(
				_menuExtrasRoot,
				"SettingsButtonRuntime",
				"Settings",
				new Vector2(1f, 0f),
				new Vector2(1f, 0f),
				new Vector2(-150f, 112f),
				new Vector2(200f, 52f),
				new Color(0.20f, 0.34f, 0.56f, 0.92f)).GetComponent<Button>();

			_tutorialButton = CreateButton(
				_menuExtrasRoot,
				"TutorialButtonRuntime",
				"Tutorial",
				new Vector2(1f, 0f),
				new Vector2(1f, 0f),
				new Vector2(-150f, 174f),
				new Vector2(200f, 52f),
				new Color(0.29f, 0.42f, 0.24f, 0.92f)).GetComponent<Button>();

			_creditsButton = CreateButton(
				_menuExtrasRoot,
				"CreditsButtonRuntime",
				"Credits",
				new Vector2(1f, 0f),
				new Vector2(1f, 0f),
				new Vector2(-150f, 236f),
				new Vector2(200f, 52f),
				new Color(0.45f, 0.28f, 0.18f, 0.92f)).GetComponent<Button>();

			_settingsPanel = CreateModalPanel(
				_menuExtrasRoot,
				"SettingsPanelRuntime",
				"Settings",
				"Adjust a simple master volume for menu and gameplay audio.");
			_settingsPanel.SetActive(false);

			var sliderRoot = CreatePanel(
				_settingsPanel.transform as RectTransform,
				"MasterVolumeSliderRoot",
				new Vector2(0.5f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new Vector2(0f, 0f),
				new Vector2(420f, 70f),
				new Color(1f, 1f, 1f, 0.04f));

			CreateText(
				sliderRoot,
				"MasterVolumeLabel",
				"Master Volume",
				21,
				FontStyles.Bold,
				TextAlignmentOptions.Left,
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
				new Vector2(8f, -4f),
				new Vector2(-8f, 30f));

			_masterVolumeSlider = CreateSlider(
				sliderRoot,
				"MasterVolumeSlider",
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(8f, 12f),
				new Vector2(-96f, 38f));

			_volumeValueText = CreateText(
				sliderRoot,
				"MasterVolumeValue",
				"100%",
				20,
				FontStyles.Bold,
				TextAlignmentOptions.Right,
				new Vector2(1f, 0f),
				new Vector2(1f, 1f),
				new Vector2(-84f, 8f),
				new Vector2(-8f, -8f));

			_tutorialPanel = CreateModalPanel(
				_menuExtrasRoot,
				"TutorialPanelRuntime",
				"How To Play",
				SharedTutorialText);
			var tutorialTexts = _tutorialPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
			if (tutorialTexts.Length > 1)
			{
				_tutorialBodyText = tutorialTexts[1];
			}
			_tutorialPanel.SetActive(false);

			_creditsPanel = CreateModalPanel(
				_menuExtrasRoot,
				"CreditsPanelRuntime",
				"Credits",
				"Sigil War Ascend\nCourse final project prototype\n\nYou can replace this panel later with your team members, roles, acknowledgements, and asset credits.");
			_creditsPanel.SetActive(false);
		}

		private void RefreshGameplayOverlay()
		{
			if (HasAssignedGameplayOverlay() == false && HasAssignedReadyUpOverlay() == false && HasAssignedEliminationOverlay() == false)
				return;

			var gameManager = FindObjectOfType<SigilWarGameManager>();
			bool hasGameplayContext = IsConnected && gameManager != null;
			RefreshReadyUpOverlay(gameManager);
			RefreshEliminationOverlay(gameManager);

			if (_hudPanel != null)
			{
				_hudPanel.gameObject.SetActive(hasGameplayContext && (gameManager == null || gameManager.IsReadyUpActive == false) && (_eliminationRoot == null || _eliminationRoot.activeSelf == false));
			}

			if (_timerText != null)
			{
				var timerTarget = _timerRoot != null ? _timerRoot : _timerText.transform.parent.gameObject;
				timerTarget.SetActive(hasGameplayContext && (gameManager == null || gameManager.IsReadyUpActive == false) && (_eliminationRoot == null || _eliminationRoot.activeSelf == false));
			}

			if (_hintText != null)
			{
				_hintText.gameObject.SetActive(hasGameplayContext && _isMatchEndVisible == false && (gameManager == null || gameManager.IsReadyUpActive == false) && (_eliminationRoot == null || _eliminationRoot.activeSelf == false));
			}

			if (hasGameplayContext == false)
			{
				SetMatchEndVisible(false, string.Empty, string.Empty);
				return;
			}

			var localPlayer = GetLocalPlayer();
			UpdateHudTexts(gameManager, localPlayer);

			if (gameManager.CurrentPhase == MatchPhase.MatchEnded)
			{
				var result = BuildMatchEndMessage(gameManager, localPlayer);
				SetMatchEndVisible(true, result.title, result.body);
			}
			else
			{
				SetMatchEndVisible(false, string.Empty, string.Empty);
			}
		}

		private void RefreshReadyUpOverlay(SigilWarGameManager gameManager)
		{
			if (_readyUpRoot == null)
				return;

			bool visible = IsConnected && gameManager != null && gameManager.IsReadyUpActive;
			_readyUpRoot.SetActive(visible);

			if (visible == false)
				return;

			var localPlayer = GetLocalPlayer();
			PlayerRef localPlayerRef = localPlayer != null ? localPlayer.OwnerPlayerRef : (_runnerInstance != null ? _runnerInstance.LocalPlayer : PlayerRef.None);
			bool isLocalReady = gameManager.IsPlayerReady(localPlayerRef);

			if (_readyUpBodyText != null)
			{
				_readyUpBodyText.text = SharedTutorialText;
			}

			if (_readyUpStatusText != null)
			{
				_readyUpStatusText.text = $"Sẵn sàng: {gameManager.ReadyPlayerCount}/{gameManager.ActivePlayerCount}";
			}

			if (_readyUpConfirmButton != null)
			{
				_readyUpConfirmButton.interactable = isLocalReady == false;
			}

			if (_readyUpConfirmLabel != null)
			{
				_readyUpConfirmLabel.text = isLocalReady ? "Bạn đã sẵn sàng" : "Đã hiểu / Sẵn sàng";
			}

			HideMenuPanels();
			if (IsMenuOpen)
			{
				SetMenuVisible(false);
			}

			SetCursorState(false);
		}

		private void ConfirmReadyUp()
		{
			var gameManager = FindObjectOfType<SigilWarGameManager>();
			if (gameManager == null || _runnerInstance == null)
				return;

			var localPlayer = GetLocalPlayer();
			PlayerRef localPlayerRef = localPlayer != null ? localPlayer.OwnerPlayerRef : _runnerInstance.LocalPlayer;
			gameManager.SetPlayerReady(localPlayerRef, true);
		}

		private void RefreshEliminationOverlay(SigilWarGameManager gameManager)
		{
			if (_eliminationRoot == null)
				return;

			bool visible = false;
			if (IsConnected && gameManager != null && gameManager.IsReadyUpActive == false && gameManager.CurrentPhase != MatchPhase.MatchEnded)
			{
				var localPlayer = GetLocalPlayer();
				if (localPlayer != null && localPlayer.Health != null)
				{
					visible = localPlayer.Health.IsAlive == false && gameManager.IsRespawnAllowedInCurrentPhase == false;
				}
			}

			_eliminationRoot.SetActive(visible);

			if (visible)
			{
				HideMenuPanels();
				if (IsMenuOpen)
				{
					SetMenuVisible(false);
				}

				SetCursorState(false);
			}
		}

		private void ToggleMenuPanel(GameObject targetPanel)
		{
			if (targetPanel == null)
				return;

			bool shouldOpen = targetPanel.activeSelf == false;
			HideMenuPanels();
			targetPanel.SetActive(shouldOpen);
		}

		private void HideMenuPanels()
		{
			if (_settingsPanel != null)
			{
				_settingsPanel.SetActive(false);
			}

			if (_tutorialPanel != null)
			{
				_tutorialPanel.SetActive(false);
			}

			if (_creditsPanel != null)
			{
				_creditsPanel.SetActive(false);
			}
		}

		private void ApplyMasterVolumeFromSlider(float value)
		{
			ApplyMasterVolume(value);
			PlayerPrefs.SetFloat(MasterVolumePrefsKey, value);
			PlayerPrefs.Save();
		}

		private void ApplyMasterVolume(float value)
		{
			AudioListener.volume = Mathf.Clamp01(value);

			if (_volumeValueText != null)
			{
				_volumeValueText.text = $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
			}
		}

		private void UpdateHudTexts(SigilWarGameManager gameManager, SigilWarPlayer localPlayer)
		{
			if (_phaseText != null)
			{
				_phaseText.text = $"Phase: {FormatPhase(gameManager.CurrentPhase)}";
			}

			if (_objectiveText != null)
			{
				_objectiveText.text = BuildObjectiveText(gameManager, localPlayer);
			}

			if (_timerText != null)
			{
				float time = gameManager.CurrentPhase == MatchPhase.CorePhase && gameManager.CurrentCoreHolder != PlayerRef.None
					? gameManager.RemainingCoreControlTime
					: gameManager.RemainingPhaseTime;
				string label = gameManager.CurrentPhase == MatchPhase.CorePhase && gameManager.CurrentCoreHolder != PlayerRef.None
					? "Core"
					: "Phase";
				_timerText.text = $"{label} {FormatTime(time)}";
			}

			if (_statsText != null)
			{
				if (localPlayer != null && localPlayer.Health != null)
				{
					_statsText.text =
						$"HP {localPlayer.Health.CurrentHealth}/{localPlayer.Health.MaxHealth}    " +
						$"Kills {localPlayer.PlayerKills}    Pickups {localPlayer.CollectedPickups}";
				}
				else
				{
					_statsText.text = "Waiting for local player spawn...";
				}
			}
		}

		private string BuildObjectiveText(SigilWarGameManager gameManager, SigilWarPlayer localPlayer)
		{
			switch (gameManager.CurrentPhase)
			{
				case MatchPhase.Preparation:
					return "Prepare yourself. The lane clash begins soon.";
				case MatchPhase.LanePhase:
					return "Push your lane, survive enemy pressure, and collect momentum.";
				case MatchPhase.PortalPhase:
					return "Portals are open. Contest the arena and watch for the boss.";
				case MatchPhase.CorePhase:
				{
					if (gameManager.CurrentCoreHolder == PlayerRef.None)
						return "Capture the core objective. Hold it long enough to win.";

					if (localPlayer != null && gameManager.CurrentCoreHolder == localPlayer.OwnerPlayerRef)
						return "You control the core. Stay alive until the timer ends.";

					return $"An opponent controls the core. Stop them before {FormatTime(gameManager.RemainingCoreControlTime)}.";
				}
				case MatchPhase.MatchEnded:
					return "The match is over.";
				default:
					return "Waiting for match start.";
			}
		}

		private (string title, string body) BuildMatchEndMessage(SigilWarGameManager gameManager, SigilWarPlayer localPlayer)
		{
			bool localPlayerWon = localPlayer != null && gameManager.Winner == localPlayer.OwnerPlayerRef;

			switch (gameManager.VictoryReason)
			{
				case VictoryType.CoreControl:
					return localPlayerWon
						? ("Victory", "You controlled the core long enough and won the match.")
						: ("Defeat", "Another player secured the core before you could stop them.");
				case VictoryType.LastSurvivor:
					return localPlayerWon
						? ("Victory", "You were the last surviving player in the arena.")
						: ("Defeat", "Another player outlasted everyone else.");
				case VictoryType.TimeOut:
					return gameManager.Winner == PlayerRef.None
						? ("Time Out", "The match timer expired before anyone claimed victory.")
						: localPlayerWon
							? ("Victory", "Time expired and you were declared the winner.")
							: ("Match Ended", "Time expired and another player was declared the winner.");
				default:
					return ("Match Ended", "The battle has concluded.");
			}
		}

		private SigilWarPlayer GetLocalPlayer()
		{
			if (_runnerInstance == null)
				return null;

			var playerObject = _runnerInstance.GetPlayerObject(_runnerInstance.LocalPlayer);
			return playerObject != null ? playerObject.GetComponent<SigilWarPlayer>() : null;
		}

		private void SetMatchEndVisible(bool isVisible, string title, string body)
		{
			_isMatchEndVisible = isVisible;

			if (_matchEndRoot != null)
			{
				_matchEndRoot.SetActive(isVisible);
			}

			if (isVisible == false)
				return;

			if (_matchEndTitleText != null)
			{
				_matchEndTitleText.text = title;
			}

			if (_matchEndBodyText != null)
			{
				_matchEndBodyText.text = body;
			}

			if (IsMenuOpen)
			{
				SetMenuVisible(false);
			}

			SetCursorState(false);
		}

		private static string FormatPhase(MatchPhase phase)
		{
			return phase switch
			{
				MatchPhase.Preparation => "Preparation",
				MatchPhase.LanePhase => "Lane Clash",
				MatchPhase.PortalPhase => "Portal Surge",
				MatchPhase.CorePhase => "Core Control",
				MatchPhase.MatchEnded => "Match Ended",
				_ => "Waiting",
			};
		}

		private static string FormatTime(float seconds)
		{
			int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
			int minutes = totalSeconds / 60;
			int remainingSeconds = totalSeconds % 60;
			return $"{minutes:00}:{remainingSeconds:00}";
		}

		private static RectTransform CreateStretchRect(string name, RectTransform parent)
		{
			var gameObject = new GameObject(name, typeof(RectTransform));
			var rectTransform = gameObject.GetComponent<RectTransform>();
			rectTransform.SetParent(parent, false);
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			rectTransform.localScale = Vector3.one;
			return rectTransform;
		}

		private static RectTransform CreatePanel(
			RectTransform parent,
			string name,
			Vector2 anchorMin,
			Vector2 anchorMax,
			Vector2 anchoredPosition,
			Vector2 sizeDelta,
			Color color)
		{
			var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
			var rectTransform = gameObject.GetComponent<RectTransform>();
			rectTransform.SetParent(parent, false);
			rectTransform.anchorMin = anchorMin;
			rectTransform.anchorMax = anchorMax;
			rectTransform.anchoredPosition = anchoredPosition;
			rectTransform.sizeDelta = sizeDelta;
			rectTransform.localScale = Vector3.one;

			var image = gameObject.GetComponent<Image>();
			image.color = color;
			return rectTransform;
		}

		private static TextMeshProUGUI CreateText(
			RectTransform parent,
			string name,
			string text,
			float fontSize,
			FontStyles fontStyle,
			TextAlignmentOptions alignment,
			Vector2 anchorMin,
			Vector2 anchorMax,
			Vector2 offsetMin,
			Vector2 offsetMax)
		{
			var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
			var rectTransform = gameObject.GetComponent<RectTransform>();
			rectTransform.SetParent(parent, false);
			rectTransform.anchorMin = anchorMin;
			rectTransform.anchorMax = anchorMax;
			rectTransform.offsetMin = offsetMin;
			rectTransform.offsetMax = offsetMax;
			rectTransform.localScale = Vector3.one;

			var textComponent = gameObject.GetComponent<TextMeshProUGUI>();
			textComponent.text = text;
			textComponent.fontSize = fontSize;
			textComponent.fontStyle = fontStyle;
			textComponent.alignment = alignment;
			textComponent.color = Color.white;
			textComponent.enableWordWrapping = true;
			textComponent.raycastTarget = false;
			if (TMP_Settings.defaultFontAsset != null)
			{
				textComponent.font = TMP_Settings.defaultFontAsset;
			}

			return textComponent;
		}

		private static RectTransform CreateButton(
			RectTransform parent,
			string name,
			string label,
			Vector2 anchorMin,
			Vector2 anchorMax,
			Vector2 anchoredPosition,
			Vector2 sizeDelta,
			Color backgroundColor)
		{
			var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
			var rectTransform = buttonObject.GetComponent<RectTransform>();
			rectTransform.SetParent(parent, false);
			rectTransform.anchorMin = anchorMin;
			rectTransform.anchorMax = anchorMax;
			rectTransform.anchoredPosition = anchoredPosition;
			rectTransform.sizeDelta = sizeDelta;
			rectTransform.localScale = Vector3.one;

			var image = buttonObject.GetComponent<Image>();
			image.color = backgroundColor;

			var button = buttonObject.GetComponent<Button>();
			var colors = button.colors;
			colors.normalColor = backgroundColor;
			colors.highlightedColor = backgroundColor * 1.08f;
			colors.pressedColor = backgroundColor * 0.92f;
			colors.selectedColor = colors.highlightedColor;
			colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.45f);
			button.colors = colors;

			CreateText(
				rectTransform,
				"Label",
				label,
				24,
				FontStyles.Bold,
				TextAlignmentOptions.Center,
				Vector2.zero,
				Vector2.one,
				new Vector2(12f, 6f),
				new Vector2(-12f, -6f));

			return rectTransform;
		}

		private static GameObject CreateModalPanel(RectTransform parent, string name, string title, string body)
		{
			var root = CreatePanel(
				parent,
				name,
				new Vector2(0.5f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new Vector2(0f, 0f),
				new Vector2(640f, 360f),
				new Color(0.08f, 0.10f, 0.14f, 0.97f)).gameObject;

			CreateText(
				root.transform as RectTransform,
				"Title",
				title,
				30,
				FontStyles.Bold,
				TextAlignmentOptions.Center,
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
				new Vector2(24f, -28f),
				new Vector2(-24f, 76f));

			CreateText(
				root.transform as RectTransform,
				"Body",
				body,
				20,
				FontStyles.Normal,
				TextAlignmentOptions.TopLeft,
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				new Vector2(28f, 84f),
				new Vector2(-28f, -86f));

			return root;
		}

		private static Slider CreateSlider(
			RectTransform parent,
			string name,
			Vector2 anchorMin,
			Vector2 anchorMax,
			Vector2 offsetMin,
			Vector2 offsetMax)
		{
			var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
			var rootRect = root.GetComponent<RectTransform>();
			rootRect.SetParent(parent, false);
			rootRect.anchorMin = anchorMin;
			rootRect.anchorMax = anchorMax;
			rootRect.offsetMin = offsetMin;
			rootRect.offsetMax = offsetMax;
			rootRect.localScale = Vector3.one;

			var background = CreatePanel(
				rootRect,
				"Background",
				new Vector2(0f, 0.5f),
				new Vector2(1f, 0.5f),
				Vector2.zero,
				new Vector2(0f, 12f),
				new Color(1f, 1f, 1f, 0.16f));

			var fillArea = CreateStretchRect("Fill Area", rootRect);
			fillArea.offsetMin = new Vector2(0f, 0f);
			fillArea.offsetMax = new Vector2(-18f, 0f);

			var fill = CreatePanel(
				fillArea,
				"Fill",
				new Vector2(0f, 0.5f),
				new Vector2(1f, 0.5f),
				Vector2.zero,
				new Vector2(0f, 12f),
				new Color(0.26f, 0.68f, 0.46f, 1f));

			var handleArea = CreateStretchRect("Handle Slide Area", rootRect);
			handleArea.offsetMin = new Vector2(0f, -8f);
			handleArea.offsetMax = Vector2.zero;

			var handle = CreatePanel(
				handleArea,
				"Handle",
				new Vector2(0f, 0.5f),
				new Vector2(0f, 0.5f),
				Vector2.zero,
				new Vector2(24f, 24f),
				new Color(0.92f, 0.95f, 1f, 1f));

			var slider = root.GetComponent<Slider>();
			slider.minValue = 0f;
			slider.maxValue = 1f;
			slider.value = 1f;
			slider.targetGraphic = handle.GetComponent<Image>();
			slider.fillRect = fill;
			slider.handleRect = handle;
			slider.direction = Slider.Direction.LeftToRight;

			background.SetAsFirstSibling();
			return slider;
		}
	}
}
