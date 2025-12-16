using System;
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
    
    public bool IsReady { get; private set; } = false;
    public event Action OnFirebaseInitialized; // Sự kiện bắn ra khi xong
    private void Awake()
    {
        // Phải kiểm tra xem đã có thằng nào nắm giữ Instance chưa
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Hủy cái mới ngay, KHÔNG ĐƯỢC ĐỤNG VÀO Instance cũ
            return; 
        }

        Instance = this; // Chỉ gán khi chưa có ai, hoặc là chính mình
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

            // 3. Báo tin cho tất cả các script đang chờ
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

}