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
    public GameObject gameOverPanel; // Panel che màn hình lúc tải
    public TextMeshProUGUI loadingStatusText;  // Text: "Đang tải...", "Đợi người khác..."
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI gameOverTimerPanel;
    
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

    [NotNull] private userDataPVP myPlayer = new userDataPVP();
    [NotNull] private userDataPVP otherPlayer = new userDataPVP();
    
    // Mạng của 2 người chơi (Key: ActorNumber, Value: Lives)
    private Dictionary<int, int> playerLives = new Dictionary<int, int>();
    DatabaseReference reference;
    [SerializeField] private List<QuestionData> rawAllQuestions = new List<QuestionData>();
    private void Start()
    {
        for (var i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.RemoveAllListeners(); // Xóa cũ
            answerButtons[i].onClick.AddListener(()=>
            {
                OnAnswerSelected(index);
            });
        }
        
        SetButtonsInteractable(false);
        InitUIPlayer();
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Kết nối thành công -> Bắt đầu lấy dữ liệu
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadQuestionsFromFirebase();
            }
            else
            {
                Debug.LogError("Lỗi Firebase: " + dependencyStatus);
            }
        });
    }
    
    void LoadQuestionsFromFirebase() {
    reference.Child("questions").Child("list") 
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
    void RPC_SetupAndStartGame(int seed, int startTurnActor, PhotonMessageInfo info)
    {
        Debug.Log("Nhận được Seed: " + seed + ". Bắt đầu trộn câu hỏi...");

        // 1. Tạo bộ random với Seed được đồng bộ
        System.Random rnd = new System.Random(seed);

        // 2. Trộn danh sách (Shuffle) dựa trên Seed này
        // (Đây là thuật toán trộn Fisher-Yates chuẩn)
        allQuestions = rawAllQuestions.OrderBy(x => rnd.Next()).ToList();

        // 3. Cắt lấy 100 câu đầu tiên
        int questionCountToTake = 100;
        if (allQuestions.Count > questionCountToTake)
        {
            allQuestions = allQuestions.Take(questionCountToTake).ToList();
        }

        Debug.Log($"Đã chốt {allQuestions.Count} câu hỏi cho ván này.");

        // 4. Bắt đầu các logic game như cũ
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

        if (PhotonNetwork.IsMasterClient)
        {
            if (rawAllQuestions.Count > 0)
            {
                int gameSeed = UnityEngine.Random.Range(0, 999999);
                int startActor = PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)].ActorNumber;
                photonView.RPC("RPC_SetupAndStartGame", RpcTarget.Others, gameSeed, startActor);
                photonView.RPC("RPC_SetupAndStartGame", RpcTarget.MasterClient, gameSeed, startActor);
            }
            else
            {
                Debug.LogError("List câu hỏi rỗng, không thể bắt đầu game!");
            }
        }
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

    // --- PHẦN LOGIC NGƯỜI CHƠI ---
    
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
            // SAI (hoặc Hết giờ): Trừ mạng
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

            if (CheckGameOverCondition())
            {
                Debug.LogError("vào game over");
                return;
            }
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
        int deadCount = 0;
        int survivorActorNumber = -1; // Lưu ID người còn sống

        // 1. Duyệt qua tất cả người chơi để đếm số người chết/sống
        foreach (var kvp in playerLives)
        {
            if (kvp.Value <= 0)
            {
                deadCount++;
            }
            else
            {
                survivorActorNumber = kvp.Key; // Lưu lại ID người này để tuyên bố thắng
            }
        }

        // 2. LOGIC HÒA (Ưu tiên kiểm tra trước): Cả 2 đều hết mạng
        // Trường hợp này xảy ra khi A chết, B trả lời sai và cũng chết theo.
        if (deadCount >= 2)
        {
            Debug.Log("Game Over: DRAW!");
            // Gửi RPC thông báo Hòa
            photonView.RPC("RPC_GameOver", RpcTarget.All, "DRAW", survivorActorNumber);
            return true; // Game kết thúc
        }

        // 3. LOGIC CÓ NGƯỜI THẮNG: Chỉ có 1 người chết, người kia còn sống
        if (deadCount == 1 && survivorActorNumber != -1)
        {
            // --- QUAN TRỌNG: XỬ LÝ LUẬT "VỚT VÁT" CỦA BẠN ---
        
            // Nếu bạn muốn "A chết nhưng B vẫn được trả lời nốt câu hỏi để xem có bị Hòa không":
            // Bạn cần thêm biến kiểm tra xem B đã trả lời chưa.
            // Ví dụ: if (!isTurnFinished) return false; 
        
            // Còn nếu chơi luật chuẩn (Ai về 0 trước là thua ngay lập tức):
            Debug.Log("Game Over: Winner is " + survivorActorNumber);
        
            // Gửi RPC thông báo người thắng (kèm ID người thắng để hiển thị)
            photonView.RPC("RPC_GameOver", RpcTarget.All, "WINNER_" + survivorActorNumber, survivorActorNumber);
            return true; // Game kết thúc
        }

        // 4. Chưa ai chết hoặc cả 2 vẫn sống -> Game tiếp tục
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
        gameOverPanel.SetActive(true);
        if (msg == "DRAW")
        {
            gameOverText.text = "CẢ HAI HOÀ NHAU";
            saveMatchDatabase("DRAW",EloCalculator.GameResult.Draw,otherPlayer.name);
        }
        bool amIWinner = (PhotonNetwork.LocalPlayer.ActorNumber == survivorActorNumber);
        if (amIWinner)
        {
            gameOverText.text = "NGƯỜI CHIẾN THẮNG LÀ: \n" + myPlayer.name;
            saveMatchDatabase("WIN",EloCalculator.GameResult.Win,otherPlayer.name);
        }
        else
        {
            gameOverText.text = "NGƯỜI CHIẾN THẮNG LÀ: \n" + otherPlayer.name;
            saveMatchDatabase("LOSE",EloCalculator.GameResult.Loss,otherPlayer.name);
        }
        isTimerRunning = false;
        SetButtonsInteractable(false);
        StartCoroutine(RunCountdownLoadScene());
    }
    
    IEnumerator<WaitForSeconds> RunCountdownLoadScene()
    {
        gameOverTimerPanel.text = "Trờ về trang chủ sau: 3";
        yield return new WaitForSeconds(1f);
        gameOverTimerPanel.text = "Trờ về trang chủ sau: 2";
        yield return new WaitForSeconds(1f);
        gameOverTimerPanel.text = "Trờ về trang chủ sau: 1";
        yield return new WaitForSeconds(1f);
        gameOverTimerPanel.text = "Trờ về trang chủ sau: 0";
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("HomeScene");
    }

    void saveMatchDatabase(string resultState,EloCalculator.GameResult result,string otherName)
    {
        int randomRankPoint = EloCalculator.CalculateRatingChange(myPlayer.rank,otherPlayer.rank,result);
        RankDatabaseManager.Instance.SaveMatchHistory(resultState, randomRankPoint, otherName);
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
        gameOverPanel.SetActive(false);
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
    
}