using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InformationUser : BaseCode
{
    public Image avatar;
    public Image avatarBorder;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI coinText;

    public UserProfileSO userProfileSO;
    
    private void OnEnable()
    {
        userProfileSO.OnUserInfoChanged += updateInformation;
    }

    private void Start()
    {
        // TRƯỜNG HỢP 1: Firebase đã xong rồi (Load scene lại hoặc vào game muộn)
        if (FirebaseDatabaseManager.Instance.IsReady)
        {
            FirebaseDatabaseManager.Instance.ListenToUserInfo();
        }
        // TRƯỜNG HỢP 2: Firebase chưa xong (Mới bật game)
        else
        {
            Debug.Log("⏳ Đang chờ Firebase init...");
            // Đăng ký: "Khi nào xong thì gọi hàm ListenToUserInfo của tao nhé"
            FirebaseDatabaseManager.Instance.OnFirebaseInitialized += OnFirebaseReady;
        }
    }

    // Hàm trung gian để gọi khi sự kiện xảy ra
    private void OnFirebaseReady()
    {
        // Huỷ đăng ký ngay để tránh gọi lại 2 lần (Memory Leak)
        FirebaseDatabaseManager.Instance.OnFirebaseInitialized -= OnFirebaseReady;
        
        // Giờ thì an toàn 100% để gọi
        FirebaseDatabaseManager.Instance.ListenToUserInfo();
    }
    
    // Đảm bảo huỷ đăng ký nếu object bị destroy giữa chừng
    private void OnDestroy()
    {
        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.OnFirebaseInitialized -= OnFirebaseReady;
        }
    }

    public void updateInformation(UserInfoData userInfo)
    {
        string username = userInfo.name;
        int coin = userInfo.coin;
        usernameText.text = username;
        coinText.text = coin.ToString();
        avatar.sprite = assetManager.getSpriteAvatar(userInfo.avatar);
        avatarBorder.sprite = assetManager.getSpriteBorder(userInfo.border);
    }

    private void OnDisable()
    {
        userProfileSO.OnUserInfoChanged -= updateInformation;
    }
}
