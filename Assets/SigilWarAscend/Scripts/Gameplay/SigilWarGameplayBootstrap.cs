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
		private SigilWarGameplayHudController _gameplayHudController;

		private async void Start()
		{
			if (SigilWarSessionData.LaunchData.HasPendingLaunch == false || SigilWarSessionData.LaunchData.IsLaunchDataComplete == false)
			{
				SigilWarSessionData.SetPendingStatus("Launch data is incomplete. Returning to main menu.");
				SigilWarSessionData.SetReturnToMainMenuReason("IncompleteLaunchData");
				SceneManager.LoadScene(MainMenuSceneName);
				return;
			}

			if (RunnerPrefab == null)
			{
				Debug.LogError("[SigilWarGameplayBootstrap] RunnerPrefab is missing.", this);
				SigilWarSessionData.SetPendingStatus("Runner Prefab is missing in Gameplay scene.");
				SigilWarSessionData.SetReturnToMainMenuReason("MissingRunnerPrefab");
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
				GameMode = SigilWarSessionData.LaunchData.RequestedGameMode,
				SessionName = SigilWarSessionData.LaunchData.RoomName,
				PlayerCount = SigilWarSessionData.LaunchData.MaxPlayerCount,
				SessionProperties = new Dictionary<string, SessionProperty>
				{
					["GameMode"] = SigilWarSessionData.LaunchData.GameModeIdentifier,
				},
				Scene = sceneInfo,
			};

			StartGameResult result = await _runnerInstance.StartGame(startArguments);
			if (result.Ok == false)
			{
				Debug.LogError($"[SigilWarGameplayBootstrap] Failed to start game: {result.ShutdownReason}", this);
				SigilWarSessionData.ClearLaunchData();
				SigilWarSessionData.SetPendingStatus($"Connection Failed: {result.ShutdownReason}");
				SigilWarSessionData.SetReturnToMainMenuReason(result.ShutdownReason.ToString());
				SceneManager.LoadScene(MainMenuSceneName);
				return;
			}

			BindSceneUiControllers();
			SigilWarSessionData.MarkMatchStarted();
			SigilWarSessionData.SetSceneFlow(SceneManager.GetActiveScene().name, string.Empty);
			SigilWarSessionData.MarkLaunchConsumed();
			SetGameplayCursorLocked(false);
		}

		private void Update()
		{
			if (_runnerInstance != null)
			{
				BindSceneUiControllers();
			}
		}

		private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
		{
			SigilWarSessionData.SetPendingStatus($"Shutdown: {reason}");
			SigilWarSessionData.SetReturnToMainMenuReason(reason.ToString());
			SceneManager.LoadScene(MainMenuSceneName);
		}

		private void BindSceneUiControllers()
		{
			if (_readyUpUiController == null)
			{
				_readyUpUiController = FindFirstObjectByType<SigilWarReadyUpUiController>();
			}

			if (_gameplayHudController == null)
			{
				_gameplayHudController = FindFirstObjectByType<SigilWarGameplayHudController>();
			}
		}

		private static void SetGameplayCursorLocked(bool isLocked)
		{
			Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !isLocked;
		}
	}
}
