using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
		public int MaxPlayerCount = 8;

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

		private NetworkRunner _runnerInstance;
		private static string _shutdownStatus;

		public bool IsMenuOpen => PanelGroup != null && PanelGroup.gameObject.activeSelf;
		public bool IsConnected => _runnerInstance != null;

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
				PanelGroup.gameObject.SetActive(false);
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

			if (PanelGroup.gameObject.activeSelf && _runnerInstance == null)
				return;

			PanelGroup.gameObject.SetActive(!PanelGroup.gameObject.activeSelf);
		}

		private void OnEnable()
		{
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
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
			{
				TogglePanelVisibility();
			}

			RefreshPanelState();
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
				SetCursorState(false);
				return;
			}

			bool isPanelVisible = PanelGroup.gameObject.activeSelf;

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

			SetCursorState(isPanelVisible == false);
		}

		private static void SetCursorState(bool lockCursor)
		{
			Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !lockCursor;
		}
	}
}
