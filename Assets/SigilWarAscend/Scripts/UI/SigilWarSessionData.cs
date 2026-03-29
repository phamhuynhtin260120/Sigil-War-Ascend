using Fusion;

namespace SigilWarAscend.UI
{
	public static class SigilWarSessionData
	{
		public sealed class SigilWarSessionLaunchData
		{
			public bool HasPendingLaunch { get; internal set; }
			public string RoomName { get; internal set; } = string.Empty;
			public string Nickname { get; internal set; } = string.Empty;
			public string GameModeIdentifier { get; internal set; } = string.Empty;
			public int MaxPlayerCount { get; internal set; }
			public GameMode RequestedGameMode { get; internal set; } = GameMode.Shared;
			public string SelectedCharacterId { get; internal set; } = string.Empty;
			public string SelectedRoleId { get; internal set; } = string.Empty;
			public string SelectedSkinId { get; internal set; } = string.Empty;
			public bool IsLaunchDataComplete { get; internal set; }
		}

		public sealed class SigilWarSceneFlowState
		{
			public string LastSceneName { get; internal set; } = string.Empty;
			public string NextSceneName { get; internal set; } = string.Empty;
			public string PendingStatusMessage { get; internal set; } = string.Empty;
			public string ReturnToMainMenuReason { get; internal set; } = string.Empty;
			public bool WasMatchStarted { get; internal set; }
			public bool ShouldOpenResultScene { get; internal set; }
		}

		public sealed class SigilWarMatchResultData
		{
			public bool HasMatchResult { get; internal set; }
			public int WinnerPlayerRefId { get; internal set; }
			public string WinnerNickname { get; internal set; } = string.Empty;
			public string VictoryReason { get; internal set; } = string.Empty;
			public bool LocalPlayerWon { get; internal set; }
			public int LocalPlayerKills { get; internal set; }
			public int LocalPlayerDeaths { get; internal set; }
			public int LocalPlayerPickups { get; internal set; }
			public string LocalPlayerCharacterId { get; internal set; } = string.Empty;
			public string MatchSummaryText { get; internal set; } = string.Empty;
		}

		public static SigilWarSessionLaunchData LaunchData { get; } = new SigilWarSessionLaunchData();
		public static SigilWarSceneFlowState FlowState { get; } = new SigilWarSceneFlowState();
		public static SigilWarMatchResultData ResultData { get; } = new SigilWarMatchResultData();

		public static bool HasPendingLaunch => LaunchData.HasPendingLaunch;
		public static string RoomName => LaunchData.RoomName;
		public static string Nickname => LaunchData.Nickname;
		public static string GameModeIdentifier => LaunchData.GameModeIdentifier;
		public static int MaxPlayerCount => LaunchData.MaxPlayerCount;
		public static GameMode RequestedGameMode => LaunchData.RequestedGameMode;

		public static void ConfigureMainMenuLaunch(
			string roomName,
			string nickname,
			string gameModeIdentifier,
			int maxPlayerCount,
			GameMode requestedGameMode)
		{
			LaunchData.RoomName = roomName ?? string.Empty;
			LaunchData.Nickname = nickname ?? string.Empty;
			LaunchData.GameModeIdentifier = gameModeIdentifier ?? string.Empty;
			LaunchData.MaxPlayerCount = maxPlayerCount;
			LaunchData.RequestedGameMode = requestedGameMode;
			LaunchData.HasPendingLaunch = true;
			LaunchData.IsLaunchDataComplete = false;
			FlowState.LastSceneName = "MainMenu";
		}

		public static void ApplyCharacterSelection(string selectedCharacterId, string selectedRoleId = "", string selectedSkinId = "")
		{
			LaunchData.SelectedCharacterId = selectedCharacterId ?? string.Empty;
			LaunchData.SelectedRoleId = selectedRoleId ?? string.Empty;
			LaunchData.SelectedSkinId = selectedSkinId ?? string.Empty;
		}

		public static void MarkLaunchReady(string nextSceneName)
		{
			LaunchData.HasPendingLaunch = true;
			LaunchData.IsLaunchDataComplete = true;
			FlowState.NextSceneName = nextSceneName ?? string.Empty;
		}

		public static void SetSceneFlow(string lastSceneName, string nextSceneName)
		{
			FlowState.LastSceneName = lastSceneName ?? string.Empty;
			FlowState.NextSceneName = nextSceneName ?? string.Empty;
		}

		public static void SetPendingStatus(string message)
		{
			FlowState.PendingStatusMessage = message ?? string.Empty;
		}

		public static string ConsumePendingStatus()
		{
			string value = FlowState.PendingStatusMessage;
			FlowState.PendingStatusMessage = string.Empty;
			return value;
		}

		public static void SetReturnToMainMenuReason(string reason)
		{
			FlowState.ReturnToMainMenuReason = reason ?? string.Empty;
		}

		public static void MarkMatchStarted()
		{
			FlowState.WasMatchStarted = true;
		}

		public static void StoreMatchResult(
			int winnerPlayerRefId,
			string winnerNickname,
			string victoryReason,
			bool localPlayerWon,
			int localPlayerKills,
			int localPlayerDeaths,
			int localPlayerPickups,
			string localPlayerCharacterId,
			string matchSummaryText)
		{
			ResultData.HasMatchResult = true;
			ResultData.WinnerPlayerRefId = winnerPlayerRefId;
			ResultData.WinnerNickname = winnerNickname ?? string.Empty;
			ResultData.VictoryReason = victoryReason ?? string.Empty;
			ResultData.LocalPlayerWon = localPlayerWon;
			ResultData.LocalPlayerKills = localPlayerKills;
			ResultData.LocalPlayerDeaths = localPlayerDeaths;
			ResultData.LocalPlayerPickups = localPlayerPickups;
			ResultData.LocalPlayerCharacterId = localPlayerCharacterId ?? string.Empty;
			ResultData.MatchSummaryText = matchSummaryText ?? string.Empty;
		}

		public static void ClearLaunchData()
		{
			LaunchData.HasPendingLaunch = false;
			LaunchData.RoomName = string.Empty;
			LaunchData.Nickname = string.Empty;
			LaunchData.GameModeIdentifier = string.Empty;
			LaunchData.MaxPlayerCount = 0;
			LaunchData.RequestedGameMode = GameMode.Shared;
			LaunchData.SelectedCharacterId = string.Empty;
			LaunchData.SelectedRoleId = string.Empty;
			LaunchData.SelectedSkinId = string.Empty;
			LaunchData.IsLaunchDataComplete = false;
		}

		public static void ClearResultData()
		{
			ResultData.HasMatchResult = false;
			ResultData.WinnerPlayerRefId = 0;
			ResultData.WinnerNickname = string.Empty;
			ResultData.VictoryReason = string.Empty;
			ResultData.LocalPlayerWon = false;
			ResultData.LocalPlayerKills = 0;
			ResultData.LocalPlayerDeaths = 0;
			ResultData.LocalPlayerPickups = 0;
			ResultData.LocalPlayerCharacterId = string.Empty;
			ResultData.MatchSummaryText = string.Empty;
		}

		public static void ResetAll()
		{
			ClearLaunchData();
			ClearResultData();
			FlowState.LastSceneName = string.Empty;
			FlowState.NextSceneName = string.Empty;
			FlowState.PendingStatusMessage = string.Empty;
			FlowState.ReturnToMainMenuReason = string.Empty;
			FlowState.WasMatchStarted = false;
			FlowState.ShouldOpenResultScene = false;
		}

		public static void Clear()
		{
			ResetAll();
		}
	}
}
