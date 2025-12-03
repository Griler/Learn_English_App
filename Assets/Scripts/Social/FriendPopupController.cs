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
        //SwitchTab(0);
    }

    public void SwitchTab(int index)
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
                panelListFriend.OnShow();
                btnTabList.GetComponent<Image>().color = Color.white;
                btnTabAdd.GetComponent<Image>().color = Color.gray2;
                btnTabRequests.GetComponent<Image>().color = Color.gray2;
                break;
            case 1:
                panelFriendAdd.gameObject.SetActive(true);
                title.text = "Add Friends";
                panelFriendAdd.OnShow();
                btnTabList.GetComponent<Image>().color = Color.gray2;
                btnTabAdd.GetComponent<Image>().color = Color.white;
                btnTabRequests.GetComponent<Image>().color= Color.gray2;

                break;
            case 2:
                panelFriendRequests.gameObject.SetActive(true);
                title.text = "Accept Friends";
                panelFriendRequests.OnShow();
                btnTabList.GetComponent<Image>().color = Color.gray2;
                btnTabAdd.GetComponent<Image>().color = Color.gray2;
                btnTabRequests.GetComponent<Image>().color = Color.white;
                break;
        }
    }
}