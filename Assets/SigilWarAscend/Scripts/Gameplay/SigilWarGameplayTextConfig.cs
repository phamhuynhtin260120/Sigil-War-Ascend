using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[CreateAssetMenu(
		fileName = "SigilWarGameplayTextConfig",
		menuName = "Sigil War Ascend/Config/Gameplay Text Config")]
	public sealed class SigilWarGameplayTextConfig : ScriptableObject
	{
		private const string DefaultResourcePath = "SigilWarGameplayTextConfig";

		private static SigilWarGameplayTextConfig _cachedDefault;

		[Header("Ready Up")]
		[TextArea(2, 4)]
		public string ReadyUpTitle = "Huong Dan Truoc Khi Vao Tran";

		[TextArea(8, 20)]
		public string ReadyUpInstructions =
			"LUAT CHOI SIGIL WAR ASCEND\n\n" +
			"1. Giai doan San sang:\n" +
			"- Tat ca nguoi choi doc huong dan va bam 'Da hieu / San sang'.\n" +
			"- Truoc khi tat ca cung san sang, nguoi choi khong the dieu khien nhan vat.\n\n" +
			"2. Preparation Phase:\n" +
			"- Tran dau bat dau dem nguoc.\n" +
			"- Giai doan nay duoc phep respawn.\n\n" +
			"3. Lane Phase:\n" +
			"- Nguoi choi di chuyen, tan cong va tranh chap khu vuc.\n" +
			"- Quai thuong xuat hien theo lane.\n" +
			"- Giai doan nay duoc phep respawn.\n\n" +
			"4. Portal Phase:\n" +
			"- Portal mo, boss va cac doi tuong tranh chap bat dau xuat hien.\n" +
			"- Nguoi choi van co the giao tranh va tiep tuc chiem uu the.\n" +
			"- Giai doan nay duoc phep respawn.\n\n" +
			"5. Core Phase:\n" +
			"- Core xuat hien. Nguoi nao giu Core du thoi gian quy dinh se thang.\n" +
			"- Giai doan nay khong duoc respawn.\n" +
			"- Neu bi ha guc trong giai doan nay, ban bi loai khoi tran.\n\n" +
			"6. Dieu kien thang:\n" +
			"- Giu Core du thoi gian.\n" +
			"- Hoac tro thanh nguoi song sot cuoi cung.\n\n" +
			"DIEU KHIEN:\n" +
			"WASD di chuyen | Shift chay | Space nhay | Chuot trai tan cong | ESC tam dung";

		public string ReadyUpWaitingStatus = "Dang cho nguoi choi san sang...";
		public string ReadyUpProgressFormat = "San sang: {0}/{1}";
		public string ReadyUpConfirmLabel = "Da hieu / San sang";
		public string ReadyUpConfirmedLabel = "Ban da san sang";

		[Header("Elimination")]
		public string EliminationTitle = "Ban da bi loai khoi tran";

		[TextArea(3, 6)]
		public string EliminationBody =
			"Giai doan hien tai khong cho phep hoi sinh. " +
			"Ban co the roi tran va quay ve phong tao room de bat dau lai.";

		public string ReturnToRoomLabel = "Quay ve phong tao room";

		[Header("Splash")]
		public string SplashTitle = "SIGIL WAR ASCEND";

		[TextArea(3, 6)]
		public string SplashBody =
			"Final Project Prototype\n" +
			"Multiplayer arena battle with phase-based objectives.\n\n" +
			"Tip: update this splash with your team name, logo, and class section before final submission.";

		public string SplashHint = "Press any key to continue";

		[Header("Tutorial Panel")]
		public string TutorialButtonLabel = "Tutorial";
		public string TutorialPanelTitle = "How To Play";

		[Header("Credits")]
		public string CreditsButtonLabel = "Credits";
		public string CreditsPanelTitle = "Credits";

		[TextArea(4, 10)]
		public string CreditsPanelBody =
			"Sigil War Ascend\nFinal project prototype\n\n" +
			"Suggested final credits format:\n" +
			"- Team name\n" +
			"- Member 1: Programming\n" +
			"- Member 2: Level Design\n" +
			"- Member 3: UI and Audio\n" +
			"- Asset acknowledgements\n\n" +
			"Replace this text with your real team information before submission.";
		
		public static SigilWarGameplayTextConfig LoadDefault()
		{
			if (_cachedDefault == null)
			{
				_cachedDefault = Resources.Load<SigilWarGameplayTextConfig>(DefaultResourcePath);
			}

			return _cachedDefault;
		}
	}
}
