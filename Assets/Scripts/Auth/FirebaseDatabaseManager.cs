using System;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;

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
                    int score = int.Parse(snapshot.Child("score").Value.ToString());
                    Debug.Log($"User: {email}, Score: {score}");
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