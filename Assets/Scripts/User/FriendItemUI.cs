using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FriendItemUI : BaseCode
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public Image avatarImage; // (Chưa xử lý load ảnh thật để code gọn)
    public Image borderImage; // (Chưa xử lý load ảnh thật để code gọn)
    public TextMeshProUGUI rankPoint;
    public Button invitePvpBtn;
    public Button deleteBtn;

    private string currentFriendId;
    private UserInfoData cachedInfo;

    // Hàm này được gọi khi Instantiate prefab
    public void SetupUI(UserInfoData info)
    {
        // Đăng ký sự kiện cho các nút
        invitePvpBtn.onClick.AddListener(OnInviteClicked);
        deleteBtn.onClick.AddListener(OnDeleteClicked);
        cachedInfo = info;
        nameText.text = info.name;
        rankPoint.text = info.rankPoint.ToString();
        avatarImage.sprite = assetManager.getSpriteAvatar(info.avatar);
        borderImage.sprite = assetManager.getSpriteBorder(info.border); 
    }

    void OnViewInfoClicked()
    {
        if(cachedInfo == null) return;
        // Gọi popup hiển thị (Sẽ làm ở bước 4)
        //OtherUserProfilePopup.Instance.ShowPopup(cachedInfo);
    }

    void OnInviteClicked()
    {
        FriendActionService.Instance.InvitePvP(currentFriendId);
    }

    void OnDeleteClicked()
    {
        FriendActionService.Instance.RemoveFriend(currentFriendId);
    }
}