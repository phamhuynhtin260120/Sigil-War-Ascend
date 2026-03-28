# Sigil War Ascend - Project Tracking

## Muc Dich Cua File Nay

File nay duoc tao de thong ke va chot lai:

- nhung gi da lam
- nhung gi dang lam
- flow scene/UI ma du an dang theo duoi
- cac quy tac da thong nhat trong qua trinh phat trien
- chuc nang chi tiet cua tung scene
- setup hien tai can nho khi tiep tuc lam viec

File nay can duoc cap nhat dinh ky khi flow hoac kien truc du an thay doi.

## Requirement Dang Follow

Du an dang follow file yeu cau:

- `C:/Users/TDC/Downloads/1. Game Final Project Requirement.xlsx`

Nhung nhom tieu chi dang bam theo:

- Gameplay
- Level Design
- Graphics & Animation
- UI/UX
- Audio
- Polish
- Technical & Performance

## Nguyen Tac Lam Viec Da Thong Nhat

1. Uu tien su dung lai code da co.
2. Chi update file cu khi co the, han che xay lai tu dau.
3. Chi tao file moi khi that su co mot trach nhiem moi ro rang.
4. Mọi thay doi phai bam theo requirement, khong chi can "chay duoc".
5. Network state chi nen song trong `GamePlay`.
6. Cac scene truoc khi vao tran chi giu input data, khong giu gameplay state thuc chien.
7. ScriptableObject duoc dung cho config/data mac dinh, khong thay the runtime network state.
8. UI se duoc lam lai theo tung buoc, tung scene, tranh don qua nhieu logic vao mot script.
9. Canvas/UI phai phan tang hop ly, tranh flow de UI che nhau.
10. Giu flow scene ro rang de phu hop checklist Polish va UI/UX.

## Flow Tong The Dang Theo Duoi

Flow muc tieu cua du an:

1. `MainMenu`
2. `Select`
3. `GamePlay`
4. `Result`

Trang thai hien tai:

- `MainMenu`: da co va dang hoat dong
- `Select`: chua lam
- `GamePlay`: da co va dang hoat dong o muc gameplay core
- `Result`: chua tach thanh scene rieng

## Scene Va Trach Nhiem

### 1. MainMenu Scene

File scene:

- `Assets/Scenes/MainMenu.unity`

Muc dich:

- scene dau vao cua tro choi
- nhap `Room Name`
- nhap `Nickname`
- bat dau flow vao game
- hien status khi quay ve tu `GamePlay`

Script chiu trach nhiem chinh:

- [SigilWarGameMenu.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarGameMenu.cs)
- [SigilWarSessionData.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarSessionData.cs)
- [SigilWarMainMenu.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarMainMenu.cs)
- [SigilWarPlayerPrefsKeys.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarPlayerPrefsKeys.cs)

Script lien quan:

- [SigilWarGameMenu.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarGameMenu.cs)
  - doc va luu nickname
  - doc room name
  - tao launch data qua `SigilWarSessionData`
  - chuyen scene sang `GamePlay`
  - hien pending status khi quay lai menu

- [SigilWarSessionData.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarSessionData.cs)
  - giu du lieu tam thoi giua cac scene
  - hien tai dang giu:
    - `RoomName`
    - `Nickname`
    - `GameModeIdentifier`
    - `MaxPlayerCount`
    - `RequestedGameMode`
    - `PendingStatusMessage`

- [SigilWarMainMenu.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarMainMenu.cs)
  - helper don gian cho menu scene
  - quit game
  - unlock cursor khi vao menu

Setup hien tai da xac nhan:

- `MainMenu` da co `EventSystem`
- `MainMenu` da co `Main Camera`
- `MainMenu` da co object `SigilWarGameMenu`
- nut `Start` dang bind dung vao `SigilWarGameMenu.StartGame()`
- `Build Settings` da co `MainMenu` o index 0

### 2. Select Scene

Trang thai:

- chua tao xong
- la scene se duoc them tiep theo

Muc dich du kien:

- cho phep nguoi choi chon nhan vat
- co the them mo ta ky nang / role / chi so co ban
- xac nhan lua chon truoc khi vao `GamePlay`

Yeu cau kien truc:

- khong start network tai day
- chi ghi lua chon vao `SigilWarSessionData`
- sau do moi load sang `GamePlay`

Script du kien su dung:

- mo rong [SigilWarSessionData.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarSessionData.cs)
- co the can 1 script moi rieng cho scene `Select`

### 3. GamePlay Scene

File scene:

- `Assets/Scenes/GamePlay.unity`

Muc dich:

- tao `NetworkRunner`
- start Fusion session
- giu toan bo gameplay state
- quan ly phase match
- ready-up
- spawn player
- combat
- respawn
- victory / defeat

Script chiu trach nhiem chinh:

- [SigilWarGameplayBootstrap.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarGameplayBootstrap.cs)
- [SigilWarGameManager.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.cs)
- [SigilWarGameManager.ReadyUp.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.ReadyUp.cs)
- [SigilWarGameManager.Phases.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.Phases.cs)
- [SigilWarGameManager.Respawn.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.Respawn.cs)
- [SigilWarGameManager.Victory.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.Victory.cs)
- [SigilWarGameManager.World.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.World.cs)
- [SigilWarGameManager.Logging.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.Logging.cs)
- [SigilWarGameManager.Encounters.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.Encounters.cs)

Script gameplay lien quan:

- [SigilWarPlayer.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayer.cs)
- [SigilWarPlayerInput.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayerInput.cs)
- [SigilWarPlayerMovement.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayerMovement.cs)
- [SigilWarPlayerCombat.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayerCombat.cs)
- [SigilWarPlayerVfx.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayerVfx.cs)
- [SigilWarHealth.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarHealth.cs)
- [SigilWarHealthBar.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarHealthBar.cs)
- [SigilWarNameplate.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarNameplate.cs)

Script UI hien tai trong `GamePlay`:

- [SigilWarReadyUpUiController.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarReadyUpUiController.cs)

Script config hien tai:

- [SigilWarGameplayTextConfig.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarGameplayTextConfig.cs)
- [SigilWarMatchRulesConfig.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarMatchRulesConfig.cs)

Setup hien tai da xac nhan:

- `GamePlay` da co `Main Camera` va tag `MainCamera`
- `GamePlay` da co object `SigilWarGameplayBootstrap`
- `SigilWarGameplayBootstrap` da duoc gan `RunnerPrefab`
- `MainMenuSceneName` cua bootstrap dang la `MainMenu`
- `GamePlay` dang co `Managers / Spawners / Objectives`
- `GameManager` dang ton tai trong scene thong qua prefab instance

Flow hien tai trong `GamePlay`:

1. scene duoc load tu `MainMenu`
2. [SigilWarGameplayBootstrap.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarGameplayBootstrap.cs) doc `SigilWarSessionData`
3. bootstrap tao `NetworkRunner`
4. bootstrap start Fusion session
5. `GameManager` vao `ReadyUp`
6. [SigilWarReadyUpUiController.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarReadyUpUiController.cs) hien overlay runtime
7. nguoi choi bam ready
8. player local duoc spawn
9. `GameManager.SetPlayerReady(...)`
10. khi tat ca san sang, tran dau moi bat dau

### 4. Result Scene

Trang thai:

- chua tao xong
- hien tai ket qua cuoi tran van chua duoc tach thanh 1 scene rieng

Muc dich du kien:

- hien ket qua `Victory / Defeat / Draw`
- co thong tin tong ket tran
- cho nguoi choi:
  - choi lai
  - quay ve `MainMenu`

Yeu cau kien truc:

- scene nay la scene hien thi ket qua, khong giu network state chinh
- neu can du lieu ket qua, se truyen qua session data hoac 1 result data object toi gian

## Cac File Chiu Trach Nhiem Theo Scene

### MainMenu

- [SigilWarGameMenu.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarGameMenu.cs)
- [SigilWarSessionData.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarSessionData.cs)
- [SigilWarMainMenu.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarMainMenu.cs)
- [SigilWarPlayerPrefsKeys.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarPlayerPrefsKeys.cs)

### Select

- chua co file rieng
- se uu tien tan dung [SigilWarSessionData.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarSessionData.cs)
- co the can them 1 file moi cho scene select neu that su can

### GamePlay

- [SigilWarGameplayBootstrap.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarGameplayBootstrap.cs)
- [SigilWarReadyUpUiController.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarReadyUpUiController.cs)
- [SigilWarGameManager.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/GameManager/SigilWarGameManager.cs)
- cac partial cua `SigilWarGameManager`
- [SigilWarPlayer.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayer.cs)
- [SigilWarPlayerInput.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayerInput.cs)
- [SigilWarPlayerMovement.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayerMovement.cs)
- [SigilWarPlayerCombat.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/Player/SigilWarPlayerCombat.cs)
- [SigilWarHealth.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarHealth.cs)
- [SigilWarHealthBar.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarHealthBar.cs)
- [SigilWarGameplayTextConfig.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarGameplayTextConfig.cs)
- [SigilWarMatchRulesConfig.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarMatchRulesConfig.cs)

### Result

- chua co file rieng
- du kien se can 1 script moi toi gian de hien ket qua

## Nhung Viec Da Lam

- da phan tich requirement va doi chieu voi du an
- da tach `MainMenu` va `GamePlay` thanh 2 scene
- da them `MainMenu` vao `Build Settings`
- da chuyen flow sang:
  - `MainMenu` chi nhap du lieu
  - `GamePlay` moi start network
- da tao [SigilWarSessionData.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarSessionData.cs)
- da tao [SigilWarGameplayBootstrap.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarGameplayBootstrap.cs)
- da rut [SigilWarGameMenu.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarGameMenu.cs) ve dung vai tro `MainMenu`
- da fix logic de `MainMenu` hien dung khi vao scene
- da fix van de player local voi camera sau khi tach scene
- da dua text gameplay sang [SigilWarGameplayTextConfig.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarGameplayTextConfig.cs)
- da dua match rules sang [SigilWarMatchRulesConfig.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarMatchRulesConfig.cs)
- da dung lai `Ready Up UI` o `GamePlay` bang [SigilWarReadyUpUiController.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarReadyUpUiController.cs)
- da cho flow:
  - vao `GamePlay`
  - hien ready-up
  - bam ready
  - spawn player local
  - vao tran

## Nhung Viec Dang Lam

- chot va giu on dinh flow `MainMenu -> Select -> GamePlay -> Result`
- tiep tuc tai su dung code cu thay vi xay lai toan bo
- xac dinh ro trach nhiem theo tung scene
- chuan bi them `Select Scene`
- sau do se lam `Result Scene`

## Nhung Viec Chua Lam

- `Select Scene`
- `Result Scene`
- HUD in-game day du theo checklist
- Pause Menu hoan chinh
- setting, tutorial, credits day du theo flow moi
- audio system toi thieu theo requirement
- BGM theo scene
- SFX UI / combat / result day du
- object pooling
- polish flow ket thuc tran
- doi chieu tiep cac muc good/excellent trong requirement

## Setup Hien Tai Can Nho

### Build Settings

Thu tu hien tai:

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/GamePlay.unity`

### MainMenu

- `SigilWarGameMenu` dang duoc gan trong scene
- `Start` dang bind vao `StartGame()`
- status quay ve tu gameplay dang doc qua `SigilWarSessionData.ConsumePendingStatus()`

### GamePlay

- `SigilWarGameplayBootstrap` dang ton tai trong scene
- `RunnerPrefab` da duoc gan
- `Main Camera` co tag `MainCamera`
- `ReadyUp UI` dang duoc tao runtime neu scene chua co san
- `ReadyUp` hien tai doc text tu `SigilWarGameplayTextConfig`

### Match Rules / Text Config

- `GameManager` dang doc rule mac dinh tu:
  - [SigilWarMatchRulesConfig.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarMatchRulesConfig.cs)
- `ReadyUp` dang doc text tu:
  - [SigilWarGameplayTextConfig.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/Gameplay/SigilWarGameplayTextConfig.cs)

## Dinh Huong Cho Buoc Tiep Theo

Buoc hop ly nhat tiep theo:

1. mo rong [SigilWarSessionData.cs](D:/WordSpace/Game/Sigil-War-Ascend/Assets/SigilWarAscend/Scripts/UI/SigilWarSessionData.cs) de chua lua chon nhan vat
2. tao `Select Scene` voi muc tieu toi gian nhung ro rang
3. sua flow `MainMenu -> Select -> GamePlay`
4. sau do moi lam `Result Scene`

## Ghi Chu Kien Truc Quan Trong

- `MainMenu` va `Select` chi la noi thu thap du lieu dau vao
- `GamePlay` moi la noi giu network state, player state, match state
- `ScriptableObject` la noi chua config mac dinh
- `NetworkBehaviour` moi la noi chua runtime state trong tran
- can tiep tuc bam requirement de dam bao du an khong lech checklist cuoi ky
