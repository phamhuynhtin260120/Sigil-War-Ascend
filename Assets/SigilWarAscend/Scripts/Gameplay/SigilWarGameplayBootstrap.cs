using System.Collections.Generic;
using Fusion;
using SigilWarAscend.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Starts the network session after the Gameplay scene has loaded.
	/// </summary>
	public sealed class SigilWarGameplayBootstrap : MonoBehaviour
	{
		[Header("Network Setup")]
		public NetworkRunner RunnerPrefab;
		public string MainMenuSceneName = "MainMenu";

		private NetworkRunner _runnerInstance;
		private bool _isStarting;
		private SigilWarReadyUpUiController _readyUpUiController;

		private async void Start()
		{
			if (SigilWarSessionData.HasPendingLaunch == false)
			{
				SigilWarSessionData.SetPendingStatus("No pending launch session. Returning to main menu.");
				SceneManager.LoadScene(MainMenuSceneName);
				return;
			}

			if (RunnerPrefab == null)
			{
				Debug.LogError("[SigilWarGameplayBootstrap] RunnerPrefab is missing.", this);
				SigilWarSessionData.SetPendingStatus("Runner Prefab is missing in Gameplay scene.");
				SceneManager.LoadScene(MainMenuSceneName);
				return;
			}

			if (_isStarting)
				return;

			_isStarting = true;
			_runnerInstance = Instantiate(RunnerPrefab);

			NetworkEvents events = _runnerInstance.GetComponent<NetworkEvents>();
			if (events != null)
			{
				events.OnShutdown.AddListener(OnShutdown);
			}

			NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
			sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

			StartGameArgs startArguments = new StartGameArgs()
			{
				GameMode = SigilWarSessionData.RequestedGameMode,
				SessionName = SigilWarSessionData.RoomName,
				PlayerCount = SigilWarSessionData.MaxPlayerCount,
				SessionProperties = new Dictionary<string, SessionProperty>
				{
					["GameMode"] = SigilWarSessionData.GameModeIdentifier,
				},
				Scene = sceneInfo,
			};

			StartGameResult result = await _runnerInstance.StartGame(startArguments);
			if (result.Ok == false)
			{
				Debug.LogError($"[SigilWarGameplayBootstrap] Failed to start game: {result.ShutdownReason}", this);
				SigilWarSessionData.Clear();
				SigilWarSessionData.SetPendingStatus($"Connection Failed: {result.ShutdownReason}");
				SceneManager.LoadScene(MainMenuSceneName);
				return;
			}

			EnsureReadyUpUiController();
			SigilWarSessionData.Clear();
			SetGameplayCursorLocked(false);
		}

		private void Update()
		{
			if (_runnerInstance != null)
			{
				EnsureReadyUpUiController();
			}
		}

		private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
		{
			SigilWarSessionData.SetPendingStatus($"Shutdown: {reason}");
			SceneManager.LoadScene(MainMenuSceneName);
		}

		private void EnsureReadyUpUiController()
		{
			if (_readyUpUiController != null)
				return;

			_readyUpUiController = FindFirstObjectByType<SigilWarReadyUpUiController>();
			if (_readyUpUiController == null)
			{
				GameObject readyUpRoot = new GameObject("SigilWarReadyUpUi");
				_readyUpUiController = readyUpRoot.AddComponent<SigilWarReadyUpUiController>();
			}
		}

		private static void SetGameplayCursorLocked(bool isLocked)
		{
			Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !isLocked;
		}
	}
}
