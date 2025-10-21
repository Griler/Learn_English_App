using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using Firebase.Auth;

public class FirebaseDatabaseManager : MonoBehaviour
{
    DatabaseReference dbReference;
    private FirebaseUser currentUser;
    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // 📤 Lưu dữ liệu người chơi
    public void SaveUserData()
    {
        currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        UserData user = new UserData(currentUser.Email, 02);
        string json = JsonUtility.ToJson(user);

        dbReference.Child("dsds").Child(currentUser.UserId).SetRawJsonValueAsync(json)
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
