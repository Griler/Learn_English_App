using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections; // Cần cho IEnumerator
using ExitGames.Client.Photon;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[System.Serializable]
public class userDataPVP
{
    public string name;
    public int rank;

    public userDataPVP()
    {
    }
}

public class GamePlayController : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI[] answerTexts; 
    
    [Header("UI Info")]
    public TextMeshProUGUI statusText; 
    public List<Image> myLives;
    public List<Image> enemyLives;
    public Sprite disableHeart;
    public Sprite enableHeart;
    public GameObject player1Container;
    public GameObject player2Container;
    
    public GameObject loadingPanel; // Panel che màn hình lúc tải
    public GameObject gameWinPanel; 
    public GameObject gameLosePanel; 
    public TextMeshProUGUI loadingStatusText;  // Text: "Đang tải...", "Đợi người khác..."
    public TextMeshProUGUI countdownText;

    private int rankChange = 0;
    [Header("Timer Settings")]
    public float timeLimit = 5f; 
    public TextMeshProUGUI timerText;

    private float currentTimer;
    private bool isTimerRunning = false;
    
    [Header("Game Data")]
    public List<QuestionData> allQuestions; 

    private int currentQuestionIndex = 0;
    private int currentTurnActorNumber;
    private bool isDataLoaded = false;
    private string matchId = "";
    private bool isGameStarted = false; // Cờ chặn gọi setup 2 lần
    
    [NotNull] private userDataPVP myPlayer = new userDataPVP();
    [NotNull] private userDataPVP otherPlayer = new userDataPVP();
    
    // Mạng của 2 người chơi (Key: ActorNumber, Value: Lives)
    private Dictionary<int, int> playerLives = new Dictionary<int, int>();
    DatabaseReference reference;
    [SerializeField] private List<QuestionData> rawAllQuestions = new List<QuestionData>();
    private bool isGameOver = false; 

    // --- [MỚI] HẰNG SỐ CHO BOT ---
    private const int BOT_ACTOR_NUMBER = 9999;

    private void Start()
    {
        Hashtable resetProps = new Hashtable();
        resetProps.Add("IsLoaded", false); 
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
        rankChange = 0;
        isGameOver = false; 

        // Setup Buttons
        for (var i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.RemoveAllListeners(); 
            answerButtons[i].onClick.AddListener(()=>
            {
                OnAnswerSelected(index);
            });
            answerButtons[i].gameObject.SetActive(false);
        }

        SetButtonsInteractable(false);
        
        // Setup UI Player (Bot hoặc Người)
        InitUIPlayer();

        // Load Data
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.INMATCH);
                LoadQuestionsFromFirebase();
            }
            else
            {
                Debug.LogError("Lỗi Firebase: " + dependencyStatus);
            }
        });
    }
    
    void LoadQuestionsFromFirebase() 
    {
        reference.Child("questions").Child("list_test") 
            .GetValueAsync().ContinueWithOnMainThread(task => {
            
            if (task.IsFaulted || !task.IsCompleted || task.Result.Value == null)
            {
                Debug.LogError("Lỗi tải câu hỏi Firebase!");
                return;
            }

            DataSnapshot snapshot = task.Result;
            rawAllQuestions = new List<QuestionData>(); 

            foreach (DataSnapshot child in snapshot.Children)
            {
                try 
                {
                    QuestionData newQ = new QuestionData();
                    if (child.Child("questionText").Value != null)
                        newQ.questionText = child.Child("questionText").Value.ToString();
                    if (child.Child("correctAnswerIdx").Value != null)
                        newQ.correctAnswerIdx = int.Parse(child.Child("correctAnswerIdx").Value.ToString());   
                    if (child.Child("id").Value != null)
                        newQ.id = int.Parse(child.Child("id").Value.ToString());

                    List<string> answersList = new List<string>();
                    foreach(DataSnapshot ans in child.Child("answers").Children)
                    {
                        answersList.Add(ans.Value.ToString());
                    }
                    newQ.answers = answersList.ToArray();
                    rawAllQuestions.Add(newQ);
                }
                catch (System.Exception ex) { Debug.LogWarning("Lỗi parse câu hỏi: " + ex.Message); }
            }
            
            Debug.Log($"Đã tải xong {rawAllQuestions.Count} câu hỏi.");
            isDataLoaded = true;

            // --- [LOGIC PHÂN NHÁNH: BOT vs NGƯỜI] ---
            if (BotMatchHelper.IsBotMatch)
            {
                // MODE BOT: Tự động Start luôn, không cần chờ RPC
                loadingStatusText.text = "Đang vào trận đấu tập...";
                int seed = UnityEngine.Random.Range(1, 9999);
                
                // Random ai đi trước (Mình hoặc Bot)
                bool playerGoFirst = (UnityEngine.Random.Range(0, 2) == 0);
                int startActor = playerGoFirst ? PhotonNetwork.LocalPlayer.ActorNumber : BOT_ACTOR_NUMBER;

                
                // Giả lập countdown chạy luôn
                RPC_StartCountdown();
            }
            else 
            {
                // MODE PVP: Báo server mình đã sẵn sàng và chờ đối thủ
                loadingStatusText.text = "Đang đợi người chơi khác...";
                Hashtable props = new Hashtable();
                props.Add("IsLoaded", true);
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }
        });
    }

    // --- RPC SETUP GAME ---
    // Sửa signature để nhận null info nếu gọi local
    [PunRPC] 
    void RPC_SetupAndStartGame(int seed, int startTurnActor)
    {
        if (isGameStarted) return;
        isGameStarted = true;
        
        UnityEngine.Random.InitState(seed);
        rawAllQuestions = rawAllQuestions.OrderBy(x => x.id).ToList();
        
        // Trộn câu hỏi theo Seed để 2 bên giống nhau
        allQuestions = rawAllQuestions.OrderBy(x =>
        {
            int random = UnityEngine.Random.Range(1, seed);
            return random;
        }).ToList();

        // Bật nút trả lời
        for (var i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(true);
        }

        // Init Logic Game
        InitGameLogic(startTurnActor);
    }
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Chỉ chạy khi đấu PvP Online
        if (BotMatchHelper.IsBotMatch) return;

        if (PhotonNetwork.IsMasterClient)
        {
            if (changedProps.ContainsKey("IsLoaded"))
            {
                CheckAllPlayersReady();
            }
        }
    }
    
    private void CheckAllPlayersReady()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isLoaded;
            if (p.CustomProperties.TryGetValue("IsLoaded", out isLoaded))
            {
                if (!(bool)isLoaded) return; 
            }
            else return; 
        }
        photonView.RPC("RPC_StartCountdown", RpcTarget.All);
    }
    
    [PunRPC]
    void RPC_StartCountdown()
    {
        StartCoroutine(Co_RunCountdownAndStart());
    }

    IEnumerator Co_RunCountdownAndStart()
    {
        loadingStatusText.text = "BẮT ĐẦU SAU:";
        countdownText.gameObject.SetActive(true);
        countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        countdownText.text = "1";
        yield return new WaitForSeconds(1f);
        
        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);
        loadingPanel.SetActive(false);
        if (rawAllQuestions.Count > 0)
        {
            int seed = UnityEngine.Random.Range(1, 9999);
            int startActor = PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)].ActorNumber;
            if (!BotMatchHelper.IsBotMatch && PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_SetupAndStartGame", RpcTarget.All, seed, startActor);
            }
            else if (BotMatchHelper.IsBotMatch && PhotonNetwork.IsMasterClient)
            {
                RPC_SetupAndStartGame(seed, startActor);
            }
        }
    }
    
    void InitGameLogic(int startActor)
    {
        playerLives.Clear();

        // 1. Setup Máu
        if (BotMatchHelper.IsBotMatch)
        {
            // Mode Bot: Thêm mình và Bot
            playerLives.Add(PhotonNetwork.LocalPlayer.ActorNumber, 3);
            playerLives.Add(BOT_ACTOR_NUMBER, 3);
        }
        else
        {
            // Mode PvP: Thêm tất cả người trong phòng
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if(!playerLives.ContainsKey(p.ActorNumber))
                    playerLives.Add(p.ActorNumber, 3);
            }
        }
        
        UpdateLivesUI();

        // 2. Thiết lập lượt đi
        currentTurnActorNumber = startActor;
        
        // 3. Load câu đầu tiên lên UI
        RPC_SyncState(0, currentTurnActorNumber);
        
        // --- [QUAN TRỌNG] KÍCH HOẠT BOT NẾU NÓ ĐI TRƯỚC ---
        if (BotMatchHelper.IsBotMatch && currentTurnActorNumber == BOT_ACTOR_NUMBER)
        {
            StartCoroutine(BotTurnRoutine());
        }
    }

    // --- PHẦN LOGIC NGƯỜI CHƠI ---
    
    void OnAnswerSelected(int index)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != currentTurnActorNumber)
        {
            return;
        }

        SetButtonsInteractable(false);

        // Nếu đấu Bot, mình chính là Master (Host), tự gửi cho mình
        // Nếu đấu PvP, gửi RPC lên Master
        photonView.RPC("RPC_SubmitAnswer", RpcTarget.MasterClient, index);
    }

    // --- PHẦN LOGIC SERVER & BOT BRAIN ---

    [PunRPC]
    void RPC_SubmitAnswer(int answerIndex, PhotonMessageInfo info)
    {
        // Xác định ai gửi: Nếu info != null thì lấy Sender, nếu null (Bot gọi local) thì coi như Bot
        int senderID = (info.Sender.ActorNumber != null) ? info.Sender.ActorNumber : BOT_ACTOR_NUMBER;
        ProcessAnswerLogic(senderID, answerIndex);
    }
    
    // --- CORE LOGIC ---
    void ProcessAnswerLogic(int playerID, int answerIndex)
    {
        // Chỉ Master Client xử lý logic (Khi đấu Bot, mình là Master)
        if (!PhotonNetwork.IsMasterClient) return;
        if (isGameOver) return; 

        isTimerRunning = false; // Stop Timer

        bool isCorrect = false;
        // answerIndex = -1 là hết giờ
        if (answerIndex >= 0 && answerIndex < 4)
        {
            isCorrect = (answerIndex == allQuestions[currentQuestionIndex].correctAnswerIdx);
        }

        if (!isCorrect)
        {
            // Sai -> Trừ máu
            if (playerLives.ContainsKey(playerID))
            {
                playerLives[playerID]--;
            }
            
            // Sync máu
            SyncLivesToClients();

            // Check Thua
            if (CheckGameOverCondition()) return;
        }

        // Next Question
        currentQuestionIndex++;
        if (currentQuestionIndex >= allQuestions.Count) currentQuestionIndex = 0;
        
        // Đổi lượt
        SwitchTurn();

        // Sync State mới xuống Client
        photonView.RPC("RPC_SyncState", RpcTarget.All, currentQuestionIndex, currentTurnActorNumber);

        if (BotMatchHelper.IsBotMatch && currentTurnActorNumber == BOT_ACTOR_NUMBER)
        {
            StartCoroutine(BotTurnRoutine());
        }
    }
    
    // --- BOT AI ---
    IEnumerator BotTurnRoutine()
    {
        // Bot suy nghĩ 2s - 4s
        float thinkTime = UnityEngine.Random.Range(2.0f, 4.0f);
        yield return new WaitForSeconds(thinkTime);

        if (isGameOver) yield break;

        // Quyết định đúng sai dựa trên BotAccuracy
        bool willAnswerCorrect = UnityEngine.Random.Range(0, 100) < BotMatchHelper.BotAccuracy;
        int finalAnswerIdx = -1;
        int correctIdx = allQuestions[currentQuestionIndex].correctAnswerIdx;

        if (willAnswerCorrect)
        {
            finalAnswerIdx = correctIdx;
        }
        else
        {
            // Chọn sai
            List<int> wrongOptions = new List<int>();
            for (int i = 0; i < 4; i++) if (i != correctIdx) wrongOptions.Add(i);
            
            if (wrongOptions.Count > 0)
                finalAnswerIdx = wrongOptions[UnityEngine.Random.Range(0, wrongOptions.Count)];
        }
        
        ProcessAnswerLogic(BOT_ACTOR_NUMBER, finalAnswerIdx);
    }

    void SwitchTurn()
    {
        if (BotMatchHelper.IsBotMatch)
        {
            // Toggle giữa Mình và Bot
            if (currentTurnActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                currentTurnActorNumber = BOT_ACTOR_NUMBER;
            else
                currentTurnActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        }
        else
        {
            // Logic PvP Online
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber != currentTurnActorNumber)
                {
                    currentTurnActorNumber = p.ActorNumber;
                    break;
                }
            }
        }
    }

    void SyncLivesToClients()
    {
        // Chuyển Dictionary thành Array để gửi qua RPC
        int[] actors = new int[playerLives.Count];
        int[] lives = new int[playerLives.Count];
        int i = 0;
        foreach(var kvp in playerLives)
        {
            actors[i] = kvp.Key;
            lives[i] = kvp.Value;
            i++;
        }
        photonView.RPC("RPC_SyncLives", RpcTarget.All, actors, lives);
    }
    
    // --- UPDATE & TIMER ---
    private void Update()
    {
        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime;
            if(timerText != null) timerText.text = Mathf.CeilToInt(currentTimer).ToString();

            if (PhotonNetwork.IsMasterClient && currentTimer <= 0)
            {
                Debug.Log("Hết giờ! Xử thua lượt này.");
                ProcessAnswerLogic(currentTurnActorNumber, -1);
            }
        }
    }
    
    // --- GAME OVER CHECK ---
    bool CheckGameOverCondition()
    {
        int survivorActorNumber = -1; 
        bool isAnyoneDead = false;    

        foreach (var kvp in playerLives)
        {
            if (kvp.Value > 0) survivorActorNumber = kvp.Key;
            else isAnyoneDead = true; 
        }

        if (isAnyoneDead)
        {
            if (survivorActorNumber != -1)
            {
                photonView.RPC("RPC_GameOver", RpcTarget.All, "WINNER", survivorActorNumber);
            }
            else
            {
                photonView.RPC("RPC_GameOver", RpcTarget.All, "DRAW", -1);
            }
            return true; 
        }
        return false;
    }

    // --- RPCs SYNC CLIENT ---

    [PunRPC]
    void RPC_SyncState(int questionIdx, int turnActorID)
    {
        currentQuestionIndex = questionIdx;
        currentTurnActorNumber = turnActorID;

        // UI Câu hỏi
        QuestionData data = allQuestions[currentQuestionIndex];
        questionText.text = data.questionText;
        for (int i = 0; i < 4; i++) answerTexts[i].text = data.answers[i];

        // UI Turn
        bool isMyTurn = (PhotonNetwork.LocalPlayer.ActorNumber == currentTurnActorNumber);
        SetButtonsInteractable(isMyTurn);
        
        statusText.text = isMyTurn ? "Lượt của BẠN" : "Lượt đối thủ...";
        statusText.color = isMyTurn ? Color.green : Color.red;
        
        currentTimer = timeLimit;
        isTimerRunning = true;    
    }

    [PunRPC]
    void RPC_SyncLive(int playerID) // (Có thể bỏ hàm này nếu đã dùng SyncLives full)
    {
        // Hàm này giữ lại để tạo hiệu ứng nổ tim nếu cần
        // Nhưng logic chính cập nhật Dictionary nên dùng RPC_SyncLives
    }

    [PunRPC]
    void RPC_SyncLives(int[] actors, int[] lives)
    {
        playerLives.Clear();
        for(int i=0; i<actors.Length; i++)
        {
            playerLives.Add(actors[i], lives[i]);
        }
        UpdateLivesUI();
    }
    
    void UpdateLivesUI()
    {
        // Reset Visual
        foreach (Image myLife in myLives) myLife.sprite = enableHeart;
        foreach (Image enemy in enemyLives) enemy.sprite = enableHeart;
        
        // Cập nhật theo data
        // Lấy ID của mình
        int myID = PhotonNetwork.LocalPlayer.ActorNumber;
        
        if (playerLives.ContainsKey(myID))
        {
            int currentLives = playerLives[myID];
            // Nếu còn 2 máu -> tim thứ 3 (index 2) bị disable
            // Nếu còn 1 máu -> tim 2,3 disable
            for (int i = 0; i < 3; i++)
            {
                if (i >= currentLives) myLives[i].sprite = disableHeart;
            }
        }
        
        // Lấy ID đối thủ (Khác ID mình)
        int enemyID = -1;
        foreach(var key in playerLives.Keys)
        {
            if (key != myID) enemyID = key;
        }

        if (enemyID != -1 && playerLives.ContainsKey(enemyID))
        {
            int currentLives = playerLives[enemyID];
            for (int i = 0; i < 3; i++)
            {
                if (i >= currentLives) enemyLives[i].sprite = disableHeart;
            }
        }
    }

    [PunRPC]
    void RPC_GameOver(string msg, int survivorActorNumber)
    {
        loadingPanel.SetActive(false);
        isGameOver = true;
        isTimerRunning = false;
        SetButtonsInteractable(false);

        // Check xem mình có thắng không
        bool amIWinner = (PhotonNetwork.LocalPlayer.ActorNumber == survivorActorNumber);
        
        UpdateMissionState(GlobalData.MissionKeys.P2P);

        if (msg == "DRAW")
        {
            saveMatchDatabase("DRAW", EloCalculator.GameResult.Draw, otherPlayer.name);
            // Hiện panel Draw (hoặc Lose tùy logic game)
            if (gameLosePanel)
            {
                gameLosePanel.SetActive(true);
                gameLosePanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
            }
        }
        else if (amIWinner)
        {
            saveMatchDatabase("WIN", EloCalculator.GameResult.Win, otherPlayer.name);
            gameWinPanel.SetActive(true);
            gameWinPanel.GetComponent<GameOverPanelController>().Modegame = 1;
            gameWinPanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
            UpdateMissionState(GlobalData.MissionKeys.WIN_P2P);
        }
        else
        {
            saveMatchDatabase("LOSE", EloCalculator.GameResult.Loss, otherPlayer.name);
            gameLosePanel.SetActive(true);
            gameLosePanel.GetComponent<GameOverPanelController>().Modegame = 1;
            gameLosePanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
        }
        
        isGameStarted = false;
    }
    
    // --- HELPER FUNCTIONS ---
    
    private async void UpdateMissionState(string nameMission)
    {
        await FirebaseDatabaseManager.Instance.CompleteMissionById(nameMission);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Đã thoát phòng, về Home.");
        FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.ONLINE);
        SceneManager.LoadScene("HomeScene");
    }

    void saveMatchDatabase(string resultState, EloCalculator.GameResult result, string otherName)
    {
        rankChange = EloCalculator.CalculateRatingChange(myPlayer.rank, otherPlayer.rank, result);
        // Nếu đấu Bot hoặc đấu Friend thì có thể ko tính rank (tùy logic game bạn)
        if (NetworkGameState.CurrentJoinType == NetworkGameState.JoinType.FriendInvite)
        {
            rankChange = 0;
        }
            
        RankDatabaseManager.Instance.SaveMatchHistory(matchId, resultState, rankChange, otherName, "Đáp Nhanh");
    }

    void SetButtonsInteractable(bool state)
    {
        foreach (Button btn in answerButtons)
        {
            btn.interactable = state;
        }
    }

    void InitUIPlayer()
    {
        loadingPanel.SetActive(true);
        gameLosePanel.SetActive(false);
        gameWinPanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        loadingStatusText.text = "ĐANG TẢI CÂU HỎI.....";

        // Setup Player 1 (Là mình)
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) 
            {
                UpdateSinglePlayerUI(player, player1Container);
            }
            // Nếu là Online Mode -> Setup Player 2 từ Photon PlayerList
            else if (!BotMatchHelper.IsBotMatch) 
            {
                UpdateSinglePlayerUI(player, player2Container);
            }
        }
        
        // Setup Player 2 (Nếu là Bot Mode)
        if (BotMatchHelper.IsBotMatch)
        {
            // Điền data ảo vào biến otherPlayer
            otherPlayer.name = BotMatchHelper.BotName;
            otherPlayer.rank = BotMatchHelper.BotRank;
            
            // Hiển thị UI
            if (player2Container != null)
            {
                player2Container.SetActive(true);
                player2Container.GetComponent<FriendItemUI>().SetupUI(
                    BotMatchHelper.BotName,
                    BotMatchHelper.BotAvatarID,
                    BotMatchHelper.BotBorderID,
                    BotMatchHelper.BotRank.ToString()
                );
            }
        }
    }

    void UpdateSinglePlayerUI(Player player, GameObject playerContainer)
    {
        string nameTxt = player.NickName;
        string avatarId = GetSafeString(player, "AvatarID"); 
        string borderId = GetSafeString(player, "BorderID");
        string rankPoint = GetSafeString(player, "Rank");
        
        if (player.IsLocal)
        {
            myPlayer.name = player.NickName;
            myPlayer.rank = int.Parse(rankPoint);
        }
        else
        {
            otherPlayer.name = player.NickName;
            otherPlayer.rank = int.Parse(rankPoint);
        }
        playerContainer.GetComponent<FriendItemUI>().SetupUI(nameTxt, avatarId, borderId, rankPoint);
    }

    private string GetSafeString(Player player, string key, string defaultValue = "0")
    {
        if (player.CustomProperties.TryGetValue(key, out object val)) return val.ToString(); 
        return defaultValue;
    }

    private void OnDestroy()
    {
        for (var i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].onClick.RemoveAllListeners(); 
        }
    }
    
    // --- XỬ LÝ KHI ĐỐI THỦ THOÁT GAME (Chỉ Online) ---
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(isGameOver) return;
        if(BotMatchHelper.IsBotMatch) return; // Bot không bao giờ thoát :)

        Debug.Log("Người chơi " + otherPlayer.NickName + " đã thoát game.");
        isTimerRunning = false;
        isGameStarted = false;
        SetButtonsInteractable(false);

        statusText.text = "Đối thủ đã thoát! Bạn thắng!";
        
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        HandleOpponentLeftWin(myActorNumber);
    }

    void HandleOpponentLeftWin(int winnerActorNumber)
    {
        saveMatchDatabase("WIN", EloCalculator.GameResult.Win, otherPlayer.name);
        UpdateMissionState(GlobalData.MissionKeys.WIN_P2P);
        UpdateMissionState(GlobalData.MissionKeys.P2P);

        gameWinPanel.SetActive(true);
        if(gameWinPanel.GetComponent<GameOverPanelController>() != null)
        {
            gameWinPanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
        }
    }
}