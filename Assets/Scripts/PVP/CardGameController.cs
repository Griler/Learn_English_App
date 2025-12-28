using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using ExitGames.Client.Photon;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Data Model
[System.Serializable]
public class CardDataModel
{
    public int id;
    public string englishWord;
    public string spriteName;
}

[System.Serializable]
public class UserDataPVP
{
    public string name;
    public int rank;
    public int actorId;
    public UserDataPVP() { }
}

public class CardGameController : MonoBehaviourPunCallbacks
{
    [Header("--- GAME SETTINGS ---")] 
    public int maxPairsInGame = 10;
    public float turnTimeLimit = 15f;
    private const int BOT_ACTOR_NUMBER = 9999; // ID Định danh cho Bot

    [Header("--- UI REFERENCES ---")] 
    public Transform gridContainer;
    public GameObject cardPrefab;

    public TextMeshProUGUI statusText;
    public TextMeshProUGUI p1ScoreText; // Điểm Player 1 (Local)
    public TextMeshProUGUI p2ScoreText; // Điểm Player 2 (Remote/Bot)
    public TextMeshProUGUI timerText;

    public GameObject loadingPanel;
    public TextMeshProUGUI loadingStatusText;
    public TextMeshProUGUI countdownText;

    public GameObject gameWinPanel;
    public GameObject gameLosePanel;
    private int rankChange = 0;

    public GameObject player1Container; // Avatar Left
    public GameObject player2Container; // Avatar Right

    [Header("--- DATA ---")]
    [SerializeField]
    private List<CardDataModel> rawCardData = new List<CardDataModel>();
    private List<CardController> activeCards = new List<CardController>();

    private string matchId = "";
    private int currentTurnActorNumber;
    private bool isProcessingMatch = false; 
    private int firstCardIndex = -1;
    private int secondCardIndex = -1;

    // Timer
    private float currentTimer;
    private bool isTimerRunning = false;

    // Score & Win Condition
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private int totalPairsFound = 0;
    private int targetPairsToWin = 0; 
    
    // User Info
    [NotNull] private UserDataPVP myPlayer = new UserDataPVP();
    [NotNull] private UserDataPVP otherPlayer = new UserDataPVP();

    DatabaseReference reference;
    private bool isGameStarted = false; 
    private bool canClick = true; 
    private bool isGameOver = false; 

    private void Start()
    {
        Hashtable resetProps = new Hashtable();
        resetProps.Add("IsLoaded", false); 
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
        rankChange = 0;
        isGameOver = false; 

        // Setup UI ban đầu (Load avatar Bot nếu cần)
        InitUIPlayer();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.INMATCH);
                LoadCardsFromFirebase();
            }
            else
            {
                Debug.LogError("Lỗi Firebase Dependencies: " + dependencyStatus);
            }
        });
    }

    // 1. LOAD DATA
    void LoadCardsFromFirebase()
    {
        reference.Child("card_data").Child("list").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.IsCompleted || task.Result.Value == null) return;

            DataSnapshot snapshot = task.Result;
            rawCardData = new List<CardDataModel>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                try
                {
                    CardDataModel card = new CardDataModel();
                    if (child.Child("id").Value != null)
                        card.id = int.Parse(child.Child("id").Value.ToString());
                    if (child.Child("englishWord").Value != null)
                        card.englishWord = child.Child("englishWord").Value.ToString();
                    if (child.Child("spriteName").Value != null)
                        card.spriteName = child.Child("spriteName").Value.ToString();

                    rawCardData.Add(card);
                }
                catch (System.Exception ex) { }
            }

            Debug.Log($"Đã tải kho: {rawCardData.Count} từ vựng.");
            isGameStarted = false; // Reset cờ start

            // --- [LOGIC PHÂN NHÁNH BOT vs PVP] ---
            if (BotMatchHelper.IsBotMatch)
            {
                loadingStatusText.text = "Đang vào trận đấu tập...";
                int seed = UnityEngine.Random.Range(1, 999999);
                
                // Start countdown giả
                RPC_StartCountdown();
            }
            else 
            {
                loadingStatusText.text = "Đang đợi người chơi khác...";
                Hashtable props = new Hashtable();
                props.Add("IsLoaded", true);
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }
        });
    }

    // --- 2. CHECK READY (Chỉ dùng cho PvP) ---
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (BotMatchHelper.IsBotMatch) return; // Bỏ qua nếu là Bot

        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey("IsLoaded"))
        {
            CheckAllPlayersReady();
        }
    }

    private void CheckAllPlayersReady()
    {
        if (PhotonNetwork.PlayerList.Length < 2) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("IsLoaded", out object isLoaded))
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

    IEnumerator<WaitForSeconds> Co_RunCountdownAndStart()
    {
        loadingStatusText.text = "BẮT ĐẦU TRONG:";
        countdownText.gameObject.SetActive(true);
        countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.gameObject.SetActive(false);
        loadingPanel.SetActive(false);


        int gameSeed = UnityEngine.Random.Range(1, 999999);
        int startActor = PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)].ActorNumber;    
        if (!BotMatchHelper.IsBotMatch && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SetupBoard", RpcTarget.AllBuffered, gameSeed, startActor);
        }
        else
        {
            bool playerGoFirst = (UnityEngine.Random.Range(0, 2) == 0);
            startActor  = playerGoFirst ? PhotonNetwork.LocalPlayer.ActorNumber : BOT_ACTOR_NUMBER;
            RPC_SetupBoard(gameSeed, startActor);
        }
    }

    // --- 3. SETUP GAME ---
    [PunRPC]
    void RPC_SetupBoard(int seed, int startTurnActor)
    {
        if (isGameStarted) return; 
        isGameStarted = true;
        
        UnityEngine.Random.InitState(seed);
        rawCardData = rawCardData.OrderBy(item => item.id).ToList();
        
        // Trộn và lấy thẻ
        List<CardDataModel> poolData = new List<CardDataModel>(rawCardData);
        poolData = poolData.OrderBy(x => UnityEngine.Random.Range(1, seed)).ToList();
        int countToTake = Mathf.Min(poolData.Count, maxPairsInGame);
        List<CardDataModel> selectedWords = poolData.Take(countToTake).ToList();

        targetPairsToWin = selectedWords.Count;
        totalPairsFound = 0;

        // Nhân đôi thẻ
        List<DeckItem> deck = new List<DeckItem>();
        foreach (var item in selectedWords)
        {
            deck.Add(new DeckItem(item, true));
            deck.Add(new DeckItem(item, false));
        }
        deck = deck.OrderBy(x => UnityEngine.Random.value).ToList();

        // Spawn thẻ
        foreach (Transform child in gridContainer) Destroy(child.gameObject);
        activeCards.Clear();
        playerScores.Clear();

        for (int i = 0; i < deck.Count; i++)
        {
            CardDataModel data = deck[i].data;
            GameObject cardObj = Instantiate(cardPrefab, gridContainer);
            CardController controller = cardObj.GetComponent<CardController>();
            cardObj.SetActive(true);
            controller.Init(data.id, i, data.englishWord, data.spriteName, this, deck[i].isTypeWorld);
            activeCards.Add(controller);
        }

        // Init điểm
        if (BotMatchHelper.IsBotMatch)
        {
            playerScores.Add(PhotonNetwork.LocalPlayer.ActorNumber, 0);
            playerScores.Add(BOT_ACTOR_NUMBER, 0);
        }
        else
        {
            foreach (Player p in PhotonNetwork.PlayerList) playerScores.Add(p.ActorNumber, 0);
        }

        currentTurnActorNumber = startTurnActor;
        UpdateTurnUI();
        UpdateScoreUI();

        currentTimer = turnTimeLimit;
        isTimerRunning = true;
        
        // --- [BOT TRIGGER] ---
        // Nếu Bot đi trước, gọi Bot chạy
        if (BotMatchHelper.IsBotMatch && currentTurnActorNumber == BOT_ACTOR_NUMBER)
        {
            StartCoroutine(BotTurnRoutine());
        }
    }

    // --- 4. GAMEPLAY LOGIC ---

    // Sự kiện CLICK của người chơi thật
    public void OnCardClicked(int cardIndex)
    {
        if(!canClick) return;
        // Kiểm tra đúng lượt mình không
        if (PhotonNetwork.LocalPlayer.ActorNumber != currentTurnActorNumber) return;
        if (isProcessingMatch) return; 

        // Gửi RPC lên Master (Nếu đấu Bot thì mình là Master)
        photonView.RPC("RPC_RequestFlip", RpcTarget.MasterClient, cardIndex, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_RequestFlip(int cardIndex, int senderActorNumber)
    {
        // Chỉ Master xử lý logic
        if (!PhotonNetwork.IsMasterClient) return;

        // Validate
        if (activeCards[cardIndex].isFaceUp || activeCards[cardIndex].isLocked) return;

        // 1. Lật visual lên
        photonView.RPC("RPC_FlipCardVisual", RpcTarget.All, cardIndex);

        // 2. Xử lý Logic tìm cặp
        if (firstCardIndex == -1)
        {
            firstCardIndex = cardIndex;
        }
        else
        {
            secondCardIndex = cardIndex;
            isProcessingMatch = true;
            photonView.RPC("RPC_SetBusyState", RpcTarget.All, true);

            StartCoroutine(CheckMatchLogic(senderActorNumber));
        }
    }
    
    // --- [BOT LOGIC] ---
    IEnumerator BotTurnRoutine()
    {
        Debug.Log("Bot đang suy nghĩ...");
        // 1. Nghĩ 1-2s
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2f));
        if (isGameOver) yield break;

        // 2. Chọn thẻ thứ nhất (Chưa mở, chưa lock)
        var availableCards = activeCards.Where(c => !c.isFaceUp && !c.isLocked).ToList();
        if(availableCards.Count == 0) yield break;
        
        // Random thẻ đầu
        CardController card1 = availableCards[UnityEngine.Random.Range(0, availableCards.Count)];
        
        // Gọi hàm RequestFlip (Giả lập click) - Gọi trực tiếp vì Bot là local
        RPC_RequestFlip(card1.indexInGrid, BOT_ACTOR_NUMBER);
        
        // 3. Nghĩ tiếp 1s để chọn thẻ 2
        yield return new WaitForSeconds(1f);
        
        // Refresh danh sách thẻ còn lại
        availableCards = activeCards.Where(c => !c.isFaceUp && !c.isLocked).ToList(); // Card 1 đã faceUp rồi
        if(availableCards.Count == 0) yield break;

        CardController card2 = null;

        // --- Logic thông minh của Bot ---
        // Nếu may mắn (< Accuracy), Bot sẽ cố tìm thẻ trùng với Card 1
        BotMatchHelper.BotAccuracy = 30;
        bool isSmartMove = UnityEngine.Random.Range(0, 100) <= BotMatchHelper.BotAccuracy;
        
        if (isSmartMove)
        {
            // Tìm trong đống bài úp xem có con nào trùng ID với card1 không
            // (Thực tế: Bot "nhìn xuyên bài" để fake trí nhớ)
            card2 = availableCards.FirstOrDefault(c => c.cardId == card1.cardId);
        }

        // Nếu không thông minh hoặc không tìm thấy, chọn bừa
        if(card2 == null)
        {
             card2 = availableCards[UnityEngine.Random.Range(0, availableCards.Count)];
        }
        
        // Lật thẻ 2
        RPC_RequestFlip(card2.indexInGrid, BOT_ACTOR_NUMBER);
    }

    IEnumerator CheckMatchLogic(int playerID)
    {
        yield return new WaitForSeconds(1.0f); // Delay để người chơi xem thẻ

        bool isMatch = (activeCards[firstCardIndex].cardId == activeCards[secondCardIndex].cardId);

        if (isMatch)
        {
            totalPairsFound++;
            // Cộng điểm
            if (playerScores.ContainsKey(playerID)) playerScores[playerID]++;
            // Không đổi lượt nếu đoán đúng (Tùy luật game, ở đây giữ nguyên luật cũ là đoán đúng đi tiếp?)
            // À code cũ của bạn: Đoán đúng -> RPC_MatchResult -> UpdateScore.
            // Nhưng code cũ: isMatch -> SyncScore. Else -> SwitchTurn. -> Luật là ĐÚNG ĐƯỢC ĐI TIẾP.
        }
        else
        {
            SwitchTurn();
        }
        
        // Sync kết quả xuống Client
        photonView.RPC("RPC_MatchResult", RpcTarget.All, isMatch, currentTurnActorNumber, firstCardIndex, secondCardIndex);
        
        // Sync điểm số (Để đảm bảo UI chuẩn)
        int[] actors = playerScores.Keys.ToArray();
        int[] scores = playerScores.Values.ToArray();
        photonView.RPC("RPC_SyncAllScores", RpcTarget.All, actors, scores);

        // Reset biến
        firstCardIndex = -1;
        secondCardIndex = -1;
        isProcessingMatch = false;
        photonView.RPC("RPC_SetBusyState", RpcTarget.All, false);

        if (totalPairsFound >= targetPairsToWin)
        {
            CheckGameOver();
        }
        else
        {
            photonView.RPC("RPC_ResetTimer", RpcTarget.All);
            
            // --- [BOT TRIGGER] ---
            // Nếu vẫn là lượt Bot (do đoán đúng) hoặc vừa chuyển sang lượt Bot
            if (BotMatchHelper.IsBotMatch && currentTurnActorNumber == BOT_ACTOR_NUMBER)
            {
                StartCoroutine(BotTurnRoutine());
            }
        }
    }
    
    void SwitchTurn()
    {
        if (BotMatchHelper.IsBotMatch)
        {
             if (currentTurnActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                currentTurnActorNumber = BOT_ACTOR_NUMBER;
            else
                currentTurnActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        }
        else
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber != currentTurnActorNumber)
                {
                    currentTurnActorNumber = p.ActorNumber;
                    break;
                }
            }
        }
        canClick = true;
    }

    [PunRPC]
    void RPC_SyncAllScores(int[] actors, int[] scores)
    {
        playerScores.Clear();
        for(int i=0; i<actors.Length; i++) playerScores.Add(actors[i], scores[i]);
        UpdateScoreUI();
    }

    [PunRPC]
    void RPC_ResetTimer()
    {
        currentTimer = turnTimeLimit;
        isTimerRunning = true;
        if (timerText != null) timerText.color = Color.white;
    }

    [PunRPC]
    void RPC_FlipCardVisual(int index)
    {
        if (index >= 0 && index < activeCards.Count) activeCards[index].FlipOpen();
    }

    [PunRPC]
    void RPC_SetBusyState(bool state)
    {
        isProcessingMatch = state;
    }

    [PunRPC]
    void RPC_MatchResult(bool isMatch, int nextTurnActor, int idx1, int idx2)
    {
        currentTurnActorNumber = nextTurnActor;
        UpdateTurnUI();

        if (isMatch)
        {
            if (idx1 >= 0) activeCards[idx1].LockCard();
            if (idx2 >= 0) activeCards[idx2].LockCard();
        }
        else
        {
            if (idx1 >= 0) activeCards[idx1].FlipClose();
            if (idx2 >= 0) activeCards[idx2].FlipClose();
        }
    }

    // --- 5. TIMER & UPDATE ---
    void Update()
    {
        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime;
            if (timerText != null)
            {
                timerText.color = (currentTimer <= 5) ? Color.red : Color.saddleBrown;
                timerText.text = Mathf.CeilToInt(currentTimer).ToString();
            }

            if (currentTimer < 1) canClick = false;

            if (PhotonNetwork.IsMasterClient && currentTimer <= 0)
            {
                HandleTurnTimeout();
            }
        }
    }

    void HandleTurnTimeout()
    {
        isTimerRunning = false;
        SwitchTurn();
        // Timeout: Úp thẻ đang mở dở lại
        photonView.RPC("RPC_TimeoutTurnSwitch", RpcTarget.All, currentTurnActorNumber, firstCardIndex);
        
        // Nếu timeout sang lượt Bot -> Gọi Bot
        if (BotMatchHelper.IsBotMatch && currentTurnActorNumber == BOT_ACTOR_NUMBER)
        {
            StartCoroutine(BotTurnRoutine());
        }
    }

    [PunRPC]
    void RPC_TimeoutTurnSwitch(int nextTurnActor, int cardToCloseIndex)
    {
        if (cardToCloseIndex != -1 && cardToCloseIndex < activeCards.Count)
        {
            activeCards[cardToCloseIndex].FlipClose();
        }

        firstCardIndex = -1;
        secondCardIndex = -1;
        isProcessingMatch = false;
        canClick = true;

        currentTurnActorNumber = nextTurnActor;
        UpdateTurnUI();
        RPC_ResetTimer();
    }

    // --- 6. END GAME ---
    void CheckGameOver()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("RPC_EndGameProcess", RpcTarget.All);
    }

    [PunRPC]
    void RPC_EndGameProcess()
    {
        loadingPanel.SetActive(false);
        isTimerRunning = false;
        isGameOver = true;

        int myScore = 0;
        int enemyScore = 0;

        if (playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
            myScore = playerScores[PhotonNetwork.LocalPlayer.ActorNumber];
            
        // Tìm điểm đối thủ
        foreach (var kvp in playerScores)
        {
            if (kvp.Key != PhotonNetwork.LocalPlayer.ActorNumber) enemyScore = kvp.Value;
        }

        UpdateMissionState(GlobalData.MissionKeys.P2P);
        
        if (myScore > enemyScore)
        {
            saveMatchDatabase("WIN", EloCalculator.GameResult.Win, otherPlayer.name);
            gameWinPanel.SetActive(true);
            gameWinPanel.GetComponent<GameOverPanelController>().Modegame = 2;
            gameWinPanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
            UpdateMissionState(GlobalData.MissionKeys.WIN_P2P);
        }
        else if (myScore < enemyScore)
        {
            saveMatchDatabase("LOSE", EloCalculator.GameResult.Loss, otherPlayer.name);
            gameLosePanel.SetActive(true);
            gameLosePanel.GetComponent<GameOverPanelController>().Modegame = 2;
            gameLosePanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
        }
        else
        {
            saveMatchDatabase("DRAW", EloCalculator.GameResult.Draw, otherPlayer.name);
            gameWinPanel.SetActive(true); // Hoặc Draw Panel
            gameWinPanel.GetComponent<GameOverPanelController>().Modegame = 2;
            gameWinPanel.GetComponent<GameOverPanelController>().ShowGameOver((int)(rankChange/2));
            UpdateMissionState(GlobalData.MissionKeys.WIN_P2P);
        }

        isGameStarted = false;
    }

    void saveMatchDatabase(string resultState, EloCalculator.GameResult result, string otherName)
    {
        rankChange = EloCalculator.CalculateRatingChange(myPlayer.rank, otherPlayer.rank, result);
        if (NetworkGameState.CurrentJoinType == NetworkGameState.JoinType.FriendInvite)
            rankChange = 0;
            
        RankDatabaseManager.Instance.SaveMatchHistory(matchId, resultState, rankChange, otherName, "Lật thẻ");
    }

    // --- UTILS ---
    void UpdateTurnUI()
    {
        bool isMyTurn = (PhotonNetwork.LocalPlayer.ActorNumber == currentTurnActorNumber);
        statusText.text = isMyTurn ? "Lượt của BẠN" : "Đối thủ đang chọn...";
        statusText.color = isMyTurn ? Color.green : Color.red;
    }

    void UpdateScoreUI()
    {
        int myScore = 0;
        int enemyScore = 0;

        if (playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
            myScore = playerScores[PhotonNetwork.LocalPlayer.ActorNumber];

        foreach(var kvp in playerScores) {
            if(kvp.Key != PhotonNetwork.LocalPlayer.ActorNumber) enemyScore = kvp.Value;
        }

        p1ScoreText.text = "Tôi: " + myScore;
        p2ScoreText.text = "Đối thủ: " + enemyScore;
    }

    void InitUIPlayer()
    {
        loadingPanel.SetActive(true);
        gameLosePanel.SetActive(false);
        gameWinPanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        loadingStatusText.text = "ĐANG TẢI DỮ LIỆU.....";

        // Setup Player 1 (Mình)
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) UpdateSinglePlayerUI(player, player1Container);
            else if (!BotMatchHelper.IsBotMatch) UpdateSinglePlayerUI(player, player2Container);
        }
        
        // Setup Player 2 (Bot)
        if (BotMatchHelper.IsBotMatch)
        {
            otherPlayer.name = BotMatchHelper.BotName;
            otherPlayer.rank = BotMatchHelper.BotRank;
            
            if(player2Container != null)
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

        if (player.IsLocal) {
            myPlayer.name = player.NickName;
            myPlayer.rank = int.Parse(rankPoint);
        } else {
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
    
    public override void OnLeftRoom()
    {
        Debug.Log("Đã thoát phòng Card Game, về Home.");
        FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.ONLINE);
        SceneManager.LoadScene("HomeScene"); 
    }
    
    private async void UpdateMissionState(string nameMission)
    {
        await FirebaseDatabaseManager.Instance.CompleteMissionById(nameMission);
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(isGameOver) return;
        if(BotMatchHelper.IsBotMatch) return;

        Debug.Log("Người chơi " + otherPlayer.NickName + " đã thoát game.");
        isGameStarted = false;
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