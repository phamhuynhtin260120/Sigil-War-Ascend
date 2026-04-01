using Fusion;
using SigilWarAscend.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SigilWarAscend.UI
{
	/// <summary>
	/// Scene-authored gameplay HUD binder.
	/// Assign the UI references from the Gameplay scene so the visual layout stays fully in the editor.
	/// </summary>
	public sealed class SigilWarGameplayHudController : MonoBehaviour
	{
		[Header("Scene UI")]
		[SerializeField] private CanvasGroup _rootGroup;
		[SerializeField] private TextMeshProUGUI _playerTitleText;
		[SerializeField] private TextMeshProUGUI _playerStatsText;
		[SerializeField] private Image _healthFillImage;
		[SerializeField] private TextMeshProUGUI _healthText;
		[SerializeField] private TextMeshProUGUI _matchTitleText;
		[SerializeField] private TextMeshProUGUI _matchStatsText;

		[Header("Respawn Overlay")]
		[SerializeField] private CanvasGroup _respawnOverlayGroup;
		[SerializeField] private TextMeshProUGUI _respawnTitleText;
		[SerializeField] private TextMeshProUGUI _respawnCountdownText;
		[SerializeField] private TextMeshProUGUI _respawnDetailText;

		private SigilWarGameManager _gameManager;
		private SigilWarPlayer _localPlayer;

		private void Awake()
		{
			SetVisible(false);
			SetRespawnOverlayVisible(false);
		}

		private void Update()
		{
			ResolveReferences();
			if (CanReadGameplayState(_gameManager) == false)
			{
				SetVisible(false);
				SetRespawnOverlayVisible(false);
				return;
			}

			if (_gameManager.IsReadyUpActive)
			{
				SetVisible(false);
				SetRespawnOverlayVisible(false);
				return;
			}

			bool canReadLocalPlayer = CanReadPlayerState(_localPlayer);
			if (canReadLocalPlayer == false)
			{
				SetVisible(false);
				SetRespawnOverlayVisible(false);
				return;
			}

			bool showRespawnOverlay = canReadLocalPlayer && _localPlayer.IsAlive == false && _localPlayer.IsRespawnPending;
			bool showSceneUi = showRespawnOverlay == false;

			SetVisible(showSceneUi);
			SetRespawnOverlayVisible(showRespawnOverlay);
			RefreshLocalPlayerPanel();
			RefreshMatchPanel();
			if (showRespawnOverlay)
			{
				RefreshRespawnOverlay();
			}
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
				_playerTitleText.text = CanReadPlayerState(_localPlayer) && string.IsNullOrWhiteSpace(_localPlayer.Nickname) == false
					? _localPlayer.Nickname
					: "Dang tim local player...";
			}

			if (_playerStatsText != null)
			{
				if (CanReadPlayerState(_localPlayer) == false)
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
				_healthText.text = CanReadPlayerState(_localPlayer) == false
					? "HP: --/--"
					: $"HP: {_localPlayer.CurrentHealth}/{Mathf.Max(_localPlayer.MaxHealth, 0)}";
			}

			if (_healthFillImage != null)
			{
				float fill = CanReadPlayerState(_localPlayer) ? Mathf.Clamp01(_localPlayer.HealthNormalized) : 0f;
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

		private void SetVisible(bool isVisible)
		{
			if (_rootGroup == null)
				return;

			_rootGroup.alpha = isVisible ? 1f : 0f;
			_rootGroup.interactable = isVisible;
			_rootGroup.blocksRaycasts = false;
		}

		private void RefreshRespawnOverlay()
		{
			if (CanReadPlayerState(_localPlayer) == false || _localPlayer.IsAlive || _localPlayer.IsRespawnPending == false)
				return;

			float remaining = _localPlayer.RemainingRespawnTime;
			if (_respawnTitleText != null)
			{
				_respawnTitleText.text = "You Are Defeated";
			}

			if (_respawnCountdownText != null)
			{
				_respawnCountdownText.text = FormatTime(remaining);
			}

			if (_respawnDetailText != null && _gameManager != null)
			{
				_respawnDetailText.text = $"Respawn in {FormatPhase(_gameManager.CurrentPhase)}";
			}
		}

		private void SetRespawnOverlayVisible(bool isVisible)
		{
			if (_respawnOverlayGroup == null)
				return;

			_respawnOverlayGroup.alpha = isVisible ? 1f : 0f;
			_respawnOverlayGroup.interactable = false;
			_respawnOverlayGroup.blocksRaycasts = false;
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

		private static bool CanReadGameplayState(SigilWarGameManager gameManager)
		{
			return gameManager != null &&
				gameManager.Object != null &&
				gameManager.Object.IsValid &&
				gameManager.Runner != null;
		}

		private static bool CanReadPlayerState(SigilWarPlayer player)
		{
			return player != null &&
				player.Object != null &&
				player.Object.IsValid &&
				player.Runner != null;
		}

	}
}
