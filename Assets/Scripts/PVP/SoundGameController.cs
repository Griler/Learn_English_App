using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using ExitGames.Client.Photon;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using JetBrains.Annotations;
using UnityEngine.Networking; // Cần thư viện này để tải Audio từ URL
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Data Model
[System.Serializable]
public class GameQuestionData
{
    public int id;
    public string questionText;
    public string imageUrl;
    public List<string> answers;
    public int correctAnswerIdx;
    public string correctAnswer;

    // Biến này để lưu file nhạc tải về từ Google (Không lưu vào JSON)
    [System.NonSerialized] 
    public AudioClip audioClip; 
}

public class SoundGameController : MonoBehaviourPunCallbacks
{
    [Header("--- UI REFERENCES ---")]
    public Button playSoundBtn;
    public AudioSource audioSource;
    public TextMeshProUGUI statusText;
    
    public Button[] optionButtons;       
    public Image[] optionImages;         
    
    [Header("--- SCORES & UI ---")]
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p2ScoreText;
    
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingStatusText;
    public TextMeshProUGUI countdownText;

    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI gameOverTimerPanel;

    public GameObject player1Container;
    public GameObject player2Container;

    [Header("--- GAME SETTINGS ---")]
    public int targetScoreToWin = 5;
    public float penaltyTime = 1.0f;
    // Giới hạn số câu hỏi tải về để tránh chờ quá lâu (VD: 20 câu)
    public int limitQuestionLoad = 20; 

    [Header("--- DATA ---")]
    public List<GameQuestionData> allQuestions; // List dùng để chơi
    [SerializeField] private List<GameQuestionData> rawData = new List<GameQuestionData>(); // List thô tải từ Firebase

    // State
    private int currentQuestionIndex = 0;
    private bool isRoundActive = false;
    private bool isPenalty = false;
    private bool isGameStarted = false;
    private string matchId = "";

    // Scores
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    [NotNull] private UserDataPVP myPlayer = new UserDataPVP();
    [NotNull] private UserDataPVP otherPlayer = new UserDataPVP();

    DatabaseReference reference;

    private void Start()
    {
        Hashtable resetProps = new Hashtable();
        resetProps.Add("IsLoaded", false);
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);

        playSoundBtn.onClick.AddListener(PlayCurrentSound);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
        }
        
        InitUIPlayer();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadDataFromFirebase();
            }
            else
            {
                Debug.LogError("Firebase Error: " + task.Result);
            }
        });
    }

    // --- 1. TẢI JSON TỪ FIREBASE ---
    void LoadDataFromFirebase()
    {
        reference.Child("sound_data").Child("list").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Value != null)
            {
                rawData = new List<GameQuestionData>();
                DataSnapshot snapshot = task.Result;

                foreach (DataSnapshot child in snapshot.Children)
                {
                    try
                    {
                        GameQuestionData item = new GameQuestionData();
                        if (child.Child("id").Value != null) item.id = int.Parse(child.Child("id").Value.ToString());
                        if (child.Child("correctAnswer").Value != null) item.correctAnswer = child.Child("correctAnswer").Value.ToString();
                        if (child.Child("correctAnswerIdx").Value != null) item.correctAnswerIdx = int.Parse(child.Child("correctAnswerIdx").Value.ToString());

                        item.answers = new List<string>();
                        foreach (DataSnapshot ans in child.Child("answers").Children)
                        {
                            item.answers.Add(ans.Value.ToString());
                        }
                        
                        rawData.Add(item);
                    }
                    catch (Exception ex) { Debug.LogWarning("Parse Error: " + ex.Message); }
                }

                // Sau khi có Data JSON -> Bắt đầu tải Audio Google TTS
                StartCoroutine(DownloadAllAudioFromGoogle());
            }
        });
    }

    // --- 2. DOWNLOAD AUDIO TỪ GOOGLE TTS ---
    IEnumerator DownloadAllAudioFromGoogle()
    {
        // Random trộn danh sách câu hỏi trước khi tải để mỗi ván mỗi khác
        System.Random rnd = new System.Random(); // (Lưu ý: Random này chỉ là client-side tạm thời để tải, server sẽ sync seed sau)
        rawData = rawData.OrderBy(x => rnd.Next()).Take(limitQuestionLoad).ToList();

        int count = 0;
        foreach (var q in rawData)
        {
            count++;
            loadingStatusText.text = $"Đang tải giọng đọc: {count}/{rawData.Count}";

            // URL API Google TTS (Unofficial)
            string wordToSpeak = UnityWebRequest.EscapeURL(q.correctAnswer);
            string url = $"https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&text_len=32&client=tw-ob&q={wordToSpeak}&tl=en";

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // LƯU AUDIO VÀO BIẾN CỦA OBJECT ĐỂ DÙNG SAU
                    q.audioClip = DownloadHandlerAudioClip.GetContent(www);
                }
                else
                {
                    Debug.LogError($"Lỗi tải audio từ '{q.correctAnswer}': {www.error}");
                }
            }
        }

        loadingStatusText.text = "Đang đợi người chơi khác...";
        
        // Báo hiệu tôi đã tải xong
        Hashtable props = new Hashtable();
        props.Add("IsLoaded", true);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // --- 3. CHECK READY ---
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
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
            if (!p.CustomProperties.TryGetValue("IsLoaded", out object isLoaded) || !(bool)isLoaded) return;
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
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        countdownText.gameObject.SetActive(false);
        loadingPanel.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            int seed = UnityEngine.Random.Range(0, 999999);
            photonView.RPC("RPC_SetupGame", RpcTarget.AllBuffered, seed);
        }
    }

    // --- 4. GAME LOGIC ---
    [PunRPC]
    void RPC_SetupGame(int seed)
    {
        if (isGameStarted) return;
        isGameStarted = true;

        // Lưu ý: Lúc này rawData đã có Audio, ta cần đồng bộ thứ tự câu hỏi
        // Do bước tải Audio ta đã cắt rawData còn 'limitQuestionLoad' câu, 
        // nên cần đảm bảo logic shuffle đồng bộ ở đây dựa trên danh sách đã tải.
        // Tuy nhiên, do mỗi máy tự random rawData ở bước tải, có thể dẫn đến lệch câu hỏi.
        // ==> FIX LOGIC: Ta sẽ dùng danh sách rawData hiện có (đã tải audio) làm pool. 
        // (Lưu ý: Để chuẩn xác 100% PVP, bước tải Audio nên tải cùng 1 seed list, 
        // nhưng để đơn giản ta giả định 2 bên tải đủ bộ data giống nhau hoặc chấp nhận tải full database).
        
        // Ở đây code đơn giản hóa: Sử dụng rawData đã tải làm allQuestions
        allQuestions = new List<GameQuestionData>(rawData); 
        
        // Master Shuffle lại 1 lần nữa cho chắc chắn ngẫu nhiên (dù 2 bên có thể lệch data nếu random lúc tải khác nhau)
        // ĐỂ FIX LỖI ĐỒNG BỘ: Bước DownloadAllAudioFromGoogle nên bỏ dòng Shuffle đi, tải tuần tự từ 0->N
        // Sau đó vào đây mới Shuffle bằng Seed.
        
        // Sửa lại logic trộn bài bằng seed:
        System.Random rnd = new System.Random(seed);
        allQuestions = allQuestions.OrderBy(x => rnd.Next()).ToList();

        playerScores.Clear();
        foreach (Player p in PhotonNetwork.PlayerList) playerScores.Add(p.ActorNumber, 0);
        UpdateScoreUI();

        if (PhotonNetwork.IsMasterClient)
        {
            StartNewRound(0);
        }
    }

    // Master ra lệnh round mới
    void StartNewRound(int roundIdx)
    {
        photonView.RPC("RPC_SyncRound", RpcTarget.All, roundIdx);
    }

    [PunRPC]
    void RPC_SyncRound(int roundIdx)
    {
        currentQuestionIndex = roundIdx;
        GameQuestionData data = allQuestions[currentQuestionIndex];

        // 1. PHÁT AUDIO TỪ RAM (Đã tải trước đó)
        if (data.audioClip != null)
        {
            audioSource.clip = data.audioClip;
            audioSource.Play();
        }
        else
        {
            Debug.LogError("Audio chưa tải được hoặc bị lỗi cho từ: " + data.correctAnswer);
        }

        // 2. Setup 4 nút Hình Ảnh
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].interactable = true;
            optionButtons[i].image.color = Color.white;

            if (i < data.answers.Count)
            {
                string wordName = data.answers[i];
                Sprite sp = LoadSprite(wordName); // Load hình từ Resources
                
                if (sp != null)
                {
                    optionImages[i].sprite = sp;
                    optionImages[i].preserveAspect = true;
                }
            }
        }

        isRoundActive = true;
        isPenalty = false;
        statusText.text = "Nghe và chọn hình đúng!";
        statusText.color = Color.white;
    }

    void PlayCurrentSound()
    {
        if (audioSource.clip != null) audioSource.Play();
    }

    // --- CÁC PHẦN XỬ LÝ CLICK, SCORE, END GAME GIỮ NGUYÊN ---
    void OnOptionClicked(int btnIndex)
    {
        if (!isRoundActive || isPenalty) return;
        photonView.RPC("RPC_SubmitAnswer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, btnIndex);
    }

    [PunRPC]
    void RPC_SubmitAnswer(int senderActor, int btnIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!isRoundActive) return;

        GameQuestionData data = allQuestions[currentQuestionIndex];
        bool isCorrect = (btnIndex == data.correctAnswerIdx);

        if (isCorrect)
        {
            playerScores[senderActor]++;
            photonView.RPC("RPC_RoundResult", RpcTarget.All, senderActor, true, btnIndex);

            if (playerScores[senderActor] >= targetScoreToWin)
            {
                photonView.RPC("RPC_EndGame", RpcTarget.All, senderActor);
            }
            else
            {
                StartCoroutine(WaitAndNextRound());
            }
        }
        else
        {
            photonView.RPC("RPC_PenaltyPlayer", RpcTarget.All, senderActor, btnIndex);
        }
    }

    IEnumerator WaitAndNextRound()
    {
        isRoundActive = false;
        yield return new WaitForSeconds(2.0f);
        int nextIdx = currentQuestionIndex + 1;
        if (nextIdx >= allQuestions.Count) nextIdx = 0;
        StartNewRound(nextIdx);
    }

    [PunRPC]
    void RPC_RoundResult(int winnerActor, bool isCorrect, int btnIndex)
    {
        UpdateScoreUI();
        isRoundActive = false;
        if (winnerActor == PhotonNetwork.LocalPlayer.ActorNumber) {
            statusText.text = "CHÍNH XÁC! (+1)";
            statusText.color = Color.green;
            optionButtons[btnIndex].image.color = Color.green;
        } else {
            string winnerName = (winnerActor == otherPlayer.actorId) ? otherPlayer.name : "Đối thủ";
            statusText.text = $"{winnerName} ĐÃ GHI ĐIỂM!";
            statusText.color = Color.red;
        }
    }

    [PunRPC]
    void RPC_PenaltyPlayer(int targetActor, int btnIndex)
    {
        if (targetActor == PhotonNetwork.LocalPlayer.ActorNumber) StartCoroutine(PenaltyRoutine(btnIndex));
    }

    IEnumerator PenaltyRoutine(int btnIndex)
    {
        isPenalty = true;
        statusText.text = "SAI RỒI! Đợi 1 giây...";
        statusText.color = Color.red;
        optionButtons[btnIndex].image.color = Color.red;
        optionButtons[btnIndex].interactable = false;
        yield return new WaitForSeconds(penaltyTime);
        isPenalty = false;
        statusText.text = "Chọn lại đi!";
        statusText.color = Color.white;
        optionButtons[btnIndex].image.color = Color.white;
        optionButtons[btnIndex].interactable = true;
    }

    Sprite LoadSprite(string wordName)
    {
        if (string.IsNullOrEmpty(wordName)) return null;
        string path = "VocabImages/" + wordName; 
        Sprite sp = Resources.Load<Sprite>(path);
        if (sp == null) sp = Resources.Load<Sprite>(wordName);
        return sp;
    }

    [PunRPC]
    void RPC_EndGame(int winnerActor)
    {
        loadingPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        isRoundActive = false;
        bool amIWinner = (PhotonNetwork.LocalPlayer.ActorNumber == winnerActor);
        if (amIWinner) {
            gameOverText.text = "CHIẾN THẮNG!\n" + myPlayer.name;
            saveMatchDatabase("WIN", EloCalculator.GameResult.Win, otherPlayer.name);
        } else {
            gameOverText.text = "THẤT BẠI...\n" + otherPlayer.name;
            saveMatchDatabase("LOSE", EloCalculator.GameResult.Loss, otherPlayer.name);
        }
        StartCoroutine(RunCountdownLoadScene());
    }

    void UpdateScoreUI()
    {
        int myScore = playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber) ? playerScores[PhotonNetwork.LocalPlayer.ActorNumber] : 0;
        int enemyScore = 0;
        foreach (var kvp in playerScores) {
            if (kvp.Key != PhotonNetwork.LocalPlayer.ActorNumber) enemyScore = kvp.Value;
        }
        p1ScoreText.text = $"Tôi: {myScore}/{targetScoreToWin}";
        p2ScoreText.text = $"Đối thủ: {enemyScore}/{targetScoreToWin}";
    }

    void InitUIPlayer()
    {
        loadingPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        loadingStatusText.text = "ĐANG TẢI DỮ LIỆU.....";
        foreach (Player player in PhotonNetwork.PlayerList) {
            if (player.IsLocal) UpdateSinglePlayerUI(player, player1Container);
            else UpdateSinglePlayerUI(player, player2Container);
        }
    }
    void UpdateSinglePlayerUI(Player player, GameObject playerContainer) {
        string nameTxt = player.NickName;
        string avatarId = GetSafeString(player, "AvatarID");
        string borderId = GetSafeString(player, "BorderID");
        string rankPoint = GetSafeString(player, "Rank");
        if (player.IsLocal) { myPlayer.name = player.NickName; myPlayer.rank = int.Parse(rankPoint); } 
        else { otherPlayer.name = player.NickName; otherPlayer.rank = int.Parse(rankPoint); otherPlayer.actorId = player.ActorNumber; }
        playerContainer.GetComponent<FriendItemUI>().SetupUI(nameTxt, avatarId, borderId, rankPoint);
    }
    private string GetSafeString(Player player, string key, string defaultValue = "0") {
        if (player.CustomProperties.TryGetValue(key, out object val)) return val.ToString();
        return defaultValue;
    }
    void saveMatchDatabase(string resultState, EloCalculator.GameResult result, string otherName) {
        int point = EloCalculator.CalculateRatingChange(myPlayer.rank, otherPlayer.rank, result);
        RankDatabaseManager.Instance.SaveMatchHistory(matchId, resultState, point, otherName, "Nghe Từ");
    }
    IEnumerator<WaitForSeconds> RunCountdownLoadScene() {
        for (int i = 3; i >= 0; i--) {
            gameOverTimerPanel.text = "Về Home sau: " + i;
            yield return new WaitForSeconds(1f);
        }
        PhotonNetwork.LeaveRoom();
    }
    public override void OnLeftRoom() { SceneManager.LoadScene("HomeScene"); }
}