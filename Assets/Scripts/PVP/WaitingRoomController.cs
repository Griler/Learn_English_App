using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;

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
    public TextMeshProUGUI readyButtonText;
    public TextMeshProUGUI roomName;
    public TextMeshProUGUI statusText;
    
    [Header("UI Game Mode")]
    public TMP_Dropdown dropdownModeGame;
    private const string MODE_KEY = "gm";
    
    private void Start()
    {
        if (readyButton)
        {
            readyButton.onClick.AddListener(OnClick_ToggleReady);
        }

        if (outButton)
        {
            outButton.onClick.AddListener(onClickOutRoom);    
        }
        dropdownModeGame.onValueChanged.AddListener(OnDropdownChanged);

        UpdatePlayerListUI();
        
        // Reset trạng thái nút bấm
        bool isReady = (bool)PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("IsReady") && 
                       (bool)PhotonNetwork.LocalPlayer.CustomProperties["IsReady"];
        UpdateReadyButtonUI(isReady);
        UpdateRoomInfo();
        
        GetAndShowGameMode();
    }
    
    // --- [LOGIC MỚI] GỬI DỮ LIỆU LÊN SERVER KHI MASTER ĐỔI DROPDOWN ---
    void OnDropdownChanged(int index)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Map từ Index của Dropdown sang ID của Mode
            // (Dựa theo logic cũ của bạn: Case 1 -> value 0, Case 2 -> value 1...)
            // Vậy ngược lại: Index 0 -> Mode 1, Index 1 -> Mode 2...
            int modeId = index + 1; 

            Debug.Log($"Master thay đổi mode: Index {index} -> ModeID {modeId}");

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

            Debug.Log("WaitingRoom: Sync Mode từ Server = " + mode);
            
            int targetIndex = 0;
            switch (mode)
            {
                case 1: targetIndex = 0; break;
                case 2: targetIndex = 1; break;
                case 3: targetIndex = 2; break;
                default: targetIndex = 0; break;
            }
            roomName.text = dropdownModeGame.options[targetIndex].text;
            if (dropdownModeGame.value != targetIndex)
            {
                dropdownModeGame.value = targetIndex;
                dropdownModeGame.RefreshShownValue(); 
            }
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // Nếu cái thay đổi chứa key "gm" thì cập nhật UI
        if (propertiesThatChanged.ContainsKey(MODE_KEY))
        {
            GetAndShowGameMode();
        }
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
            if(player1TextStatus != null) player1TextStatus.gameObject.SetActive(false);
        }

        if (otherPlayer != null)
        {
            player2Container.SetActive(true);
            UpdateSinglePlayerUI(otherPlayer, player2Container, player2IconReady);
            if(player2TextStatus != null) player2TextStatus.gameObject.SetActive(false);
        }
        else
        {
            player2Container.SetActive(false);
            if(player2TextStatus != null)
            {
                player2TextStatus.gameObject.SetActive(true);
                player2TextStatus.text = "Waiting for opponent...";
            }
        }
        
        // Mỗi lần có người vào/ra, check lại quyền tương tác Dropdown (nếu Host out, người này thành Host thì được mở khóa)
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

    // --- EVENT NÚT READY ---
    public void OnClick_ToggleReady()
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

    void UpdateReadyButtonUI(bool isReady)
    {
        outButton.interactable = !isReady;
        readyButtonText.text = isReady ? "CANCEL" : "READY";
        readyButton.image.color = isReady ? Color.gray : Color.green;
        
        if (PhotonNetwork.IsMasterClient)
        {
            dropdownModeGame.interactable = !isReady;
        }
    }

    // --- CÁC CALLBACK CỦA PHOTON ---

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerListUI();
        // Khi người mới vào, cần đảm bảo họ nhìn thấy đúng Mode hiện tại
        
        // (Thực ra họ tự chạy Start->GetAndShowGameMode rồi, nhưng gọi lại cho chắc)
        GetAndShowGameMode(); 
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        UpdatePlayerListUI();
        CheckAllPlayersReady();
    }

    void CheckAllPlayersReady()
    {
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
        statusText.text = "Tất cả đã sẵn sàng! Đang vào game...";
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        
        int mode = 0;
        if(PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(MODE_KEY))
        {
            mode = (int)PhotonNetwork.CurrentRoom.CustomProperties[MODE_KEY];
        }
        else
        {
            mode = 1; 
        }

        Debug.Log("Start Game với Mode ID: " + mode);

        switch (mode)
        {
            case 1: // Tương ứng dropdown index 0
                PhotonNetwork.LoadLevel("PVPScene");
                break;
            case 2: // Tương ứng dropdown index 1
                PhotonNetwork.LoadLevel("PairScene");
                break;
            case 3: // Tương ứng dropdown index 2
                 PhotonNetwork.LoadLevel("SoundPickScene");
                break;
            default:
                PhotonNetwork.LoadLevel("PVPScene");
                break;
        }
    }
    
    void UpdateRoomInfo()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            Debug.Log($"Đang ở phòng: {name} | Số người: {currentPlayers}");
        }
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ToastSystem.Instance.ShowToast($"Người chơi {otherPlayer.NickName} đã thoát phòng.");
        ResetMyReadyState();
        UpdatePlayerListUI();

        if (statusText != null)
        {
            statusText.text = "Đối thủ đã thoát. Đang đợi người mới...";
        }
        
        // Nếu người thoát là Host, người ở lại thành Host -> Cần mở khóa Dropdown
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Tôi đã trở thành Host mới.");
            PhotonNetwork.CurrentRoom.IsOpen = true; 
            PhotonNetwork.CurrentRoom.IsVisible = true;
            dropdownModeGame.interactable = true; // Cho phép chỉnh mode
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
}