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
        // Khi vừa vào scene, cập nhật lại UI cho những người đang có trong phòng
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

        // --- XỬ LÝ PLAYER 1 (Thường là Host) ---
        if (players.Length > 0)
        {
            Player p1 = players[0];
            player1Container.SetActive(true);
            UpdateSinglePlayerUI(p1,player1Container,player1IconReady);
        }
        else
        {
            // Ẩn UI nếu không có người
            player1Container.SetActive(false);
            player1TextStatus.text = "Waiting...";
        }

        // --- XỬ LÝ PLAYER 2 ---
        if (players.Length > 1)
        {
            Player p2 = players[1];
            player2Container.SetActive(true);
            UpdateSinglePlayerUI(p2,player2Container, player2IconReady);
        }
        else
        {
            player2Container.SetActive(false);
            player2TextStatus.text = "Waiting...";
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

// Hàm tiện ích: Lấy giá trị bool (cho nút Ready)
    private bool GetBoolProperty(Player player, string key)
    {
        if (player.CustomProperties.TryGetValue(key, out object tempValue))
        {
            return (bool)tempValue;
        }
        return false;
    }
    // Hàm phụ trợ để lấy thông tin đẹp (Tên + Trạng thái Ready)
    string GetPlayerInfoString(Player p)
    {
        string name = p.NickName;
        object isReadyObj;
        bool isReady = false;
        
        if (p.CustomProperties.TryGetValue("IsReady", out isReadyObj))
        {
            isReady = (bool)isReadyObj;
        }

        string readyString = isReady ? "<color=green>[READY]</color>" : "<color=red>[NOT READY]</color>";
        return $"{name}\n{readyString}"; // Ví dụ: "Huy [READY]"
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
        readyButtonText.text = isReady ? "HUỶ SẴN SÀNG" : "SẴN SÀNG";
        readyButton.image.color = isReady ? Color.gray : Color.green;
    }

    // --- CÁC CALLBACK CỦA PHOTON ---

    // 1. Có người mới vào phòng
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerListUI();
    }

    // 2. Có người thoát phòng
    public override void OnPlayerLeftRoom(Player otherPlayer)
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
        PhotonNetwork.LoadLevel("PVP_MainScene");
    }
    
    void UpdateRoomInfo()
    {
        // QUAN TRỌNG: Phải kiểm tra xem có đang ở trong phòng không
        if (PhotonNetwork.CurrentRoom != null)
        {
            // 1. Lấy Tên Phòng
            string name = PhotonNetwork.CurrentRoom.Name;
            roomName.text = name;

            // 2. Lấy số lượng người chơi hiện tại / Tối đa
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            Debug.Log($"Đang ở phòng: {name} | Số người: {currentPlayers}");
        }
    }
}