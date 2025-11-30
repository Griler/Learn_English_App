using UnityEngine;
using System.Collections.Generic;
using TMPro; // Dùng cho InputField

public class FriendMainView : MonoBehaviour
{
    [Header("Data Source")]
    public UserProfileSO userProfileSO;

    [Header("List References")]
    public Transform contentParent;
    public FriendItemUI friendItemPrefab; // Kéo Prefab bước 2 vào đây

    [Header("Add Friend References")]
    public TMP_InputField addFriendInput;
    public TextMeshProUGUI addStatusText;

    private List<GameObject> currentItems = new List<GameObject>();

    private void OnEnable()
    {
        if (userProfileSO != null)
        {
            userProfileSO.OnFriendListChanged += RenderFriendList;
            // Render ngay nếu đã có data
            if(userProfileSO.friendList != null) RenderFriendList(userProfileSO.friendList);
        }
        addStatusText.text = "";
    }

    private void OnDisable()
    {
        if (userProfileSO != null)
            userProfileSO.OnFriendListChanged -= RenderFriendList;
    }

    // Hàm này được gọi tự động khi data trong SO thay đổi (do FirebaseFetcher đẩy vào)
    void RenderFriendList(List<FriendData> list)
    {
        // 1. Xóa sạch list cũ
        foreach (var item in currentItems) Destroy(item);
        currentItems.Clear();

        // 2. Tạo list mới
        if (list == null) return;
        foreach (var friendData in list)
        {
            FriendItemUI newItem = Instantiate(friendItemPrefab, contentParent);
            // Gọi hàm Setup để item tự đi lấy data chi tiết
            newItem.SetupUI(friendData.userId);
            currentItems.Add(newItem.gameObject);
        }
    }

    // --- CHỨC NĂNG NÚT THÊM BẠN ---
    // Gắn hàm này vào nút "Thêm" ở UI chính
    public void OnAddFriendButtonClicked()
    {
        string idToAdd = addFriendInput.text.Trim();
        addStatusText.text = "Đang xử lý...";

        FriendActionService.Instance.AddFriend(idToAdd, (success, message) => {
            addStatusText.text = message;
            if(success) addFriendInput.text = "";
        });
    }
}