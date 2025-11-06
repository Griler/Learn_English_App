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


    private DatabaseReference dbRef;
    public static bool IsReady { get; private set; } = false;

    private async void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        await InitializeFirebase();
    }

    private async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            IsReady = true;
            Debug.Log("✅ Firebase initialized successfully!");
        }
        else
        {
            Debug.LogError("❌ Firebase dependencies could not be resolved: " + dependencyStatus);
        }
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
    
    public void LoadWords(string mainTopic, string category, Action<List<AnimalData>> onComplete)
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("vocab_topics")
            .Child(mainTopic)
            .Child(category)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Firebase load failed: " + task.Exception);
                    onComplete?.Invoke(null);
                    return;
                }

                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    List<AnimalData> words = new List<AnimalData>();

                    foreach (DataSnapshot child in snapshot.Children)
                    {
                        try
                        {
                            string json = child.GetRawJsonValue();
                            AnimalData word = JsonUtility.FromJson<AnimalData>(json);
                            words.Add(word);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("⚠️ Parse error: " + e.Message);
                        }
                    }

                    onComplete?.Invoke(words);
                }
            });
    }
    
     public async Task AddLearnedVocabulary(string userId, string topic, string subtopic, List<string> words)
    {
        
        var vocabPath = dbRef.Child("users").Child(currentUser.UserId)
                             .Child("vocab_learned").Child(topic).Child(subtopic);

        // Lấy danh sách hiện tại nếu có
        var snapshot = await vocabPath.GetValueAsync();
        HashSet<string> currentWords = new HashSet<string>();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
                currentWords.Add(child.Value.ToString());
        }

        // Thêm các từ mới (tránh trùng)
        foreach (var word in words)
            currentWords.Add(word);

        // Lưu lại danh sách hoàn chỉnh
        await vocabPath.SetValueAsync(new List<string>(currentWords));
    }

    /// <summary>
    /// Thêm ngữ pháp đã học
    /// </summary>
    public async Task AddLearnedGrammar(string userId, string grammarKey)
    {
        var grammarPath = dbRef.Child("user_progress").Child(userId).Child("grammar_learned");

        var snapshot = await grammarPath.GetValueAsync();
        HashSet<string> learned = new HashSet<string>();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
                learned.Add(child.Value.ToString());
        }

        learned.Add(grammarKey);

        await grammarPath.SetValueAsync(new List<string>(learned));
    }

    /// <summary>
    /// Tải toàn bộ từ vựng đã học (theo topic/subtopic)
    /// </summary>
    public async Task<Dictionary<string, Dictionary<string, List<string>>>> LoadAllLearnedVocabulary()
    {
        
        var result = new Dictionary<string, Dictionary<string, List<string>>>();
        var snapshot = await dbRef.Child("users").Child(currentUser.UserId).Child("vocab_learned").GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var topicSnap in snapshot.Children)
            {
                var topicDict = new Dictionary<string, List<string>>();

                foreach (var subSnap in topicSnap.Children)
                {
                    List<string> words = new List<string>();
                    foreach (var w in subSnap.Children)
                        words.Add(w.Value.ToString());

                    topicDict[subSnap.Key] = words;
                }

                result[topicSnap.Key] = topicDict;
            }
        }
        return result;
    }
    
    public async Task<Dictionary<string, Dictionary<string, List<string>>>> LoadAllLearnedGrammar()
    {
        var result = new Dictionary<string, Dictionary<string, List<string>>>();
        var snapshot = await dbRef.Child("users").Child(currentUser.UserId).Child("grammar_learned").GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var topicSnap in snapshot.Children)
            {
                var topicDict = new Dictionary<string, List<string>>();

                foreach (var subSnap in topicSnap.Children)
                {
                    List<string> grammars = new List<string>();
                    foreach (var w in subSnap.Children)
                        grammars.Add(w.Value.ToString());

                    topicDict[subSnap.Key] = grammars;
                }

                result[topicSnap.Key] = topicDict;
            }
        }
        return result;
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