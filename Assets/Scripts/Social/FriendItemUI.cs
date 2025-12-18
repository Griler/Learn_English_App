using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FriendItemUI : BaseCode
{
    [Header("UI Elements")] public TextMeshProUGUI nameText;
    public Image avatarImage; // (Chưa xử lý load ảnh thật để code gọn)
    public Image borderImage; // (Chưa xử lý load ảnh thật để code gọn)
    public TextMeshProUGUI rankPoint;
    public Button invitePvpBtn;
    public Button deleteBtn;

    private string currentFriendId;
    private string currentFriendName;
    private UserInfoData cachedInfo;
    private Action onDelCallback;
    
    // Hàm này được gọi khi Instantiate prefab
    public void SetupUI(UserInfoData info, string friendId, Action onDeleteCB)
    {
        // Đăng ký sự kiện cho các nút
        currentFriendId = friendId;
        currentFriendName = info.name;
        onDelCallback = onDeleteCB;
        if (invitePvpBtn)
        {
            invitePvpBtn.onClick.AddListener(OnInviteClicked);
        }

        if (deleteBtn)
        {
            deleteBtn.onClick.AddListener(OnDeleteClicked);
        }

        cachedInfo = info;
        nameText.text = info.name;
        rankPoint.text = info.rankPoint.ToString();
        avatarImage.sprite = assetManager.getSpriteAvatar(info.avatar);
        borderImage.sprite = assetManager.getSpriteBorder(info.border);
    }
    
    public void SetupUI(string name, string avatar, string border, string rank)
    {
        // settup watting room
        if (invitePvpBtn)
        {
            invitePvpBtn.onClick.AddListener(OnInviteClicked);
        }

        if (deleteBtn)
        {
            deleteBtn.onClick.AddListener(OnDeleteClicked);
        }
        nameText.text = "Tên: "+ name;
        rankPoint.text = "Điểm Xếp Hạng: " + rank;
        avatarImage.sprite = assetManager.getSpriteAvatar(avatar);
        borderImage.sprite = assetManager.getSpriteBorder(border);
    }

    void OnViewInfoClicked()
    {
        if (cachedInfo == null) return;
        // Gọi popup hiển thị (Sẽ làm ở bước 4)
        //OtherUserProfilePopup.Instance.ShowPopup(cachedInfo);
    }

    void OnInviteClicked()
    {
        string userName = FirebaseDatabaseManager.Instance.userProfileSO.userInfo.name;
        FriendSystem.Instance.SendInvite(currentFriendId, userName);
    }

    void OnDeleteClicked()
    {
        FriendActionService.Instance.RemoveFriend(currentFriendId,onDelCallback);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (invitePvpBtn)
        {
            invitePvpBtn.onClick.RemoveAllListeners();
        }

        if (deleteBtn)
        {
            deleteBtn.onClick.RemoveAllListeners();
        }
    }
}