using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	public sealed partial class SigilWarGameManager
	{
		[Header("Debug")]
		public bool EnableDebugLogs;
		public bool LogPhaseFlow = true;
		public bool LogRespawnFlow = true;
		public bool LogWorldState = true;
		public bool LogVictoryFlow = true;

		private const string LogPrefix = "[SigilWarGameManager]";

		private void LogPhase(string message)
		{
			if (EnableDebugLogs == false || LogPhaseFlow == false)
				return;

			Debug.Log($"{LogPrefix}[Phase] {message}", this);
		}

		private void LogRespawn(string message)
		{
			if (EnableDebugLogs == false || LogRespawnFlow == false)
				return;

			Debug.Log($"{LogPrefix}[Respawn] {message}", this);
		}

		private void LogWorld(string message)
		{
			if (EnableDebugLogs == false || LogWorldState == false)
				return;

			Debug.Log($"{LogPrefix}[World] {message}", this);
		}

		private void LogVictory(string message)
		{
			if (EnableDebugLogs == false || LogVictoryFlow == false)
				return;

			Debug.Log($"{LogPrefix}[Victory] {message}", this);
		}

		private string FormatPlayer(PlayerRef playerRef)
		{
			return playerRef == PlayerRef.None ? "None" : $"Player{playerRef.PlayerId}";
		}

		private string FormatTime(float seconds)
		{
			return $"{seconds:0.00}s";
		}
	}
}
