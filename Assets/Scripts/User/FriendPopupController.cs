using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FriendPopupController : MonoBehaviour
{
    [Header("--- Tabs Buttons ---")]
    public Button btnTabList;
    public Button btnTabAdd;
    public Button btnTabRequests;
    

    [Header("--- Panels ---")]
    public FriendListPanel panelListFriend;
    [FormerlySerializedAs("panelAddFriend")] public FriendAddPanel panelFriendAdd;
    public TextMeshProUGUI title;
    [FormerlySerializedAs("panelRequests")] public FriendRequestPanel panelFriendRequests; // Panel xử lý lời mời kết bạn

    void Start()
    {
        // Gán sự kiện chuyển tab
        btnTabList.onClick.AddListener(() => SwitchTab(0));
        btnTabAdd.onClick.AddListener(() => SwitchTab(1));
        btnTabRequests.onClick.AddListener(() => SwitchTab(2));

        // Mặc định mở tab List Friend
        SwitchTab(0);
    }

    void SwitchTab(int index)
    {
        // 1. Tắt tất cả panel trước
        panelListFriend.gameObject.SetActive(false);
        panelFriendAdd.gameObject.SetActive(false);
        panelFriendRequests.gameObject.SetActive(false);

        // 2. Bật panel được chọn và kích hoạt logic load data của nó
        switch (index)
        {
            case 0:
                panelListFriend.gameObject.SetActive(true);
                title.text = "Friends List";
                panelListFriend.OnShow(); // Hàm load lại list friend
                break;
            case 1:
                panelFriendAdd.gameObject.SetActive(true);
                title.text = "Add Friends";
                panelFriendAdd.OnShow(); // Hàm reset giao diện tìm kiếm
                break;
            case 2:
                panelFriendRequests.gameObject.SetActive(true);
                title.text = "Accept Friends";
                panelFriendRequests.OnShow(); // Hàm load danh sách lời mời
                break;
        }
    }
}