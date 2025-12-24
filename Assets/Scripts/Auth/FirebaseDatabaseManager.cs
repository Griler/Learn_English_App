using System;
using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Newtonsoft.Json;
using UnityEditor;

public partial class FirebaseDatabaseManager : MonoBehaviour
{
    public DatabaseReference dbReference;
    public FirebaseAuth fireAuthReference;
    public FirebaseUser currentUser;
    
    public static FirebaseDatabaseManager Instance;
    private bool _isFirstConnect = true;
    public bool IsReady { get; private set; } = false;
    public event Action OnFirebaseInitialized; // Sự kiện bắn ra khi xong
    private void Awake()
    {
        // Phải kiểm tra xem đã có thằng nào nắm giữ Instance chưa
        if (Instance != null && Instance != this)
        {
            Debug.LogError(gameObject.name);// Hủy cái mới ngay, KHÔNG ĐƯỢC ĐỤNG VÀO Instance cũ
            Destroy(gameObject); // Hủy cái mới ngay, KHÔNG ĐƯỢC ĐỤNG VÀO Instance cũ
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    
        _ = InitializeFirebase();
    }

    public async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            // Init các biến quan trọng
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
            fireAuthReference = FirebaseAuth.DefaultInstance;
            // 2. Đánh dấu đã xong
            IsReady = true;
            Debug.Log("✅ Firebase initialized successfully!");
            FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false); 

            // 3. Báo tin cho tất cả các script đang chờ
            InitializeConnectionCheck();
            OnFirebaseInitialized?.Invoke();
        }
        else
        {
            Debug.LogError("❌ Firebase Error: " + dependencyStatus);
        }
    }

    public async Task CompleteMissionById(string missionId)
    {
        if (currentUser == null)
        {
            Debug.LogWarning("⚠️ Không có user đăng nhập Firebase!");
            return;
        }

        string userId = currentUser.UserId;

        var userMissionRef = FirebaseDatabase.DefaultInstance
            .GetReference("user_missions")
            .Child(userId)
            .Child("missions")
            .Child(missionId);

        // Kiểm tra xem nhiệm vụ có tồn tại không
        var snapshot = await userMissionRef.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy missionId: {missionId}");
            return;
        }

        // Cập nhật trạng thái hoàn thành
        var updateData = new Dictionary<string, object>
        {
            { "isCompleted", true }
        };

        await userMissionRef.UpdateChildrenAsync(updateData);
        Debug.Log($"✅ Mission {missionId} set isCompleted = true thành công!");
    }
    public bool IsConnected { get; private set; } = false;
    private void InitializeConnectionCheck()
    {
        // 2. Lắng nghe trạng thái kết nối (.info/connected)
        // Đây là đường dẫn đặc biệt của Firebase, chỉ tồn tại ở Client
        DatabaseReference connectedRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");

        connectedRef.ValueChanged += (object sender, ValueChangedEventArgs args) =>
        {
            if (args.Snapshot.Value != null)
            {
                bool connected = (bool)args.Snapshot.Value;
                IsConnected = connected;

                if (connected)
                {
                    _isFirstConnect = false; // Đã kết nối thành công ít nhất 1 lần
                    Debug.Log(">>> ĐÃ KẾT NỐI INTERNET/FIREBASE");
                    if(currentUser == null) return;
                    HandleUserOnline();
                }
                else
                {
                    if (!_isFirstConnect) {
                        Debug.LogWarning(">>> ĐÃ MẤT KẾT NỐI (RỚT MẠNG)");
                        HandleUserOfflineUI(); 
                    } else {
                        Debug.Log("Đang khởi tạo kết nối lần đầu...");
                    }
                }
            }
        };
    }
    
    private void HandleUserOnline()
    {
        // Đường dẫn đến status của user này: status/{userId}
        DatabaseReference userStatusRef = dbReference.Child("users").Child(currentUser.UserId).Child("userInfo").Child("status");

        // A. Đăng ký "Di Chúc" (OnDisconnect)
        // Lệnh này gửi lên Server, dặn Server là: "Khi nào tao rớt mạng, hãy set trạng thái tao là offline"
        // ServerValue.Timestamp dùng để lưu thời gian rớt mạng chính xác theo giờ Server
        string offlineState = GlobalData.STATUS.OFFLINE;
        userStatusRef.OnDisconnect().SetValue(offlineState);
        string onlineState = GlobalData.STATUS.ONLINE;
        userStatusRef.SetValueAsync(onlineState);
        ToastNetwork.Instance.hideDisconnect();

    }

    // Hàm xử lý UI khi Client tự phát hiện rớt mạng
    private void HandleUserOfflineUI()
    {
        ToastNetwork.Instance.actionOnClickButton = () => { StartCoroutine(OnRetryConnectionClicked()); };
        ToastNetwork.Instance.showDisconnect("Mất kết nối máy chủ!");
    }
    
    IEnumerator OnRetryConnectionClicked()
    {
        Debug.Log("Dang thử kết nối lại...");
        yield return new WaitForSeconds(2f);
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            HandleUserOfflineUI();
        }
    }
}