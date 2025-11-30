using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FriendItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public Image avatarImage; // (Chưa xử lý load ảnh thật để code gọn)
    public TextMeshProUGUI rankPoint;
    public Button invitePvpBtn;
    public Button deleteBtn;

    private string currentFriendId;
    private UserInfoData cachedInfo;

    // Hàm này được gọi khi Instantiate prefab
    public void SetupUI(string friendId)
    {
        this.currentFriendId = friendId;
        nameText.text = "Loading ID: " + friendId + "..."; // Hiện tạm ID trong lúc chờ load tên

        // Đăng ký sự kiện cho các nút
        invitePvpBtn.onClick.AddListener(OnInviteClicked);
        deleteBtn.onClick.AddListener(OnDeleteClicked);

        // GỌI SERVICE ĐỂ LẤY THÔNG TIN CHI TIẾT CỦA BẠN NÀY
        FriendActionService.Instance.FetchOtherUserInfo(friendId, 
            (info) => {
                // Thành công
                cachedInfo = info;
                nameText.text = info.name + $" (Lv.{info.coin})";
                rankPoint.text = info.rankPoint.ToString(); // Ví dụ hiển thị
                // Ở đây bạn thêm code load avatarImage dựa trên info.avatar
            },
            (error) => {
                // Thất bại
                nameText.text = "Lỗi: " + friendId;
            }
        );
    }

    void OnViewInfoClicked()
    {
        if(cachedInfo == null) return;
        // Gọi popup hiển thị (Sẽ làm ở bước 4)
        OtherUserProfilePopup.Instance.ShowPopup(cachedInfo);
    }

    void OnInviteClicked()
    {
        FriendActionService.Instance.InvitePvP(currentFriendId);
    }

    void OnDeleteClicked()
    {
        // Gọi service để xóa
        FriendActionService.Instance.RemoveFriend(currentFriendId);
        // Lưu ý: Không cần tự Destroy(gameObject) ở đây.
        // Hãy để FirebaseFetcher báo về UserProfileSO, rồi UI Main sẽ tự vẽ lại danh sách.
    }
}