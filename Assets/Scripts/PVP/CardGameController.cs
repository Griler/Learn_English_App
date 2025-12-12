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

// Data Model để map với JSON Firebase
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
    public UserDataPVP() { }
}

public class CardGameController : MonoBehaviourPunCallbacks
{
    [Header("--- GAME SETTINGS ---")] [Tooltip("Số cặp thẻ muốn chơi (VD: 10 cặp = 20 thẻ)")]
    public int maxPairsInGame = 10;

    public float turnTimeLimit = 15f;

    [Header("--- UI REFERENCES ---")] public Transform gridContainer; // Grid Layout Group
    public GameObject cardPrefab;

    public TextMeshProUGUI statusText;
    public TextMeshProUGUI p1ScoreText; // Điểm Player 1 (Local)
    public TextMeshProUGUI p2ScoreText; // Điểm Player 2 (Remote)
    public TextMeshProUGUI timerText;

    public GameObject loadingPanel;
    public TextMeshProUGUI loadingStatusText;
    public TextMeshProUGUI countdownText;

    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI gameOverTimerPanel;

    public GameObject player1Container; // Avatar Left
    public GameObject player2Container; // Avatar Right

    [Header("--- DATA ---")]
    // List chứa dữ liệu thô từ Firebase (Kho 50-60 từ)
    [SerializeField]
    private List<CardDataModel> rawCardData = new List<CardDataModel>();

    // List quản lý các thẻ đang hiện trên bàn
    private List<CardController> activeCards = new List<CardController>();

    private string matchId = "";
    // State Game
    private int currentTurnActorNumber;
    private bool isProcessingMatch = false; // Cờ chặn click khi đang so sánh
    private int firstCardIndex = -1;
    private int secondCardIndex = -1;

    // Timer
    private float currentTimer;
    private bool isTimerRunning = false;

    // Score & Win Condition
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private int totalPairsFound = 0;
    private int targetPairsToWin = 0; // Sẽ được set khi Start Game
    
    // User Info
    [NotNull] private UserDataPVP myPlayer = new UserDataPVP();
    [NotNull] private UserDataPVP otherPlayer = new UserDataPVP();

    DatabaseReference reference;
    private bool isGameStarted = false; // Cờ chặn gọi setup 2 lần
    private bool canClick = true; // Cờ chặn gọi setup 2 lần

    private void Start()
    {
        Hashtable resetProps = new Hashtable();
        resetProps.Add("IsLoaded", false); 
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
        
        InitUIPlayer();
        matchId = PhotonNetwork.CurrentRoom.Name;
        // Fix lỗi dependencies Firebase trước khi chạy
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadCardsFromFirebase();
            }
            else
            {
                Debug.LogError("Lỗi Firebase Dependencies: " + dependencyStatus);
            }
        });
    }

    // 1. LOAD DATA TỪ FIREBASE (Giữ nguyên format logic)
    void LoadCardsFromFirebase()
    {
        reference.Child("card_data").Child("list").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Lỗi kết nối Firebase");
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Value == null) return;

                rawCardData = new List<CardDataModel>();

                foreach (DataSnapshot child in snapshot.Children)
                {
                    if (rawCardData.Count < 11)
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
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning("Parse lỗi: " + ex.Message);
                        }

                    }
                }

                Debug.Log($"Đã tải kho: {rawCardData.Count} từ vựng.");
                loadingStatusText.text = "Đang đợi người chơi khác...";

                // Báo hiệu tôi đã tải xong
                Hashtable props = new Hashtable();
                props.Add("IsLoaded", true);
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                RPC_SetupBoard(1, 1);
            }
        });
    }

    // --- 2. CHECK READY & START COUNTDOWN ---
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey("IsLoaded"))
        {
            CheckAllPlayersReady();
        }
    }

    private void CheckAllPlayersReady()
    {
        // Phải đảm bảo phòng đủ 2 người và cả 2 đều Loaded
        if (PhotonNetwork.PlayerList.Length < 2) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("IsLoaded", out object isLoaded))
            {
                if (!(bool)isLoaded) return;
            }
            else return;
        }

        // Tất cả sẵn sàng -> Master ra lệnh đếm ngược
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

        if (PhotonNetwork.IsMasterClient)
        {
            int gameSeed = UnityEngine.Random.Range(0, 999999);
            int startActor = PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)]
                .ActorNumber;

            // Gửi Seed và người đi trước cho cả 2 máy
            photonView.RPC("RPC_SetupBoard", RpcTarget.AllBuffered, gameSeed, startActor);
        }
    }

    // --- 3. SETUP GAME (LOGIC RANDOM CARD) ---
    [PunRPC]
    void RPC_SetupBoard(int seed, int startTurnActor)
    {
        if (isGameStarted) return; 
        isGameStarted = true;
        Debug.Log($"Setup Board với Seed: {seed}");
        System.Random rnd = new System.Random(seed);

        // A. Trộn kho từ vựng và lấy giới hạn (Limit)
        List<CardDataModel> poolData = new List<CardDataModel>(rawCardData);
        poolData = poolData.OrderBy(x => rnd.Next()).ToList();

        int countToTake = Mathf.Min(poolData.Count, maxPairsInGame);
        List<CardDataModel> selectedWords = poolData.Take(countToTake).ToList();

        // Cập nhật điều kiện thắng
        targetPairsToWin = selectedWords.Count;
        totalPairsFound = 0;

        // B. Nhân đôi thẻ (Tạo cặp)
        List<DeckItem> deck = new List<DeckItem>();
        foreach (var item in selectedWords)
        {
            deck.Add(new DeckItem(item, true));
            deck.Add(new DeckItem(item, false));
        }

        // C. Trộn lại lần nữa để rải ra bàn
        deck = deck.OrderBy(x => rnd.Next()).ToList();

        // D. Spawn thẻ lên bàn
        foreach (Transform child in gridContainer) Destroy(child.gameObject);
        activeCards.Clear();
        playerScores.Clear();

        for (int i = 0; i < deck.Count; i++)
        {
            CardDataModel data = deck[i].data;
            GameObject cardObj = Instantiate(cardPrefab, gridContainer);
            CardController controller = cardObj.GetComponent<CardController>();
            cardObj.SetActive(true);
            // Init thẻ
            controller.Init(data.id, i, data.englishWord, data.spriteName, this, deck[i].isTypeWorld);
            activeCards.Add(controller);
        }  
        // Init điểm
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            playerScores.Add(p.ActorNumber, 0);
        }

        currentTurnActorNumber = startTurnActor;
        UpdateTurnUI();
        UpdateScoreUI();

        // Start Timer
        currentTimer = turnTimeLimit;
        isTimerRunning = true;
    }

    // --- 4. GAMEPLAY LOGIC ---

    // Được gọi từ CardController khi người chơi click
    public void OnCardClicked(int cardIndex)
    {
        if(!canClick) return;
        if (PhotonNetwork.LocalPlayer.ActorNumber != currentTurnActorNumber) return;
        if (isProcessingMatch) return; // Đang bận so sánh thẻ

        photonView.RPC("RPC_RequestFlip", RpcTarget.MasterClient, cardIndex, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_RequestFlip(int cardIndex, int senderActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Validate
        if (activeCards[cardIndex].isFaceUp || activeCards[cardIndex].isLocked) return;

        // Lật visual ngay lập tức
        photonView.RPC("RPC_FlipCardVisual", RpcTarget.All, cardIndex);

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

    IEnumerator CheckMatchLogic(int playerID)
    {
        yield return new WaitForSeconds(1.0f); // Đợi 1s

        bool isMatch = (activeCards[firstCardIndex].cardId == activeCards[secondCardIndex].cardId);

        if (isMatch)
        {
            totalPairsFound++;
            photonView.RPC("RPC_SyncScore", RpcTarget.All, playerID);
        }
        else
        {
            SwitchTurn();
        }

        photonView.RPC("RPC_MatchResult", RpcTarget.All, isMatch, currentTurnActorNumber, firstCardIndex,
            secondCardIndex);

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
            // Reset timer cho lượt tiếp theo
            photonView.RPC("RPC_ResetTimer", RpcTarget.All);
        }
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
            UpdateScoreUI();
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

            if (currentTimer < 1)
            {
                canClick = false;
            }

        // Logic Timeout (Chỉ Master check)
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
        // Gửi lệnh timeout, kèm theo thẻ đang mở (nếu có) để úp lại
        photonView.RPC("RPC_TimeoutTurnSwitch", RpcTarget.All, currentTurnActorNumber, firstCardIndex);
    }

    [PunRPC]
    void RPC_TimeoutTurnSwitch(int nextTurnActor, int cardToCloseIndex)
    {
        // Úp thẻ đang mở dở
        if (cardToCloseIndex != -1 && cardToCloseIndex < activeCards.Count)
        {
            activeCards[cardToCloseIndex].FlipClose();
        }

        // Reset biến tạm
        firstCardIndex = -1;
        secondCardIndex = -1;
        isProcessingMatch = false;
        canClick = true;

        currentTurnActorNumber = nextTurnActor;
        UpdateTurnUI();
        RPC_ResetTimer();
    }

    // --- 6. END GAME & UTILS ---
    void SwitchTurn()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber != currentTurnActorNumber)
            {
                currentTurnActorNumber = p.ActorNumber;
                break;
            }
        }
        canClick = true;
    }

    void CheckGameOver()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("RPC_EndGameProcess", RpcTarget.All);
    }

    [PunRPC]
    void RPC_EndGameProcess()
    {
        loadingPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        isTimerRunning = false;

        int myScore = playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber)
            ? playerScores[PhotonNetwork.LocalPlayer.ActorNumber]
            : 0;
        int enemyScore = 0;
        foreach (var kvp in playerScores)
        {
            if (kvp.Key != PhotonNetwork.LocalPlayer.ActorNumber) enemyScore = kvp.Value;
        }

        if (myScore > enemyScore)
        {
            gameOverText.text = "CHIẾN THẮNG!\n" + myPlayer.name;
            saveMatchDatabase("WIN", EloCalculator.GameResult.Win, otherPlayer.name);
        }
        else if (myScore < enemyScore)
        {
            gameOverText.text = "THẤT BẠI...\n" + otherPlayer.name;
            saveMatchDatabase("LOSE", EloCalculator.GameResult.Loss, otherPlayer.name);
        }
        else
        {
            gameOverText.text = "HÒA NHAU!";
            saveMatchDatabase("DRAW", EloCalculator.GameResult.Draw, otherPlayer.name);
        }

        isGameStarted = false;
        StartCoroutine(RunCountdownLoadScene());
    }

    void saveMatchDatabase(string resultState, EloCalculator.GameResult result, string otherName)
    {
        // Giả định bạn có sẵn script RankDatabaseManager và EloCalculator
        int randomRankPoint = EloCalculator.CalculateRatingChange(myPlayer.rank, otherPlayer.rank, result);
        RankDatabaseManager.Instance.SaveMatchHistory(matchId, resultState, randomRankPoint, otherName, "Lật thẻ");
    }

    IEnumerator<WaitForSeconds> RunCountdownLoadScene()
    {
        for (int i = 3; i >= 0; i--)
        {
            gameOverTimerPanel.text = "Trở về trang chủ sau: " + i;
            yield return new WaitForSeconds(1f);
        }
        PhotonNetwork.LeaveRoom();
    }

// Thêm hàm Callback
    public override void OnLeftRoom()
    {
        Debug.Log("Đã thoát phòng Card Game, về Home.");
        SceneManager.LoadScene("HomeScene"); 
    }
    
    

    void UpdateTurnUI()
    {
        bool isMyTurn = (PhotonNetwork.LocalPlayer.ActorNumber == currentTurnActorNumber);
        statusText.text = isMyTurn ? "Lượt của BẠN" : "Đối thủ đang chọn...";
        statusText.color = isMyTurn ? Color.green : Color.red;
    }

    [PunRPC]
    void RPC_SyncScore(int playerID)
    {
        if (playerScores.ContainsKey(playerID)) playerScores[playerID]++;
    }

void UpdateScoreUI()
    {
        // Logic hiển thị điểm
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
        gameOverPanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        loadingStatusText.text = "ĐANG TẢI DỮ LIỆU.....";

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) UpdateSinglePlayerUI(player, player1Container);
            else UpdateSinglePlayerUI(player, player2Container);
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
}