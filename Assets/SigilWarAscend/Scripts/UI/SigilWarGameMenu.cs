using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SigilWarAscend.UI
{
	/// <summary>
	/// Main Menu / Lobby UI only.
	/// Other gameplay overlays are intentionally removed for now so we can rebuild the UI flow piece by piece.
	/// </summary>
	public sealed class SigilWarGameMenu : MonoBehaviour
	{
		[Header("Start Game Setup")]
		[Tooltip("Used by Fusion session properties so this game mode stays isolated from other sample rooms.")]
		public string GameModeIdentifier = "SigilWarAscend";
		public int MaxPlayerCount = 4;
		public string CharacterSelectSceneName = "CharacterSelect";
		public string GameplaySceneName = "GamePlay";

		[Header("Debug")]
		[Tooltip("For local iteration in editor without shared multiplayer.")]
		public bool ForceSinglePlayer;

		[Header("Main Menu UI")]
		public CanvasGroup PanelGroup;
		public TMP_InputField RoomText;
		public TMP_InputField NicknameText;
		public TextMeshProUGUI StatusText;
		public GameObject StartGroup;
		public GameObject DisconnectGroup;
		[Tooltip("Non-UI objects that should hide/show together with the menu, such as background video objects.")]
		public GameObject[] AdditionalMenuObjects;

		public bool IsMenuOpen => PanelGroup != null && PanelGroup.alpha > 0.001f && PanelGroup.blocksRaycasts;

		private void Awake()
		{
			ApplySavedNickname();
			ApplyShutdownStatus();
			EnsureDisconnectedMenuVisible();
		}

		private void OnEnable()
		{
			ApplySavedNickname();
			ApplyShutdownStatus();
			EnsureDisconnectedMenuVisible();
		}

		private void Start()
		{
			EnsureDisconnectedMenuVisible();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				StartGame();
			}

			RefreshMainMenuState();
		}

		public void StartGame()
		{
			string nickname = NicknameText != null ? NicknameText.text : string.Empty;
			string roomName = RoomText != null ? RoomText.text : string.Empty;
			PlayerPrefs.SetString(SigilWarPlayerPrefsKeys.PlayerName, nickname);
			PlayerPrefs.Save();

			GameMode requestedGameMode = Application.isEditor && ForceSinglePlayer ? GameMode.Single : GameMode.Shared;
			SigilWarSessionData.ConfigureMainMenuLaunch(roomName, nickname, GameModeIdentifier, MaxPlayerCount, requestedGameMode);
			SigilWarSessionData.SetSceneFlow("MainMenu", CharacterSelectSceneName);
			SetStatus("Loading character select...");
			SceneManager.LoadScene(CharacterSelectSceneName);
		}

		public void DisconnectClicked()
		{
			SigilWarSessionData.ResetAll();
			SetStatus(string.Empty);
			EnsureDisconnectedMenuVisible();
		}

		public void TogglePanelVisibility()
		{
			if (PanelGroup == null)
				return;

			SetMenuVisible(!IsMenuOpen);
		}

		private void RefreshMainMenuState()
		{
			bool isMenuVisible = IsMenuOpen;
			RefreshMainMenuStateCore(isMenuVisible);
		}

		private void SetMenuVisible(bool isVisible)
		{
			if (PanelGroup == null)
			{
				SetCursorState(isVisible == false);
				return;
			}

			PanelGroup.alpha = isVisible ? 1f : 0f;
			PanelGroup.interactable = isVisible;
			PanelGroup.blocksRaycasts = isVisible;
			RefreshMainMenuState();
		}

		private void EnsureDisconnectedMenuVisible()
		{
			if (PanelGroup != null)
			{
				PanelGroup.alpha = 1f;
				PanelGroup.interactable = true;
				PanelGroup.blocksRaycasts = true;
			}

			RefreshMainMenuStateCore(isMenuVisible: true);
		}

		private void RefreshMainMenuStateCore(bool isMenuVisible)
		{
			if (StartGroup != null)
			{
				StartGroup.SetActive(isMenuVisible);
			}

			if (DisconnectGroup != null)
			{
				DisconnectGroup.SetActive(false);
			}

			if (RoomText != null)
			{
				RoomText.interactable = true;
			}

			if (NicknameText != null)
			{
				NicknameText.interactable = true;
			}

			if (AdditionalMenuObjects != null)
			{
				for (int i = 0; i < AdditionalMenuObjects.Length; i++)
				{
					if (AdditionalMenuObjects[i] != null)
					{
						AdditionalMenuObjects[i].SetActive(isMenuVisible);
					}
				}
			}

			SetCursorState(isMenuVisible == false);
		}

		private void ApplySavedNickname()
		{
			if (NicknameText == null)
				return;

			string nickname = PlayerPrefs.GetString(SigilWarPlayerPrefsKeys.PlayerName);
			if (string.IsNullOrWhiteSpace(nickname))
			{
				nickname = "Player" + Random.Range(10000, 100000);
			}

			NicknameText.text = nickname;
		}

		private void ApplyShutdownStatus()
		{
			string pendingStatus = SigilWarSessionData.ConsumePendingStatus();
			SetStatus(string.IsNullOrEmpty(pendingStatus) ? string.Empty : pendingStatus);
		}

		private void SetStatus(string message)
		{
			if (StatusText != null)
			{
				StatusText.text = message;
			}
		}

		private static void SetCursorState(bool lockCursor)
		{
			Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !lockCursor;
		}
	}
}
