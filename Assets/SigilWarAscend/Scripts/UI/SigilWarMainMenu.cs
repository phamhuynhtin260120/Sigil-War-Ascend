using UnityEngine;
using UnityEngine.SceneManagement;

namespace SigilWarAscend.UI
{
	/// <summary>
	/// Sigil War specific main menu scene helper.
	/// Kept separate from the Fusion sample class so the project owns its menu flow.
	/// </summary>
	public sealed class SigilWarMainMenu : MonoBehaviour
	{
		public void LoadScene(int buildIndex)
		{
			SceneManager.LoadScene(buildIndex);
		}

		public void QuitGame()
		{
			Application.Quit();

			#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
			#endif
		}

		private void OnEnable()
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
}
