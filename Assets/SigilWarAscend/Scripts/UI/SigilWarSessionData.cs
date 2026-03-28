using Fusion;

namespace SigilWarAscend.UI
{
	public static class SigilWarSessionData
	{
		public static bool HasPendingLaunch { get; private set; }
		public static string RoomName { get; private set; }
		public static string Nickname { get; private set; }
		public static string GameModeIdentifier { get; private set; }
		public static int MaxPlayerCount { get; private set; }
		public static GameMode RequestedGameMode { get; private set; }
		public static string PendingStatusMessage { get; private set; }

		public static void ConfigureLaunch(
			string roomName,
			string nickname,
			string gameModeIdentifier,
			int maxPlayerCount,
			GameMode requestedGameMode)
		{
			RoomName = roomName;
			Nickname = nickname;
			GameModeIdentifier = gameModeIdentifier;
			MaxPlayerCount = maxPlayerCount;
			RequestedGameMode = requestedGameMode;
			HasPendingLaunch = true;
		}

		public static void SetPendingStatus(string message)
		{
			PendingStatusMessage = message;
		}

		public static string ConsumePendingStatus()
		{
			string value = PendingStatusMessage;
			PendingStatusMessage = string.Empty;
			return value;
		}

		public static void Clear()
		{
			HasPendingLaunch = false;
			RoomName = string.Empty;
			Nickname = string.Empty;
			GameModeIdentifier = string.Empty;
			MaxPlayerCount = 0;
			RequestedGameMode = GameMode.Shared;
			PendingStatusMessage = string.Empty;
		}
	}
}
