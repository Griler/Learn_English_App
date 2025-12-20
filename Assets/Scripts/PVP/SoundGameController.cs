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
using UnityEngine.Networking; // Cần thiết để tải Audio
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.U2D;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[System.Serializable]
public class GameQuestionData
{
    public int id;
    public string questionText;
    public string imageUrl;
    public List<string> answers;
    public int correctAnswerIdx;
    public string correctAnswer;

    // Lưu Audio vào RAM
    [System.NonSerialized] public AudioClip audioClip;
}

public class SoundGameController : MonoBehaviourPunCallbacks
{
    [Header("--- UI REFERENCES ---")] public Button playSoundBtn;
    public AudioSource audioSource;
    public SpriteAtlas spriteAtlas;
    public TextMeshProUGUI statusText;
    public Button[] optionButtons;
    public Image[] optionImages;

    [Header("--- SCORES & UI ---")] public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p2ScoreText;
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingStatusText;
    public TextMeshProUGUI countdownText;
    public GameObject gameWinPanel;
    public GameObject gameLosePanel;
    public GameObject player1Container;
    public GameObject player2Container;
    public TextMeshProUGUI questionText;

    [Header("--- GAME SETTINGS ---")] public int targetScoreToWin = 5;
    public float penaltyTime = 1.0f;
    public int questionLimit = 20; // Số câu hỏi sẽ chơi

    // DATA
    private List<GameQuestionData> rawData = new List<GameQuestionData>(); // Full database
    public List<GameQuestionData> playList = new List<GameQuestionData>(); // 20 câu đã lọc

    // STATE
    private int currentQuestionIndex = 0;
    private bool isRoundActive = false;
    private bool isPenalty = false;
    private bool isGameStarted = false;
    private string matchId = "";

    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    [NotNull] private UserDataPVP myPlayer = new UserDataPVP();
    [NotNull] private UserDataPVP otherPlayer = new UserDataPVP();

    DatabaseReference reference;

    private void Start()
    {
        Hashtable resetProps = new Hashtable();
        resetProps.Add("FirebaseLoaded", false);
        resetProps.Add("AudioLoaded", false);
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
        rankChange = 0;
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
                FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.INMATCH);
                LoadFullJsonFromFirebase();
            }
            else Debug.LogError("Firebase Error: " + task.Result);
        });
    }

    // --- BƯỚC 1: TẢI JSON ---
    void LoadFullJsonFromFirebase()
    {
        loadingStatusText.text = "Đang tải danh sách từ vựng...";
        reference.Child("sound_data").GetValueAsync().ContinueWithOnMainThread(task =>
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
                        if (child.Child("correctAnswer").Value != null)
                            item.correctAnswer = child.Child("correctAnswer").Value.ToString();
                        if (child.Child("correctAnswerIdx").Value != null)
                            item.correctAnswerIdx = int.Parse(child.Child("correctAnswerIdx").Value.ToString());
                        if (child.Child("questionText").Value != null)
                            item.questionText = (child.Child("questionText").Value.ToString());

                        item.answers = new List<string>();
                        foreach (DataSnapshot ans in child.Child("answers").Children)
                            item.answers.Add(ans.Value.ToString());

                        rawData.Add(item);
                    }
                    catch (Exception ex)
                    {
                    }
                }

                Hashtable props = new Hashtable();
                props.Add("FirebaseLoaded", true);
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                loadingStatusText.text = "Đang đồng bộ dữ liệu...";
            }
        });
    }

    // --- BƯỚC 2: ĐỒNG BỘ ---
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (changedProps.ContainsKey("FirebaseLoaded"))
        {
            if (CheckAllPlayersProp("FirebaseLoaded"))
            {
                int gameSeed = UnityEngine.Random.Range(0, 999999);
                photonView.RPC("RPC_ProcessAndDownloadAudio", RpcTarget.All, gameSeed);
            }
        }

        if (changedProps.ContainsKey("AudioLoaded"))
        {
            if (CheckAllPlayersProp("AudioLoaded"))
            {
                photonView.RPC("RPC_StartCountdown", RpcTarget.All);
            }
        }
    }

    private bool CheckAllPlayersProp(string key)
    {
        if (PhotonNetwork.PlayerList.Length < 2) return false;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue(key, out object val) || !(bool)val) return false;
        }

        return true;
    }

    // --- BƯỚC 3: LỌC LIST & TẢI AUDIO (GOOGLE DỊCH) ---
    [PunRPC]
    void RPC_ProcessAndDownloadAudio(int seed)
    {
        System.Random rnd = new System.Random(seed);
        playList = rawData.OrderBy(x => rnd.Next()).Take(questionLimit).ToList();

        StartCoroutine(DownloadAudioSequence());
    }

    IEnumerator DownloadAudioSequence()
    {
        int count = 0;
        foreach (var q in playList)
        {
            count++;
            loadingStatusText.text = $"Đang tải audio ({count}/{playList.Count})";

            bool isSuccess = false;

            // URL Google Dịch (Unofficial API)
            string word = UnityWebRequest.EscapeURL(q.correctAnswer);
            string url =
                $"https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&text_len=32&client=tw-ob&q={word}&tl=en";

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    q.audioClip = DownloadHandlerAudioClip.GetContent(www);
                    isSuccess = true;
                }
                else
                {
                    Debug.LogWarning($"Lỗi tải '{q.correctAnswer}': {www.error}");
                }
            }

            // Delay để tránh bị Google chặn (Lỗi 429)
            if (isSuccess) yield return new WaitForSeconds(0.2f); // Nghỉ 0.2s nếu ok
            else yield return new WaitForSeconds(1.5f); // Nghỉ 1.5s nếu lỗi
        }

        loadingStatusText.text = "Đợi người chơi khác...";
        Hashtable props = new Hashtable();
        props.Add("AudioLoaded", true);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // --- BƯỚC 4: GAMEPLAY ---
    [PunRPC]
    void RPC_StartCountdown()
    {
        StartCoroutine(Co_RunCountdownAndStart());
    }

    IEnumerator Co_RunCountdownAndStart()
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
            photonView.RPC("RPC_SetupGameLogic", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_SetupGameLogic()
    {
        if (isGameStarted) return;
        isGameStarted = true;

        playerScores.Clear();
        foreach (Player p in PhotonNetwork.PlayerList) playerScores.Add(p.ActorNumber, 0);
        UpdateScoreUI();

        if (PhotonNetwork.IsMasterClient) StartNewRound(0);
    }

    void StartNewRound(int roundIdx)
    {
        photonView.RPC("RPC_SyncRound", RpcTarget.All, roundIdx);
    }

    [PunRPC]
    void RPC_SyncRound(int roundIdx)
    {
        currentQuestionIndex = roundIdx;
        GameQuestionData data = playList[currentQuestionIndex];
        // 1. Phát Audio
        if (data.audioClip != null)
        {
            audioSource.clip = data.audioClip;
            audioSource.Play();
        }

        // 2. Load Hình
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].interactable = true;
            optionButtons[i].image.color = Color.white;
            optionButtons[i].gameObject.SetActive(true);
            if (i < data.answers.Count)
            {
                Sprite sp = spriteAtlas.GetSprite(data.answers[i].ToLower());
                if (sp != null)
                {
                    optionImages[i].sprite = sp;
                    optionImages[i].preserveAspect = true;
                }
            }
        }

        questionText.text = data.questionText;
        isRoundActive = true;
        isPenalty = false;
        statusText.text = "Nghe và chọn hình đúng!";
        statusText.color = Color.black;

    }

    // --- INPUT & LOGIC ---
    void OnOptionClicked(int btnIndex)
    {
        if (!isRoundActive || isPenalty) return;
        photonView.RPC("RPC_SubmitAnswer", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, btnIndex);
    }

    [PunRPC]
    void RPC_SubmitAnswer(int senderActor, int btnIndex)
    {

        GameQuestionData data = playList[currentQuestionIndex];
        bool isCorrect = (btnIndex == data.correctAnswerIdx);
        if(isCorrect) playerScores[senderActor]++;
        if (!PhotonNetwork.IsMasterClient) return;
        if (isCorrect)
        {
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
        int nextIdx = (currentQuestionIndex + 1) % playList.Count;
        StartNewRound(nextIdx);
    }

    // --- FEEDBACK ---
    [PunRPC]
    void RPC_RoundResult(int winnerActor, bool isCorrect, int btnIndex)
    {
        Debug.LogError("vao update diem");
        UpdateScoreUI();
        isRoundActive = false;
        if (winnerActor == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            statusText.text = "CHÍNH XÁC!";
            statusText.color = Color.green;
        }
        else
        {
            string winnerName = (winnerActor == otherPlayer.actorId) ? otherPlayer.name : "Đối thủ";
            statusText.text = $"{winnerName} GHI ĐIỂM!";
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
        statusText.text = "SAI RỒI!";
        statusText.color = Color.red;
        optionButtons[btnIndex].interactable = false;
        yield return new WaitForSeconds(penaltyTime);
        isPenalty = false;
        statusText.text = "Chọn lại!";
        optionButtons[btnIndex].interactable = true;
    }

    // --- END GAME & UTILS ---
    [PunRPC]
    void RPC_EndGame(int winnerActor)
    {
        loadingPanel.SetActive(false);
        isRoundActive = false;
        bool amIWinner = (PhotonNetwork.LocalPlayer.ActorNumber == winnerActor);
        UpdateMissionState(GlobalData.MissionKeys.P2P);
        if (amIWinner)
        {
            saveMatchDatabase("WIN", EloCalculator.GameResult.Win, otherPlayer.name);
            gameWinPanel.SetActive(true);
            gameWinPanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
            UpdateMissionState(GlobalData.MissionKeys.WIN_P2P);
        }
        else
        {
            saveMatchDatabase("LOSE", EloCalculator.GameResult.Loss, otherPlayer.name);
            gameLosePanel.SetActive(true);
            gameLosePanel.GetComponent<GameOverPanelController>().ShowGameOver(rankChange);
        }

    }

    void PlayCurrentSound()
    {
        if (audioSource.clip != null) audioSource.Play();
    }
    

    void UpdateScoreUI()
    {
        Debug.LogError(playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber) + "|" + playerScores[PhotonNetwork.LocalPlayer.ActorNumber]);
        int myScore = playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber)
            ? playerScores[PhotonNetwork.LocalPlayer.ActorNumber]
            : 0;
        int enemyScore = 0;
        foreach (var kvp in playerScores)
            if (kvp.Key != PhotonNetwork.LocalPlayer.ActorNumber)
                enemyScore = kvp.Value;
        p1ScoreText.text = $"Tôi: {myScore}/{targetScoreToWin}";
        p2ScoreText.text = $"Đối thủ: {enemyScore}/{targetScoreToWin}";
    }

    void InitUIPlayer()
    {
        loadingPanel.SetActive(true);
        gameLosePanel.SetActive(false);
        gameWinPanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        loadingStatusText.text = "ĐANG TẢI DỮ LIỆU...";
        foreach (Player p in PhotonNetwork.PlayerList)
            UpdateSinglePlayerUI(p, p.IsLocal ? player1Container : player2Container);
    }

    void UpdateSinglePlayerUI(Player p, GameObject container)
    {
        if (p.IsLocal)
        {
            myPlayer.name = p.NickName;
            myPlayer.rank = int.Parse(GetSafeString(p, "Rank"));
        }
        else
        {
            otherPlayer.name = p.NickName;
            otherPlayer.rank = int.Parse(GetSafeString(p, "Rank"));
            otherPlayer.actorId = p.ActorNumber;
        }

        container.GetComponent<FriendItemUI>().SetupUI(p.NickName, GetSafeString(p, "AvatarID"),
            GetSafeString(p, "BorderID"), GetSafeString(p, "Rank"));
    }

    string GetSafeString(Player p, string key)
    {
        return p.CustomProperties.ContainsKey(key) ? p.CustomProperties[key].ToString() : "0";
    }

    private int rankChange = 0;
    void saveMatchDatabase(string state, EloCalculator.GameResult res, string oName)
    { 
        rankChange = EloCalculator.CalculateRatingChange(myPlayer.rank, otherPlayer.rank, res);
        if (NetworkGameState.CurrentJoinType == NetworkGameState.JoinType.FriendInvite)
            rankChange = 0;
        RankDatabaseManager.Instance.SaveMatchHistory(matchId, state,rankChange, oName, "Nghe Từ");
    }
    
    public override void OnLeftRoom()
    {
        FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.ONLINE);
        SceneManager.LoadScene("HomeScene");
    }
    
    private async void UpdateMissionState(string nameMission)
    {
     
        await FirebaseDatabaseManager.Instance.CompleteMissionById(nameMission);
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Người chơi " + otherPlayer.NickName + " đã thoát game.");

        // 1. Dừng Timer ngay lập tức
        isGameStarted = false;

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