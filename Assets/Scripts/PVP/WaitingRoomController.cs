using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class WaitingRoomController : MonoBehaviourPunCallbacks
{
    [Header("UI References Player 1")]
    public GameObject player1IconReady;
    public GameObject player1Container;
    public TextMeshProUGUI player1TextStatus;
    
    [Header("UI References Player 2")]
    public GameObject player2IconReady;
    public GameObject player2Container;
    public TextMeshProUGUI player2TextStatus;

    [Header("UI References Room")]
    public Button readyButton;
    public Button outButton;
    public Button botModeButton;
    public Button botAddButton;
    public Button findMatchButton;
    public TextMeshProUGUI readyButtonText;
    public TextMeshProUGUI roomName;
    public TextMeshProUGUI statusText;
    
    [Header("UI Game Mode")]
    public TMP_Dropdown dropdownModeGame;
    private const string MODE_KEY = "gm";

    [Header("Bot Settings")]
    public float maxWaitTime = 10f; // Chờ 15s không có ai thì gọi Bot
    private float currentWaitTimer;
    private bool isTimerRunning = false;
    private bool isBotMode = false;
    private void Start()
    {
        BotMatchHelper.Reset(); // Reset trạng thái Bot mỗi khi vào Room

        if (readyButton) readyButton.onClick.AddListener(OnClick_ToggleReady);
        if (outButton) outButton.onClick.AddListener(onClickOutRoom);    
        if (botAddButton) botAddButton.onClick.AddListener(addBot);    
        if (botModeButton) botModeButton.onClick.AddListener(modeBot);    
        if (findMatchButton) findMatchButton.onClick.AddListener(findMatch);    
        dropdownModeGame.onValueChanged.AddListener(OnDropdownChanged);
        botAddButton.gameObject.SetActive(false);
        readyButton.gameObject.SetActive(false);
        UpdatePlayerListUI();
        
        // Reset trạng thái nút bấm
        bool isReady = (bool)PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("IsReady") && 
                       (bool)PhotonNetwork.LocalPlayer.CustomProperties["IsReady"];
        UpdateReadyButtonUI(isReady);
        UpdateRoomInfo();
        GetAndShowGameMode();
        
        if(PhotonNetwork.CurrentRoom != null) {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }
        if (!PhotonNetwork.IsMasterClient)
        {
            findMatchButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(true);
            botModeButton.interactable = false;
        }
    }

    private void Update()
    {
        // LOGIC ĐẾM NGƯỢC TẠO BOT
        if (isTimerRunning && PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.PlayerList.Length > 1)
            {
                isTimerRunning = false; // Có người vào rồi thì dừng đếm
                return;
            }

            currentWaitTimer += Time.deltaTime;
            if(statusText) statusText.text = $"Đang tìm đối thủ... ({Mathf.Ceil(currentWaitTimer)})";
        }
    }

// Thêm hàm hiển thị Bot vào WaitingRoomController

    void StartBotMatch()
    {
        readyButton.interactable = true;
        Debug.Log("Hết giờ chờ! Hệ thống tự tạo Bot.");
        statusText.gameObject.SetActive(true);
        if(statusText) statusText.text = "Đã tạo bot";

        // 1. Đóng phòng (Fake full room)
        if(PhotonNetwork.CurrentRoom != null) {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        // 2. Setup dữ liệu Bot
        string myRankStr = GetSafeString(PhotonNetwork.LocalPlayer, "Rank");
        BotMatchHelper.SetupBotMatch(int.Parse(myRankStr));

        // 3. [MỚI] HIỂN THỊ BOT LÊN UI NGAY LẬP TỨC
        if (player2Container != null)
        {
            player2Container.SetActive(true);
            // Gọi hàm SetupUI của FriendItemUI (giả sử script này gắn trên prefab)
            player2Container.GetComponent<FriendItemUI>().SetupUI(
                BotMatchHelper.BotName,
                BotMatchHelper.BotAvatarID,
                BotMatchHelper.BotBorderID,
                BotMatchHelper.BotRank.ToString()
            );
        }
        
        if (player2TextStatus != null) player2TextStatus.gameObject.SetActive(false);

        // 4. Delay 2 giây để người chơi kịp nhìn thấy đối thủ trước khi vào game
    }

    IEnumerator DelayStartGame()
    {
        if(statusText) statusText.text = "Đang vào game...";
        yield return new WaitForSeconds(2.0f); // Chờ 2 giây
        StartGame();
    }    
    // --- [LOGIC GỬI DỮ LIỆU GAME MODE] ---
    void OnDropdownChanged(int index)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int modeId = index + 1; 
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props[MODE_KEY] = modeId;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }
    
    void GetAndShowGameMode()
    {
        dropdownModeGame.interactable = PhotonNetwork.IsMasterClient;
        var roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        if (roomProps.ContainsKey(MODE_KEY))
        {
            int mode = (int)roomProps[MODE_KEY];
            int targetIndex = mode - 1;
            if (targetIndex < 0) targetIndex = 0;
            
            if (dropdownModeGame.value != targetIndex)
            {
                dropdownModeGame.value = targetIndex;
                dropdownModeGame.RefreshShownValue(); 
            }
            if(roomName) roomName.text = dropdownModeGame.options[targetIndex].text;
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(MODE_KEY)) GetAndShowGameMode();
    }

    void UpdatePlayerListUI()
    {
        Player[] players = PhotonNetwork.PlayerList;
        Player myPlayer = null;
        Player otherPlayer = null;

        foreach (Player p in players)
        {
            if (p.IsLocal) myPlayer = p;
            else otherPlayer = p;
        }

        if (myPlayer != null)
        {
            player1Container.SetActive(true);
            UpdateSinglePlayerUI(myPlayer, player1Container, player1IconReady);
            if(player1TextStatus) player1TextStatus.gameObject.SetActive(false);
        }

        if (otherPlayer != null)
        {
            player2Container.SetActive(true);
            UpdateSinglePlayerUI(otherPlayer, player2Container, player2IconReady);
            if(player2TextStatus) player2TextStatus.gameObject.SetActive(false);
            
            // Có người thật -> Hủy Timer Bot
            isTimerRunning = false;
            if(statusText) statusText.text = "Đã tìm thấy người chơi!";
        }
        else
        {
            player2Container.SetActive(false);
            if(player2TextStatus)
            {
                player2TextStatus.gameObject.SetActive(true);
                player2TextStatus.text = "Trống";
            }
        }
        dropdownModeGame.interactable = PhotonNetwork.IsMasterClient;
    }

    void UpdateSinglePlayerUI(Player player,GameObject playerContainer, GameObject readyObj)
    {
        string nameTxt = player.NickName;
        string avatarId = GetSafeString(player, "AvatarID"); 
        string borderId = GetSafeString(player, "BorderID");
        string rankPoint = GetSafeString(player, "Rank");
        playerContainer.GetComponent<FriendItemUI>().SetupUI(nameTxt,avatarId,borderId,rankPoint);
        
        bool isReady = GetBoolProperty(player, "IsReady");
        readyObj.SetActive(isReady); 
    }

    private string GetSafeString(Player player, string key, string defaultValue = "0")
    {
        if (player.CustomProperties.TryGetValue(key, out object val)) return val.ToString(); 
        return defaultValue;
    }

    private bool GetBoolProperty(Player player, string key)
    {
        if (player.CustomProperties.TryGetValue(key, out object tempValue)) return (bool)tempValue;
        return false;
    }

    public void OnClick_ToggleReady()
    {
        if (isBotMode)
        {
            readyButton.interactable = false;
            botModeButton.interactable = false;
            UpdateReadyButtonUI(true);
            StartCoroutine(DelayStartGame());
        }
        else
        {
            object isReadyObj;
            bool currentReady = false;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsReady", out isReadyObj))
            {
                currentReady = (bool)isReadyObj;
            }
            bool newReadyState = !currentReady;
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["IsReady"] = newReadyState;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            UpdateReadyButtonUI(newReadyState);
        }
    }

    void UpdateReadyButtonUI(bool isReady)
    {
        outButton.interactable = !isReady;
        readyButtonText.text = isReady ? "CANCEL" : "READY";
        readyButton.image.color = isReady ? Color.gray : Color.green;
        if (PhotonNetwork.IsMasterClient) dropdownModeGame.interactable = !isReady;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerListUI();
        readyButton.gameObject.SetActive(true);
        findMatchButton.gameObject.SetActive(false);
        botModeButton.interactable = false;
        GetAndShowGameMode(); 
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        UpdatePlayerListUI();
        CheckAllPlayersReady();
    }

    void CheckAllPlayersReady()
    {
        if (BotMatchHelper.IsBotMatch) return; // Nếu đang mode Bot thì bỏ qua check Ready

        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.PlayerList.Length < 2) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isReadyObj;
            if (p.CustomProperties.TryGetValue("IsReady", out isReadyObj))
            {
                if (!(bool)isReadyObj) return; 
            }
            else return; 
        }
        StartGame();
    }

    void StartGame()
    {
        if(statusText) statusText.text = "Đang vào game...";
        
        // Đảm bảo đóng phòng
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        int mode = 0;
        if(PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(MODE_KEY))
        {
            mode = (int)PhotonNetwork.CurrentRoom.CustomProperties[MODE_KEY];
        }
        else mode = 1; 

        switch (mode)
        {
            case 1: PhotonNetwork.LoadLevel("PVPScene"); break;
            case 2: PhotonNetwork.LoadLevel("PairScene"); break;
            case 3: PhotonNetwork.LoadLevel("SoundPickScene"); break;
            default: PhotonNetwork.LoadLevel("PVPScene"); break;
        }
    }
    
    void UpdateRoomInfo()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            Debug.Log($"Đang ở phòng: {name}");
        }
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        readyButton.gameObject.SetActive(false);
        ResetMyReadyState();
        UpdatePlayerListUI();
        botModeButton.interactable = true;
        if (statusText) statusText.text = "Đối thủ đã thoát. Đang tìm người mới...";
        
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true; 
            PhotonNetwork.CurrentRoom.IsVisible = true;
            dropdownModeGame.interactable = true;
            isTimerRunning = true;
            currentWaitTimer = 0;
        }

        if (NetworkGameState.CurrentJoinType == NetworkGameState.JoinType.FriendInvite)
        {
            NetworkGameState.CurrentJoinType = NetworkGameState.JoinType.RandomMatchmaking;
        }
    }

    void ResetMyReadyState()
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["IsReady"] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        UpdateReadyButtonUI(false);
    }
    
    void onClickOutRoom()
    {
        PhotonNetwork.LeaveRoom();
    }
    
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("HomeScene");
    }

    void findMatch()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
        }
        
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.PlayerList.Length == 1)
        {
            currentWaitTimer = 0;
            isTimerRunning = true;
            statusText.gameObject.SetActive(true);
            if(statusText) statusText.text = $"Đang tìm đối thủ... ({Mathf.Ceil(currentWaitTimer)})";
        }
        findMatchButton.gameObject.SetActive(false);
    }

    void addBot()
    {
        botAddButton.gameObject.SetActive(false);
        readyButton.gameObject.SetActive(true);
        StartBotMatch();
    }

    void modeBot()
    {
        if (isBotMode == false)
        {
            isBotMode = true;
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }
            botAddButton.gameObject.SetActive(true);
            player2TextStatus.text = "";
            isTimerRunning = false;
            statusText.gameObject.SetActive(false);
            botAddButton.gameObject.SetActive(true);
            findMatchButton.gameObject.SetActive(false);
        }
        else
        {
            isBotMode = false;
            BotMatchHelper.IsBotMatch = false;
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;
                PhotonNetwork.CurrentRoom.IsVisible = true;
            }
            player2Container.SetActive(false);
            botAddButton.gameObject.SetActive(false);
            player2TextStatus.text = "Trống";
            readyButton.gameObject.SetActive(false);
            findMatchButton.gameObject.SetActive(true);
        }
    }
}