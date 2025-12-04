using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // Nếu dùng TextMeshPro

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
    public TextMeshProUGUI readyButtonText;
    public TextMeshProUGUI roomName;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        if (readyButton)
        {
            readyButton.onClick.AddListener(OnClick_ToggleReady);
        }
        UpdatePlayerListUI();
        
        // Reset trạng thái nút bấm
        bool isReady = (bool)PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("IsReady") && 
                       (bool)PhotonNetwork.LocalPlayer.CustomProperties["IsReady"];
        UpdateReadyButtonUI(isReady);
        UpdateRoomInfo();
    }

    // --- CẬP NHẬT UI ---
    void UpdatePlayerListUI()
    {
        Player[] players = PhotonNetwork.PlayerList;

        // 1. Tìm ra ai là Local (Mình) và ai là Remote (Địch)
        Player myPlayer = null;
        Player otherPlayer = null;

        foreach (Player p in players)
        {
            if (p.IsLocal)
            {
                myPlayer = p;
            }
            else
            {
                otherPlayer = p; // Trong chế độ 1v1, bất kỳ ai không phải mình thì là địch
            }
        }

        // --- XỬ LÝ PLAYER 1 (BÊN TRÁI - LUÔN LÀ CỦA MÌNH) ---
        // Vì mình luôn ở trong phòng, myPlayer sẽ không bao giờ null, nhưng cứ check cho an toàn
        if (myPlayer != null)
        {
            player1Container.SetActive(true);
            UpdateSinglePlayerUI(myPlayer, player1Container, player1IconReady);
        
            if(player1TextStatus != null) 
                player1TextStatus.gameObject.SetActive(false);
        }
        else
        {
            // Trường hợp cực hiếm (bug) không tìm thấy chính mình
            player1Container.SetActive(false);
        }

        // --- XỬ LÝ PLAYER 2 (BÊN PHẢI - LUÔN LÀ ĐỐI THỦ) ---
        if (otherPlayer != null)
        {
            // Có người khác trong phòng -> Hiển thị thông tin họ
            player2Container.SetActive(true);
            UpdateSinglePlayerUI(otherPlayer, player2Container, player2IconReady);
        
            if(player2TextStatus != null) 
                player2TextStatus.gameObject.SetActive(false);
        }
        else
        {
            // Chưa có ai vào -> Hiển thị "Waiting..."
            player2Container.SetActive(false); // Ẩn info container (avatar, tên)
        
            if(player2TextStatus != null)
            {
                player2TextStatus.gameObject.SetActive(true);
                player2TextStatus.text = "Waiting for opponent...";
            }
        }
    }

// Hàm cập nhật cho 1 slot UI cụ thể
    void UpdateSinglePlayerUI(Player player,GameObject playerContainer, GameObject readyObj)
    {
        // 1. Hiển thị UI lên
      
        string nameTxt = player.NickName;
        string avatarId = GetSafeString(player, "AvatarID"); 
        string borderId = GetSafeString(player, "BorderID");
        string rankPoint = GetSafeString(player, "Rank");
        playerContainer.GetComponent<FriendItemUI>().SetupUI(nameTxt,avatarId,borderId,rankPoint);
        // 6. Check trạng thái Ready
        bool isReady = GetBoolProperty(player, "IsReady");
        readyObj.SetActive(isReady); // Hiện icon check xanh nếu ready
    }


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

// Hàm tiện ích: Lấy giá trị bool (cho nút Ready)
    private bool GetBoolProperty(Player player, string key)
    {
        if (player.CustomProperties.TryGetValue(key, out object tempValue))
        {
            return (bool)tempValue;
        }
        return false;
    }

    // --- EVENT NÚT READY ---
    public void OnClick_ToggleReady()
    {
        // Lấy trạng thái hiện tại
        object isReadyObj;
        bool currentReady = false;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsReady", out isReadyObj))
        {
            currentReady = (bool)isReadyObj;
        }

        // Đảo ngược trạng thái (True -> False, False -> True)
        bool newReadyState = !currentReady;

        // Cập nhật lên server
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["IsReady"] = newReadyState;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateReadyButtonUI(newReadyState);
    }

    void UpdateReadyButtonUI(bool isReady)
    {
        readyButtonText.text = isReady ? "CANCEL" : "READY";
        readyButton.image.color = isReady ? Color.gray : Color.green;
    }

    // --- CÁC CALLBACK CỦA PHOTON ---

    // 1. Có người mới vào phòng
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerListUI();
    }

    // 3. Quan trọng nhất: Khi ai đó thay đổi Property (Bấm Ready)
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Cập nhật lại giao diện chữ [READY] bên cạnh tên
        UpdatePlayerListUI();

        // Kiểm tra xem tất cả đã Ready chưa để Start Game
        CheckAllPlayersReady();
    }

    void CheckAllPlayersReady()
    {
        // Chỉ Host mới có quyền gọi lệnh chuyển Scene
        if (!PhotonNetwork.IsMasterClient) return;

        // Cần đủ 2 người mới check (hoặc 1 nếu test)
        if (PhotonNetwork.PlayerList.Length < 2) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isReadyObj;
            if (p.CustomProperties.TryGetValue("IsReady", out isReadyObj))
            {
                if (!(bool)isReadyObj) return; // Có 1 ông chưa Ready -> Dừng
            }
            else
            {
                return; // Chưa có property IsReady -> Dừng
            }
        }

        // Nếu chạy hết vòng lặp mà không return -> Tất cả đã Ready
        StartGame();
    }

    void StartGame()
    {
        statusText.text = "Tất cả đã sẵn sàng! Đang vào game...";
        // Khoá phòng lại để không ai vào nữa
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // Load Scene Game Chính (Tên scene phải chuẩn trong Build Settings)
        PhotonNetwork.LoadLevel("PVPScene");
    }
    
    void UpdateRoomInfo()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            string name = PhotonNetwork.CurrentRoom.Name;
            roomName.text = name;

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
      
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Tôi đã trở thành Host mới. Mở lại phòng...");
            PhotonNetwork.CurrentRoom.IsOpen = true; 
            PhotonNetwork.CurrentRoom.IsVisible = true;
        }
    }

    // Hàm phụ trợ để bỏ Ready
    void ResetMyReadyState()
    {
        // Set property trên Server về false
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["IsReady"] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateReadyButtonUI(false);
    }
}