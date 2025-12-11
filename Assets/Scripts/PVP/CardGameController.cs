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

// Class data map với Firebase
[System.Serializable]
public class CardDataModel
{
    public int id; // ID định danh cặp (0-10)
    public string englishWord;
    public string spriteName; // Tên ảnh trong Resources
}

[System.Serializable]
public class 
    
    
    UserDataPVP
{
    public string name;
    public int rank;
    public UserDataPVP() { }
}

public class CardGameController : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public Transform gridContainer; // Nơi chứa các thẻ
    public GameObject cardPrefab;   // Prefab thẻ bài
    
    [Header("UI Info")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI p1ScoreText; // Điểm mình
    public TextMeshProUGUI p2ScoreText; // Điểm đối thủ
    
    public GameObject player1Container; // UI Avatar bên trái
    public GameObject player2Container; // UI Avatar bên phải
    
    public GameObject loadingPanel; 
    public GameObject gameOverPanel; 
    public TextMeshProUGUI loadingStatusText; 
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI gameOverTimerPanel;
    
    [Header("Game Settings")]
    public float turnTimeLimit = 15f; 
    public TextMeshProUGUI timerText;

    private float currentTimer;
    private bool isTimerRunning = false;
    
    [Header("Game Data")]
    // List chứa dữ liệu gốc từ Firebase (11 cặp)
    [SerializeField] private List<CardDataModel> rawCardData = new List<CardDataModel>();
    
    // List quản lý các thẻ đang hiển thị trên bàn (22 thẻ)
    private List<CardController> activeCards = new List<CardController>();

    private int currentTurnActorNumber;
    private bool isProcessingMatch = false; // Chặn click khi đang check đúng sai

    // Logic so sánh thẻ
    private int firstCardIndex = -1;
    private int secondCardIndex = -1;

    [NotNull] private UserDataPVP myPlayer = new UserDataPVP();
    [NotNull] private UserDataPVP otherPlayer = new UserDataPVP();
    
    // Điểm số (Key: ActorNumber, Value: Score)
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private int totalPairsFound = 0;
    private const int MAX_PAIRS = 11;

    DatabaseReference reference;

    private void Start()
    {
        InitUIPlayer();
        
        // Khởi tạo Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadCardsFromFirebase();
            }
            else
            {
                Debug.LogError("Lỗi Firebase: " + dependencyStatus);
            }
        });
    }
    
    // 1. LOAD DATA TỪ FIREBASE (Giữ nguyên format logic)
    void LoadCardsFromFirebase() {
        reference.Child("card_data").Child("list").GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted) { Debug.LogError("Lỗi kết nối Firebase"); return; }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Value == null) return;

                Debug.Log("Dữ liệu thô: " + snapshot.GetRawJsonValue());
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
                        catch (System.Exception ex) { Debug.LogWarning("Parse lỗi: " + ex.Message); }

                    }
                }
                
                Debug.Log($"Đã tải {rawCardData.Count} loại thẻ.");
                
                loadingStatusText.text = "Đang đợi người chơi khác...";

                // Báo đã sẵn sàng
                Hashtable props = new Hashtable();
                props.Add("IsLoaded", true);
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }
        });
    }

    // 2. CHECK READY VÀ ĐẾM NGƯỢC (Giống file mẫu)
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey("IsLoaded"))
        {
            CheckAllPlayersReady();
        }
    }
    
    private void CheckAllPlayersReady()
    {
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
        loadingStatusText.text = "GAME START IN:";
        countdownText.gameObject.SetActive(true);
        countdownText.text = "3"; yield return new WaitForSeconds(1f);
        countdownText.text = "2"; yield return new WaitForSeconds(1f);
        countdownText.text = "1"; yield return new WaitForSeconds(1f);
        
        countdownText.gameObject.SetActive(false);
        loadingPanel.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            // Tạo Seed để random giống nhau ở cả 2 máy
            int gameSeed = UnityEngine.Random.Range(0, 999999);
            // Random người đi trước
            int startActor = PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)].ActorNumber;
            
            photonView.RPC("RPC_SetupBoard", RpcTarget.AllBuffered, gameSeed, startActor);
        }
    }

    // 3. SETUP BÀN CỜ (Thay đổi logic cho phù hợp Game Thẻ Bài)
    [PunRPC]
    void RPC_SetupBoard(int seed, int startTurnActor)
    {
        // Setup Random
        System.Random rnd = new System.Random(seed);

        // Tạo danh sách ID thẻ (11 cặp -> 22 thẻ)
        // Ví dụ: [0,0, 1,1, 2,2 ...]
        List<int> cardIndices = new List<int>();
        for (int i = 0; i < rawCardData.Count; i++)
        {
            cardIndices.Add(i);
            cardIndices.Add(i);
        }

        // Shuffle danh sách ID bằng seed đồng bộ
        cardIndices = cardIndices.OrderBy(x => rnd.Next()).ToList();

        // Xóa bàn cũ
        foreach (Transform child in gridContainer) Destroy(child.gameObject);
        activeCards.Clear();
        playerScores.Clear();

        // Instantiate thẻ bài
        for (int i = 0; i < cardIndices.Count; i++)
        {
            int dataIndex = cardIndices[i]; // ID của thẻ (ví dụ 0)
            CardDataModel data = rawCardData[dataIndex]; 

            GameObject cardObj = Instantiate(cardPrefab, gridContainer);
            cardObj.SetActive(true);
            CardController controller = cardObj.GetComponent<CardController>();
            
            // Gán dữ liệu vào thẻ (ID, Index trên bàn, Ảnh, Chữ, Controller này)
            // Lưu ý: Bạn cần update script CardController để có hàm Init này
            controller.Init(data.id, i, data.englishWord, data.spriteName, this);
            
            activeCards.Add(controller);
        }

        // Init điểm số
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            playerScores.Add(p.ActorNumber, 0);
        }

        currentTurnActorNumber = startTurnActor;
        UpdateTurnUI();
        currentTimer = turnTimeLimit;
        isTimerRunning = true;
    }

    // 4. INPUT NGƯỜI CHƠI (Gửi lên Master)
    public void OnCardClicked(int cardIndex)
    {
        // Chỉ cho click nếu đúng lượt và chưa bị khóa
        if (PhotonNetwork.LocalPlayer.ActorNumber != currentTurnActorNumber) return;
        if (isProcessingMatch) return;

        // Gửi request lật thẻ
        photonView.RPC("RPC_RequestFlip", RpcTarget.MasterClient, cardIndex, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    // 5. LOGIC MASTER CLIENT (Xử lý luật game)
    [PunRPC]
    void RPC_RequestFlip(int cardIndex, int senderActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Logic check: Thẻ đã lật chưa? Đang wait không? (Double check)
        // Sau đó báo mọi người lật thẻ lên
        photonView.RPC("RPC_FlipCardVisual", RpcTarget.All, cardIndex);

        if (firstCardIndex == -1)
        {
            // Đây là thẻ đầu tiên
            firstCardIndex = cardIndex;
        }
        else
        {
            // Đây là thẻ thứ hai -> So sánh
            secondCardIndex = cardIndex;
            isProcessingMatch = true; // Khóa input
            isTimerRunning = false;   // Dừng timer tạm thời

            StartCoroutine(CheckMatchLogic(senderActorNumber));
        }
    }

    IEnumerator CheckMatchLogic(int playerID)
    {
        yield return new WaitForSeconds(1.0f); // Đợi 1s để người chơi nhìn thẻ

        CardController c1 = activeCards[firstCardIndex];
        CardController c2 = activeCards[secondCardIndex];

        if (c1.cardId == c2.cardId)
        {
            // ĐÚNG: Cộng điểm, Khóa thẻ vĩnh viễn
            if (playerScores.ContainsKey(playerID)) playerScores[playerID]++;
            totalPairsFound++;

            // Đồng bộ kết quả đúng
            photonView.RPC("RPC_MatchResult", RpcTarget.All, true, playerID, firstCardIndex, secondCardIndex, playerScores[playerID]);
            photonView.RPC("RPC_ResetCurrentTime", RpcTarget.All);


        }
        else
        {
            // SAI: Đổi lượt
            SwitchTurn();
            // Đồng bộ kết quả sai (úp lại)
            photonView.RPC("RPC_MatchResult", RpcTarget.All, false, currentTurnActorNumber, firstCardIndex, secondCardIndex, 0);
        }

        // Reset biến tạm
        firstCardIndex = -1;
        secondCardIndex = -1;
        isProcessingMatch = false;

        // Check End Game
        if (totalPairsFound >= MAX_PAIRS)
        {
            CheckGameOver();
        }
    }

    [PunRPC]
    void RPC_ResetCurrentTime()
    {
        currentTimer = turnTimeLimit;
        isTimerRunning = true;
    }

    [PunRPC]
    void RPC_FlipCardVisual(int index)
    {
        if(index >= 0 && index < activeCards.Count)
        {
            activeCards[index].FlipOpen();
        }
    }

    [PunRPC]
    void RPC_MatchResult(bool isMatch, int nextTurnActor, int idx1, int idx2, int newScore)
    {
        currentTurnActorNumber = nextTurnActor;
        UpdateTurnUI();
        if (isMatch)
        {
            // Ẩn/Khóa thẻ
            activeCards[idx1].LockCard();
            activeCards[idx2].LockCard();
            
            // Cập nhật điểm UI
            // Logic hiển thị điểm tùy thuộc vào ai vừa ghi điểm...
            // Ở đây code tắt update text
            UpdateScoreUI( nextTurnActor, newScore);
        }
        else
        {
            // Úp lại thẻ
            activeCards[idx1].FlipClose();
            activeCards[idx2].FlipClose();
        }
    }

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
        photonView.RPC("RPC_ResetCurrentTime", RpcTarget.All);

    }

    // 6. GAME OVER & DATABASE SAVE (Giữ format)
    void CheckGameOver()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Tính toán ai thắng
        int p1Score = 0; 
        int p2Score = 0;
        int p1Actor = -1; 
        int p2Actor = -1;

        // Lấy score từ Dictionary
        foreach(var kvp in playerScores) {
             // Logic lấy điểm hơi hardcode, cần cải thiện nếu muốn linh động
             // Nhưng để đơn giản: so sánh value
        }
        
        // Gửi RPC GameOver
        // Giả sử logic tính toán xong:
        // photonView.RPC("RPC_GameOver", RpcTarget.All, winnerActorNumber);
        
        // (Để code ngắn gọn, mình dùng lại logic so sánh đơn giản bên dưới RPC)
        photonView.RPC("RPC_EndGameProcess", RpcTarget.All);
    }

    [PunRPC]
    void RPC_EndGameProcess()
    {
        loadingPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        isTimerRunning = false;

        int myScore = playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber) ? playerScores[PhotonNetwork.LocalPlayer.ActorNumber] : 0;
        int enemyScore = 0;
        foreach(var kvp in playerScores) {
            if(kvp.Key != PhotonNetwork.LocalPlayer.ActorNumber) enemyScore = kvp.Value;
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

        StartCoroutine(RunCountdownLoadScene());
    }

    // --- CÁC HÀM TIỆN ÍCH ---

    void UpdateTurnUI()
    {
        bool isMyTurn = (PhotonNetwork.LocalPlayer.ActorNumber == currentTurnActorNumber);
        statusText.text = isMyTurn ? "Lượt của BẠN" : "Đối thủ đang chọn...";
        statusText.color = isMyTurn ? Color.green : Color.red;
    }
    
    void UpdateScoreUI(int playerId, int newScore)
    {
        if(newScore == 0) return;
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
        {
            p1ScoreText.text = newScore.ToString();
        }
        else
        {
            p2ScoreText.text = newScore.ToString();
        }
    }

    void Update()
    {
        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime;
            if (timerText != null) timerText.text = Mathf.CeilToInt(currentTimer).ToString();

            if (PhotonNetwork.IsMasterClient && currentTimer <= 0)
            {
                // Hết giờ -> Đổi lượt
                isTimerRunning = false;
                // Nếu đang lật dở 1 thẻ -> úp lại
                photonView.RPC("RPC_HandleTimeOut", RpcTarget.All);

            }
        }
    }
    
    [PunRPC]
    void RPC_HandleTimeOut()
    {
        if (firstCardIndex != -1)
        {
            activeCards[firstCardIndex].FlipClose();

        }
        SwitchTurn();
        UpdateTurnUI();
    }

    // Giữ nguyên logic Save Database và Load Scene của bạn
    void saveMatchDatabase(string resultState, EloCalculator.GameResult result, string otherName)
    {
        int randomRankPoint = EloCalculator.CalculateRatingChange(myPlayer.rank, otherPlayer.rank, result);
        RankDatabaseManager.Instance.SaveMatchHistory(resultState, randomRankPoint, otherName);
    }
    
    IEnumerator<WaitForSeconds> RunCountdownLoadScene()
    {
        for (int i = 3; i >= 0; i--)
        {
            gameOverTimerPanel.text = "Trở về trang chủ sau: " + i;
            yield return new WaitForSeconds(1f);
        }
        SceneManager.LoadScene("HomeScene");
    }

    // Logic UI Player giữ nguyên
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
        
        // Giả sử FriendItemUI có hàm SetupUI
        playerContainer.GetComponent<FriendItemUI>().SetupUI(nameTxt, avatarId, borderId, rankPoint);
    }

    private string GetSafeString(Player player, string key, string defaultValue = "0")
    {
        if (player.CustomProperties.TryGetValue(key, out object val)) return val.ToString();
        return defaultValue;
    }
}