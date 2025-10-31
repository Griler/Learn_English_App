using System;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEditor;

public class FirebaseDatabaseManager : MonoBehaviour
{
    DatabaseReference dbReference;
    private FirebaseUser currentUser;
    public static FirebaseDatabaseManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    
    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        LoadUserData(currentUser.UserId);
    }

    public void SaveUserData()
    {
        UserData user = new UserData(currentUser.Email, 02);
        string json = JsonUtility.ToJson(user);

        dbReference.Child("users").Child(currentUser.UserId).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("Dữ liệu đã lưu!");
                else
                    Debug.LogError("Lưu thất bại: " + task.Exception);
            });
    }

    // 📥 Đọc dữ liệu người chơi
    public void LoadUserData(string userId)
    {
        
        dbReference.Child("users").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Lỗi đọc dữ liệu: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    string email = snapshot.Child("email").Value.ToString();
                    string username = email.Split('@')[0];
                    int coin = 0;
                    var coinNode = snapshot.Child("coins");

                    if (coinNode.Exists && coinNode.Value != null)
                    {
                        int.TryParse(coinNode.Value.ToString(), out coin);
                    }         
                    string pathLoad = $" {GlobalData.pathData}/{GlobalData.pathData}";
                    PlayerPrefs.SetString("user", username);
                    PlayerPrefs.SetString("email", email);
                    PlayerPrefs.SetInt("coin", coin);
                    
                    Debug.Log($"User: {email}, coin: {coin}");
                }
                else
                {
                    Debug.Log("Không tìm thấy user.");
                }
            }
        });
    }
    public async Task<int> GetCoins()
    {
        var snapshot = await dbReference.Child("users").Child(currentUser.UserId).Child("coins").GetValueAsync();
        if (snapshot.Exists)
            return int.Parse(snapshot.Value.ToString());
        else
            return 0;
    }
    
    public async Task AddCoins(int amount)
    {
        int currentCoins = await GetCoins();
        int newTotal = currentCoins + amount;
        await dbReference.Child("users").Child(currentUser.UserId).Child("coins").SetValueAsync(newTotal);
        Debug.Log("Coins updated: " + newTotal);
    }

    public async Task<bool> CanCollectDaily()
    {
        var snapshot = await dbReference.Child("users").Child(currentUser.UserId).Child("lastDailyReward").GetValueAsync();
        if (snapshot.Exists)
        {
            DateTime lastClaim = DateTime.Parse(snapshot.Value.ToString());
            return (DateTime.UtcNow.Date > lastClaim.Date);
        }
        return true;
    }

    public async Task CollectDailyReward(int rewardAmount)
    {
        if (await CanCollectDaily())
        {
            await AddCoins(rewardAmount);
            await dbReference.Child("users").Child(currentUser.UserId).Child("lastDailyReward").SetValueAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));
            Debug.Log("Daily reward collected!");
        }
        else
        {
            Debug.Log("Already collected today.");
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

[System.Serializable]
public class UserData
{
    public string email;
    public int score;

    public UserData(string email, int score)
    {
        this.email = email;
        this.score = score;
    }
}