using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FriendSearchItemUI : BaseCode
{
    public TextMeshProUGUI nameText;
    public Image avatarImage; // (Chưa xử lý load ảnh thật để code gọn)
    public Image borderImage; // (Chưa xử lý load ảnh thật để code gọn)
    public TextMeshProUGUI rankPoint;
    public Button removeButton;

    private string _userId;
    private FriendAddPanel _controller;

    // Thêm tham số UserInfo info vào hàm
    public void Setup(string friendId, UserInfoData info, FriendAddPanel controller)
    {
        _userId = friendId;
        _controller = controller;

        // BƯỚC 2: Hiển thị data ngay lập tức (vì info đã được load từ bên ngoài rồi)
        if (info != null)
        {
            nameText.text = info.name;
        
            // Cần check null hoặc ToString() cẩn thận
            if (rankPoint != null) rankPoint.text = info.rankPoint.ToString();
        
            // Giả sử assetManager là Singleton hoặc bạn cần tham chiếu tới nó
            if (assetManager != null)
            {
                avatarImage.sprite = assetManager.getSpriteAvatar(info.avatar);
                borderImage.sprite = assetManager.getSpriteBorder(info.border); 
            }
        }

        // Reset lại listener để tránh lỗi lặp click (nếu dùng pooling)
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(OnRemoveClicked);
    }

    void OnRemoveClicked()
    {
        // Gọi ngược lại controller để xóa khỏi list data
        _controller.RemoveUserFromList(_userId);
        Destroy(gameObject);
    }
}