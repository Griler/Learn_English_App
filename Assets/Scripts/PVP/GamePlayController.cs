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
    public GameObject gameWinPanel; // Panel che màn hình lúc tải
    public GameObject gameLosePanel; // Panel che màn hình lúc tải
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
    private void Start()
    {
        Hashtable resetProps = new Hashtable();
        resetProps.Add("IsLoaded", false); 
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
        rankChange = 0;
        isGameOver = false; 
        for (var i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.RemoveAllListeners(); // Xóa cũ
            answerButtons[i].onClick.AddListener(()=>
            {
                OnAnswerSelected(index);
            });
            answerButtons[i].gameObject.SetActive(false);
        }

        matchId = PhotonNetwork.CurrentRoom.Name;
        SetButtonsInteractable(false);
        InitUIPlayer();
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Kết nối thành công -> Bắt đầu lấy dữ liệu
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
    
    void LoadQuestionsFromFirebase() {
    reference.Child("questions").Child("list_test") 
        .GetValueAsync().ContinueWithOnMainThread(task => {
        
        if (task.IsFaulted)
        {
            Debug.LogError("Lỗi kết nối Firebase: " + task.Exception);
            return;
        }

        if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;

            // Debug xem có lấy được data thô về không
            if (snapshot.Value == null)
            {
                Debug.LogError("Không tìm thấy dữ liệu! Kiểm tra lại đường dẫn .Child()");
                return;
            }

            Debug.Log("Dữ liệu thô nhận được: " + snapshot.GetRawJsonValue());

            rawAllQuestions = new List<QuestionData>(); // Reset list tạm

            foreach (DataSnapshot child in snapshot.Children)
            {
                // Bọc try-catch để nếu 1 câu lỗi thì không chết cả game
                try 
                {
                    QuestionData newQ = new QuestionData();
                    
                    // Lấy text câu hỏi (Thêm kiểm tra null cho an toàn)
                    if (child.Child("questionText").Value != null)
                        newQ.questionText = child.Child("questionText").Value.ToString();
                    
                    // Lấy đáp án đúng
                    if (child.Child("correctAnswerIdx").Value != null)
                        newQ.correctAnswerIdx = int.Parse(child.Child("correctAnswerIdx").Value.ToString());

                    // Lấy mảng đáp án
                    List<string> answersList = new List<string>();
                    foreach(DataSnapshot ans in child.Child("answers").Children)
                    {
                        answersList.Add(ans.Value.ToString());
                    }
                    newQ.answers = answersList.ToArray();

                    // --- SỬA LỖI QUAN TRỌNG Ở ĐÂY ---
                    // Phải add vào rawAllQuestions (list tạm) chứ không phải allQuestions
                    rawAllQuestions.Add(newQ);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Lỗi parse 1 câu hỏi: " + ex.Message);
                }
            }
            
            Debug.Log($"Đã tải xong kho {rawAllQuestions.Count} câu hỏi! Chờ tín hiệu từ Master...");
            
            isDataLoaded = true;
            loadingStatusText.text = "Đang đợi người chơi khác...";

            // Báo cho mạng biết mình đã xong (Set Custom Property)
            Hashtable props = new Hashtable();
            props.Add("IsLoaded", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    });
}

    // --- RPC MỚI: NHẬN SEED VÀ TRỘN CÂU HỎI ---
    [PunRPC] 
    void RPC_SetupAndStartGame(int[] mixedIndices, int startTurnActor, PhotonMessageInfo info)
    {
        if (isGameStarted) return;
        isGameStarted = true;

        Debug.Log($"[SYNC] Đã nhận danh sách {mixedIndices.Length} câu từ Master.");

        // 1. SẮP XẾP DANH SÁCH GỐC (BẮT BUỘC)
        // Để đảm bảo index 0 của máy này giống hệt index 0 của máy kia
        // Sắp xếp theo nội dung câu hỏi (hoặc ID nếu có)
        rawAllQuestions = rawAllQuestions.OrderBy(q => q.questionText).ToList();

        // 2. TẠO LIST CÂU HỎI THI ĐẤU
        allQuestions = new List<QuestionData>();

        foreach (int index in mixedIndices)
        {
            // Kiểm tra an toàn để không bị lỗi Index Out Of Range
            if (index >= 0 && index < rawAllQuestions.Count)
            {
                allQuestions.Add(rawAllQuestions[index]);
            }
        }

        Debug.Log($"Đã setup xong {allQuestions.Count} câu hỏi.");

        // 3. Bật nút trả lời
        for (var i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(true);
        }

        // 4. Bắt đầu logic game
        InitGameLogic(startTurnActor);
    }
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Chỉ Master mới có quyền kiểm tra và ra lệnh Start
        if (PhotonNetwork.IsMasterClient)
        {
            // Kiểm tra xem property thay đổi có phải là "IsLoaded" không
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
                if (!(bool)isLoaded) return; // Có ông chưa xong -> Dừng, không làm gì cả
            }
            else
            {
                return; // Chưa có key -> Chưa xong
            }
        }
        Debug.Log("Tất cả đã sẵn sàng! Bắt đầu đếm ngược.");
        photonView.RPC("RPC_StartCountdown", RpcTarget.All);
    }
    
    [PunRPC]
    void RPC_StartCountdown()
    {
        StartCoroutine(Co_RunCountdownAndStart());
    }

    IEnumerator<WaitForSeconds> Co_RunCountdownAndStart()
    {
        // Tắt loading, hiện số đếm ngược
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
        
        Debug.LogError(PhotonNetwork.LocalPlayer.IsMasterClient);
        if (PhotonNetwork.IsMasterClient)
        {
            if (rawAllQuestions.Count > 0)
            {
                int[] shuffledIndices = getShuffledIndices();
                int startActor = PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)].ActorNumber;

                Debug.LogError("tao seed sau count down");
                photonView.RPC("RPC_SetupAndStartGame", RpcTarget.All, shuffledIndices.ToArray(), startActor);
            }
            else
            {
                Debug.LogError("List câu hỏi rỗng, không thể bắt đầu game!");
            }
        }
    }

    int[] getShuffledIndices()
    {
        int gameSeed = UnityEngine.Random.Range(1, 999999);
        List<int> indices = new List<int>();
        for (int i = 0; i < rawAllQuestions.Count; i++)
        {
            indices.Add(i);
        }
        System.Random sysRnd = new System.Random(gameSeed);
        rawAllQuestions = rawAllQuestions.OrderBy(q => q.questionText).ToList();
        var shuffledIndices = indices.OrderBy(x => sysRnd.Next()).ToList();

        // 4. Cắt lấy 100 câu (nếu nhiều hơn)
        if (shuffledIndices.Count > 100)
        {
            shuffledIndices = shuffledIndices.Take(100).ToList();
        }

        return shuffledIndices.ToArray();
    }
    
    void InitGameLogic(int startActor)
    {
        // Setup mạng sống
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if(!playerLives.ContainsKey(p.ActorNumber))
                playerLives.Add(p.ActorNumber, 3);
        }
        UpdateLivesUI();

        // Thiết lập lượt đi (đã được Master random và gửi xuống)
        currentTurnActorNumber = startActor;
        
        // Load câu đầu tiên lên UI
        RPC_SyncState(0, currentTurnActorNumber);
    }

    // --- PHẦN LOGIC NGƯỜI CHƠI ---x
    
    // Khi người chơi bấm nút chọn đáp án
    void OnAnswerSelected(int index)
    {
        // Kiểm tra xem có phải lượt của mình không
        if (PhotonNetwork.LocalPlayer.ActorNumber != currentTurnActorNumber)
        {
            Debug.Log("Không phải lượt của bạn!");
            return;
        }

        // Vô hiệu hóa nút để tránh spam
        SetButtonsInteractable(false);

        // Gửi đáp án lên Master Client để chấm điểm (Nếu mình là Master thì tự gửi cho chính mình)
        photonView.RPC("RPC_SubmitAnswer", RpcTarget.MasterClient, index);
    }

    // --- PHẦN LOGIC SERVER (Chạy trên máy Master Client) ---

    [PunRPC]
    // --- PHẦN LOGIC SERVER (Chạy trên máy Master Client) ---
    void RPC_SubmitAnswer(int answerIndex, PhotonMessageInfo info)
    {
        // Nhận request từ Client, chuyển tiếp vào hàm xử lý logic
        int senderID = info.Sender.ActorNumber;
        ProcessAnswerLogic(senderID, answerIndex);
    }

    // Hàm này xử lý logic chung (được gọi bởi RPC hoặc bởi Timer)
    void ProcessAnswerLogic(int playerID, int answerIndex)
    {
        // 1. Nếu không phải Master thì không được xử lý logic game
        if (!PhotonNetwork.IsMasterClient) return;

        // 2. Nếu Timer đã dừng (tức là đã có người trả lời hoặc hết giờ trước đó), bỏ qua
        if (!isTimerRunning) return;

        // STOP TIMER NGAY LẬP TỨC để tránh bị gọi trùng
        isTimerRunning = false;

        // Logic check đúng sai y hệt cũ
        bool isCorrect = false;
        
        // Nếu answerIndex = -1 nghĩa là Hết giờ (tự quy định) -> Luôn sai
        if (answerIndex >= 0 && answerIndex < 4)
        {
            isCorrect = (answerIndex == allQuestions[currentQuestionIndex].correctAnswerIdx);
        }
        if (isCorrect)
        {
            // ĐÚNG: Load câu mới
            currentQuestionIndex++;
            if (currentQuestionIndex >= allQuestions.Count) currentQuestionIndex = 0;
            SwitchTurn();
        }
        else
        {
            photonView.RPC("RPC_SyncLive", RpcTarget.All, playerID);
            if (CheckGameOverCondition())
            {
                Debug.LogError("vào game over");
                return;
            }
            currentQuestionIndex++;
            if (currentQuestionIndex >= allQuestions.Count) currentQuestionIndex = 0;
            SwitchTurn();
        }

        // Đồng bộ lại mọi thứ xuống Client
        photonView.RPC("RPC_SyncState", RpcTarget.All, currentQuestionIndex, currentTurnActorNumber);
        
        // Đồng bộ Mạng
        int[] livesArray = new int[PhotonNetwork.PlayerList.Length];
        int[] actorArray = new int[PhotonNetwork.PlayerList.Length];
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            actorArray[i] = PhotonNetwork.PlayerList[i].ActorNumber;
            livesArray[i] = playerLives[PhotonNetwork.PlayerList[i].ActorNumber];
        }
        photonView.RPC("RPC_SyncLives", RpcTarget.All, actorArray, livesArray);
    }

    [PunRPC]
    void RPC_SyncLive(int playerID)
    {
        if (playerLives.ContainsKey(playerID))
        {
            playerLives[playerID]--;
            if (PhotonNetwork.LocalPlayer.ActorNumber == currentTurnActorNumber)
            {
                int myLive = playerLives[playerID];
                myLives[myLive].sprite = disableHeart;
                Debug.LogError("tru mang ban còn " + playerLives[playerID]);
            }
            else
            {
                int otherLive = playerLives[playerID];
                enemyLives[otherLive].sprite = disableHeart;
                Debug.LogError("tru mang doi phuong còn " + playerLives[playerID]);
            }
        }

    }
    private void Update()
    {
        // Chỉ chạy timer khi cờ đang bật
        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime;
            
            // Cập nhật UI Timer cho mượt (hiển thị số nguyên)
            if(timerText != null)
                timerText.text = Mathf.CeilToInt(currentTimer).ToString();

            // LOGIC CHECK HẾT GIỜ (Chỉ Master Client được quyền check)
            if (PhotonNetwork.IsMasterClient)
            {
                if (currentTimer <= 0)
                {
                    Debug.Log("Hết giờ! Xử thua lượt này.");
                    // Gọi hàm xử lý với đáp án -1 (đại diện cho sai/hết giờ)
                    // Người bị xử thua chính là người đang giữ lượt (currentTurnActorNumber)
                    ProcessAnswerLogic(currentTurnActorNumber, -1);
                }
            }
        }
    }
    
    void SwitchTurn()
    {
        // Tìm ID người kia
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber != currentTurnActorNumber)
            {
                currentTurnActorNumber = p.ActorNumber;
                break;
            }
        }
    }

    bool CheckGameOverCondition()
    {
        int survivorActorNumber = -1; // ID người còn sống (người thắng)
        bool isAnyoneDead = false;    // Cờ đánh dấu có ai chết chưa

        // 1. Duyệt danh sách để xem tình trạng hiện tại
        foreach (var kvp in playerLives)
        {
            if (kvp.Value > 0)
            {
                survivorActorNumber = kvp.Key; // Tìm thấy người còn sống
            }
            else
            {
                isAnyoneDead = true; // Tìm thấy người đã chết (máu <= 0)
            }
        }

        // 2. XỬ LÝ KẾT THÚC NGAY LẬP TỨC
        if (isAnyoneDead)
        {
            // Trường hợp A: Có người sống, có người chết -> Người sống thắng
            if (survivorActorNumber != -1)
            {
                Debug.Log("Game Over: Winner is " + survivorActorNumber);
                // Gửi RPC báo người thắng (survivorActorNumber)
                photonView.RPC("RPC_GameOver", RpcTarget.All, "WINNER", survivorActorNumber);
            }
            // Trường hợp B: Cả 2 cùng chết (hiếm, nhưng đề phòng lỗi logic) -> Hòa
            else
            {
                Debug.Log("Game Over: DRAW (Both died)");
                photonView.RPC("RPC_GameOver", RpcTarget.All, "DRAW", -1);
            }

            return true; // Game kết thúc
        }

        // 3. Chưa ai chết -> Game tiếp tục
        return false;
    }

    // --- CÁC HÀM ĐỒNG BỘ UI (CLIENT) ---

    [PunRPC]
    void RPC_SyncState(int questionIdx, int turnActorID)
    {
        currentQuestionIndex = questionIdx;
        currentTurnActorNumber = turnActorID;

        // 1. Hiển thị nội dung câu hỏi
        QuestionData data = allQuestions[currentQuestionIndex];
        questionText.text = data.questionText;
        for (int i = 0; i < 4; i++)
        {
            answerTexts[i].text = data.answers[i];
        }

        // 2. Cập nhật trạng thái nút bấm (Chỉ bật nút nếu đúng lượt của mình)
        bool isMyTurn = (PhotonNetwork.LocalPlayer.ActorNumber == currentTurnActorNumber);
        SetButtonsInteractable(isMyTurn);
        Debug.Log("Lượt của bạn:" + isMyTurn);
        statusText.text = isMyTurn ? "Lượt của BẠN" : "Lượt đối thủ...";
        statusText.color = isMyTurn ? Color.green : Color.red;
        currentTimer = timeLimit; // Đặt lại 5s
        isTimerRunning = true;    // Bắt đầu đếm
    }

    [PunRPC]
    void RPC_SyncLives(int[] actors, int[] lives)
    {
        for(int i=0; i<actors.Length; i++)
        {
            // Cập nhật Dictionary local để hiển thị
            if(playerLives.ContainsKey(actors[i]))
                playerLives[actors[i]] = lives[i];
            else 
                playerLives.Add(actors[i], lives[i]);
        }
    }

    [PunRPC]
    void RPC_GameOver(string msg, int survivorActorNumber)
    {
        loadingPanel.SetActive(false);
        isGameOver = true;
        if (msg == "DRAW")
        {
            saveMatchDatabase("DRAW",EloCalculator.GameResult.Draw,otherPlayer.name);
        }
        bool amIWinner = (PhotonNetwork.LocalPlayer.ActorNumber == survivorActorNumber);
        UpdateMissionState(GlobalData.MissionKeys.P2P);
        if (amIWinner)
        {
            saveMatchDatabase("WIN",EloCalculator.GameResult.Win,otherPlayer.name);
            gameWinPanel.SetActive(true);
            gameWinPanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
            UpdateMissionState(GlobalData.MissionKeys.WIN_P2P);
        }
        else
        {
            saveMatchDatabase("LOSE",EloCalculator.GameResult.Loss,otherPlayer.name);
            gameLosePanel.SetActive(true);
            gameLosePanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
        }
        isTimerRunning = false;
        isGameStarted = false;
        SetButtonsInteractable(false);
    }
    private async void UpdateMissionState(string nameMission)
    {
        await FirebaseDatabaseManager.Instance.CompleteMissionById(nameMission);
    }


// Thêm hàm Callback
    public override void OnLeftRoom()
    {
        Debug.Log("Đã thoát phòng ques Game, về Home.");
        FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.ONLINE);
        SceneManager.LoadScene("HomeScene");
    }

    void saveMatchDatabase(string resultState,EloCalculator.GameResult result,string otherName)
    {
        rankChange = EloCalculator.CalculateRatingChange(myPlayer.rank,otherPlayer.rank,result);
        if (NetworkGameState.CurrentJoinType == NetworkGameState.JoinType.FriendInvite)
            rankChange = 0;
        RankDatabaseManager.Instance.SaveMatchHistory(matchId,resultState, rankChange, otherName, "Đáp Nhanh");
    }

    void UpdateLivesUI()
    {
        foreach (Image myLife in myLives)
        {
            myLife.sprite = enableHeart;
        } 
        foreach (Image enemy in enemyLives)
        {
            enemy.sprite = enableHeart;
        }
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

        
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            
            //Kiểm tra xem player này có phải là TUI không?
            if (player.IsLocal) 
            {
                // Nếu là tui -> Nhét vào bên TRÁI
                UpdateSinglePlayerUI(player,player1Container);
            }
            else 
            {
                // Nếu là người khác -> Nhét vào bên PHẢI
                UpdateSinglePlayerUI(player,player2Container);
            }
        }
    }

    void UpdateSinglePlayerUI(Player player,GameObject playerContainer)
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
        playerContainer.GetComponent<FriendItemUI>().SetupUI(nameTxt,avatarId,borderId,rankPoint);
    }

    // Hàm tiện ích: Lấy giá trị int từ CustomProperties
    // Hàm này bất chấp server gửi int hay string, nó đều trả về string an toàn
    private string GetSafeString(Player player, string key, string defaultValue = "0")
    {
        // 1. Kiểm tra có Key đó không
        if (player.CustomProperties.TryGetValue(key, out object val))
        {
            // 2. Dù là số 10 hay chữ "10", lệnh này đều biến nó thành string "10"
            return val.ToString(); 
        }
    
        // 3. Nếu không tìm thấy, trả về giá trị mặc định
        return defaultValue;
    }

    private void OnDestroy()
    {
        for (var i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].onClick.RemoveAllListeners(); // Xóa cũ
        }
    }
    
    // --- XỬ LÝ KHI ĐỐI THỦ THOÁT GAME ---
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(isGameOver) return;
        Debug.Log("Người chơi " + otherPlayer.NickName + " đã thoát game.");

        // 1. Dừng Timer ngay lập tức
        isTimerRunning = false;
        isGameStarted = false;
        SetButtonsInteractable(false);

        // 2. Xử lý thắng cuộc cho người còn lại (là TUI)
        // Vì đối thủ out nên tui thắng mặc định
        statusText.text = "Đối thủ đã thoát! Bạn thắng!";
        
        // Gọi hàm xử lý thắng giống như khi hết máu
        // Lưu ý: Cần truyền ID của chính mình vào làm survivor
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        
        // Tái sử dụng logic thắng cuộc
        // Gọi trực tiếp vì không còn ai để RPC nữa (hoặc RPC cũng được nếu muốn chuẩn flow)
        HandleOpponentLeftWin(myActorNumber);
    }

    void HandleOpponentLeftWin(int winnerActorNumber)
    {
        // Tính điểm Elo (giả sử thắng thì cộng điểm)
        // Lưu lại lịch sử đấu: "OPP_DISCONNECT" hoặc "WIN"
        saveMatchDatabase("WIN", EloCalculator.GameResult.Win, otherPlayer.name);
        
        // Cập nhật nhiệm vụ
        UpdateMissionState(GlobalData.MissionKeys.WIN_P2P);
        UpdateMissionState(GlobalData.MissionKeys.P2P);

        // Hiển thị Panel Thắng
        gameWinPanel.SetActive(true);
        if(gameWinPanel.GetComponent<GameOverPanelController>() != null)
        {
            gameWinPanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
        }
    }
}