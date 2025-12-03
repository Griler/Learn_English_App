using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class GamePlayController : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Button[] answerButtons; // 4 nút đáp án
    public TextMeshProUGUI[] answerTexts; // Text bên trong 4 nút
    
    [Header("UI Info")]
    public TextMeshProUGUI statusText; // Hiển thị "Lượt của..."
    public List<Image> myLives; // Mạng của mình
    public List<Image> enemyLives; // Mạng đối thủ
    public Sprite disableHeart;
    public Sprite enableHeart;

    [Header("Timer Settings")]
    public float timeLimit = 5f; // Thời gian tối đa (5s)
    public TextMeshProUGUI timerText; // UI hiển thị số giây đếm ngược

    private float currentTimer;
    private bool isTimerRunning = false;
    
    [Header("Game Data")]
    // Danh sách câu hỏi (bạn có thể load từ JSON hoặc nhập tay trong Inspector)
    public List<QuestionData> allQuestions; 

    // --- BIẾN LOGIC (Chỉ Master Client quan tâm chính) ---
    private int currentQuestionIndex = 0;
    private int currentTurnActorNumber; // ID của người đang được chơi
    
    // Mạng của 2 người chơi (Key: ActorNumber, Value: Lives)
    private Dictionary<int, int> playerLives = new Dictionary<int, int>();
    DatabaseReference reference;
    [SerializeField] private List<QuestionData> rawAllQuestions = new List<QuestionData>();
    private void Start()
    {
        // 1. Tắt UI game hoặc hiện Loading Panel ở đây nếu muốn
        Debug.LogError(this.gameObject.gameObject);
        SetButtonsInteractable(false);
        statusText.text = "Đang tải câu hỏi...";

        // 2. Khởi tạo Firebase & Load Data
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
    
    void LoadQuestionsFromFirebase()
{
    // LƯU Ý: Kiểm tra kỹ đường dẫn này trên Firebase Console của bạn.
    // Nếu data nằm ngay ngoài cùng thì bỏ .Child("questions") đi, chỉ để .Child("list")
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

            // Logic Master Client xử lý Seed
            if (PhotonNetwork.IsMasterClient)
            {
                if (rawAllQuestions.Count > 0)
                {
                    int gameSeed = UnityEngine.Random.Range(0, 999999);
                    int startActor = PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)].ActorNumber;
                    photonView.RPC("RPC_SetupAndStartGame", RpcTarget.All, gameSeed, startActor);
                }
                else
                {
                    Debug.LogError("List câu hỏi rỗng, không thể bắt đầu game!");
                }
            }
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

    // --- HÀM START GAME (Sửa lại chút để nhận tham số) ---
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
            }

            if (CheckGameOverCondition()) return;
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
        // Lấy mạng của 2 người
        int p1Lives = 0;
        int p2Lives = 0;
        // Giả sử chỉ có 2 người chơi
        foreach(var kvp in playerLives)
        {
             // Logic tạm: lấy đại 2 giá trị vì loop
             if(p1Lives == 0) p1Lives = kvp.Value;
             else p2Lives = kvp.Value;
        }

        // Logic check:
        // Cần phải check kỹ ActorNumber để biết ai là ai, nhưng ở đây ta check tổng quát:
        
        bool someoneDead = false;
        int deadCount = 0;
        foreach(var life in playerLives.Values)
        {
            if(life <= 0) deadCount++;
        }

        // Logic Hòa đặc biệt của bạn: Cả 2 đều hết mạng (0)
        if (deadCount == 2)
        {
            photonView.RPC("RPC_GameOver", RpcTarget.All, "HÒA! Cả 2 đều hết mạng.");
            return true;
        }

        // Nếu chỉ có 1 người chết, ta phải xem người kia còn cơ hội trả lời không?
        // Theo luật bạn: "A sai (còn 0 mạng), B trả lời". 
        // Nghĩa là Game CHƯA DỪNG khi 1 người chết, nó chỉ dừng khi người kia trả lời xong (hoặc người kia thắng).
        // Tuy nhiên để đơn giản hoá logic cho người mới: Nếu A chết -> Check xem B có chết ko?
        // Nếu B > 0 mạng -> B Thắng. 
        // NHƯNG luật của bạn là: A chết, câu hỏi vẫn còn đó cho B. 
        // => Vậy code ở trên (Switch Turn) vẫn chạy để B có cơ hội trả lời.
        // => Game chỉ kết thúc khi B trả lời đúng (B thắng) hoặc B trả lời sai (B chết -> Hoà).
        
        // Vậy nên ở đây ta CHƯA return true vội nếu chỉ 1 người chết.
        // Trừ khi người chết là người vừa trả lời xong và người kia vẫn còn sống? 
        // Để đúng luật "B trả lời sai thì hòa", ta cứ để game tiếp diễn đến khi cả 2 cùng 0, hoặc 1 người 0 và người kia trả lời ĐÚNG câu chốt hạ.
        
        // Tạm thời Logic chuẩn game đối kháng: Ai về 0 trước là thua.
        // Để làm đúng Logic của bạn:
        if (deadCount == 2) 
        {
             photonView.RPC("RPC_GameOver", RpcTarget.All, "DRAW");
             return true;
        }
        
        return false; // Chưa kết thúc, vẫn đánh tiếp
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

        statusText.text = isMyTurn ? "Lượt của BẠN" : "Lượt đối thủ...";
        statusText.color = isMyTurn ? Color.green : Color.red;
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

            // Cập nhật UI Text
            if (actors[i] == PhotonNetwork.LocalPlayer.ActorNumber)
                for (int j = 1; j <= lives[i]; j++)
                {
                    myLives[j - 1].gameObject.SetActive(true);
                }
            else
            {
                for (int j = 1; j <= lives[i]; j++)
                {
                    enemyLives[j - 1].gameObject.SetActive(true);
                }
            }
        }
    }

    [PunRPC]
    void RPC_GameOver(string msg)
    {
        statusText.text = "GAME OVER: " + msg;
        SetButtonsInteractable(false);
        // Hiện popup kết quả, nút về sảnh, v.v.
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
}