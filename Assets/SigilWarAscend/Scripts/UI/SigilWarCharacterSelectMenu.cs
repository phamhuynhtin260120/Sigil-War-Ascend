using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SigilWarAscend.UI
{
	/// <summary>
	/// Inspector-driven Character Select controller.
	/// The scene owns the UI layout and this script only binds data + button actions.
	/// </summary>
	public sealed class SigilWarCharacterSelectMenu : MonoBehaviour
	{
		[Header("Scene Flow")]
		public string MainMenuSceneName = "MainMenu";
		public string GameplaySceneName = "GamePlay";

		[Header("UI References")]
		public TextMeshProUGUI SubtitleText;
		public TextMeshProUGUI SelectionText;
		public Button ContinueButton;

		[Header("Character Buttons")]
		public Button BladeDancerButton;
		public Button WardenButton;
		public Button ArcanistButton;
		public Button BackButton;

		private string _selectedCharacterId = string.Empty;

		private void Awake()
		{
			ValidateLaunchData();
			WireButtons();
			RefreshTexts();
		}

		private void ValidateLaunchData()
		{
			if (SigilWarSessionData.LaunchData.HasPendingLaunch)
				return;

			SigilWarSessionData.SetPendingStatus("No launch data found. Returning to Main Menu.");
			SigilWarSessionData.SetReturnToMainMenuReason("MissingLaunchData");
			SceneManager.LoadScene(MainMenuSceneName);
		}

		private void WireButtons()
		{
			RegisterButton(BladeDancerButton, () => SelectCharacter("BladeDancer"));
			RegisterButton(WardenButton, () => SelectCharacter("Warden"));
			RegisterButton(ArcanistButton, () => SelectCharacter("Arcanist"));
			RegisterButton(BackButton, BackToMainMenu);
			RegisterButton(ContinueButton, ContinueToGameplay);
		}

		private void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
		{
			if (button == null)
				return;

			button.onClick.RemoveListener(action);
			button.onClick.AddListener(action);
		}

		private void SelectCharacter(string characterId)
		{
			_selectedCharacterId = characterId;
			SigilWarSessionData.ApplyCharacterSelection(characterId);

			RefreshTexts();

			if (ContinueButton != null)
			{
				ContinueButton.interactable = true;
			}
		}

		private void ContinueToGameplay()
		{
			if (string.IsNullOrWhiteSpace(_selectedCharacterId))
				return;

			SigilWarSessionData.MarkLaunchReady(GameplaySceneName);
			SigilWarSessionData.SetSceneFlow("CharacterSelect", GameplaySceneName);
			SceneManager.LoadScene(GameplaySceneName);
		}

		private void BackToMainMenu()
		{
			SigilWarSessionData.SetSceneFlow("CharacterSelect", MainMenuSceneName);
			SceneManager.LoadScene(MainMenuSceneName);
		}

		private void RefreshTexts()
		{
			if (SubtitleText != null)
			{
				SubtitleText.text =
					$"Room: {SigilWarSessionData.LaunchData.RoomName}\n" +
					$"Player: {SigilWarSessionData.LaunchData.Nickname}";
			}

			if (SelectionText != null)
			{
				SelectionText.text = string.IsNullOrWhiteSpace(_selectedCharacterId)
					? "No character selected"
					: $"Selected: {_selectedCharacterId}";
			}

			if (ContinueButton != null)
			{
				ContinueButton.interactable = string.IsNullOrWhiteSpace(_selectedCharacterId) == false;
			}
		}
	}
}
