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
    
    public void LoadWords(string mainTopic, string category, Action<List<WordData>> onComplete)
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
                    List<WordData> words = new List<WordData>();

                    foreach (DataSnapshot child in snapshot.Children)
                    {
                        try
                        {
                            string json = child.GetRawJsonValue();
                            WordData word = JsonUtility.FromJson<WordData>(json);
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
    
    public void SaveLearnedVocabTopic(string topic, string subtopic)
    {
        string userId = currentUser.UserId;
        if (userId == null) return;

        // Kiểm tra đầu vào, không để trống
        if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(subtopic))
        {
            Debug.LogError("Lỗi: Topic và Subtopic không được để trống!");
            return;
        }

        // Đường dẫn mới sẽ là: 
        // users/{userId}/has_learn/vocabulary/{topic}/{subtopic}
        string path = $"users/{userId}/has_learn/vocabulary/{topic}/{subtopic}";

        Debug.Log($"Đang gửi yêu cầu lưu: {path}");

        // Vẫn dùng SetValueAsync(true) để đánh dấu là đã học
        dbReference.Child(path).SetValueAsync(true).ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError($"Lỗi khi lưu vocabulary topic: {task.Exception.Message}");
            }
            else if (task.IsCompleted)
            {
                Debug.Log($"Lưu topic '{topic}/{subtopic}' thành công!");
            }
        });
    }
    
    public void SaveLearnedGrammar(string grammarId)
    {
        string userId = currentUser.UserId;
        if (userId == null) return; // Dừng nếu chưa đăng nhập

        string path = $"users/{userId}/has_learn/grammar/{grammarId}";
        Debug.Log($"Đang gửi yêu cầu lưu: {path}");

        // Không "await", thay vào đó dùng ".ContinueWithOnMainThread"
        dbReference.Child(path).SetValueAsync(true).ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                // Nếu có lỗi (ví dụ: mất mạng, không có quyền ghi)
                Debug.LogError($"Lỗi khi lưu grammar: {task.Exception.Message}");
            }
            else if (task.IsCompleted)
            {
                // Nếu thành công
                Debug.Log($"Lưu grammar '{grammarId}' thành công!");
            }
        });

        // Hàm sẽ chạy đến đây và kết thúc ngay,
        // không chờ Firebase trả lời.
    }
    
    
    public void FetchAllQuestionsByGrammar(string grammarKey, Action<List<GrammarQuestion>>onComplete)
    {

        List<GrammarQuestion> questionList = new List<GrammarQuestion>();
        dbReference.Child("grammar-review").Child(grammarKey).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Không thể lấy dữ liệu: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                
                // Lấy chuỗi JSON thô từ Firebase
                string jsonString = snapshot.GetRawJsonValue();

                if (string.IsNullOrEmpty(jsonString))
                {
                    Debug.LogWarning("Không có dữ liệu tại 'allQuestions'.");
                    return;
                }

                // Dùng Newtonsoft để parse danh sách
                try
                { 
                    questionList = JsonConvert.DeserializeObject<List<GrammarQuestion>>(jsonString);
                    Debug.Log("Lấy thành công " + questionList.Count + " câu hỏi.");
                    // In ra câu hỏi đầu tiên để kiểm tra
                    if(questionList.Count > 0)
                    {
                        Debug.Log("Câu 1: " + questionList[0].question);
                        onComplete.Invoke(questionList);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Lỗi parse JSON: " + e.Message);

                }
            }
            
        });
    }
    
    public void LoadLearnedVocabTopics(Action<Dictionary<string, List<string>>> onComplete)
    {
        string userId = currentUser.UserId;
        if (userId == null)
        {
            // Nếu chưa đăng nhập, trả về một Dictionary rỗng ngay lập tức
            onComplete?.Invoke(new Dictionary<string, List<string>>());
            return;
        }

        // Đường dẫn đến mục vocabulary
        string path = $"users/{userId}/has_learn/vocabulary";
        Debug.Log($"Đang tải dữ liệu từ: {path}");

        dbReference.Child(path).GetValueAsync().ContinueWithOnMainThread(task => {
            
            // Chuẩn bị kết quả
            var learnedTopics = new Dictionary<string, List<string>>();

            if (task.IsFaulted)
            {
                Debug.LogError($"Lỗi khi tải vocabulary: {task.Exception.Message}");
                // Vẫn gọi callback với danh sách rỗng
                onComplete?.Invoke(learnedTopics); 
                return;
            }
            
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                // Kiểm tra xem có dữ liệu không
                if (!snapshot.Exists || !snapshot.HasChildren)
                {
                    Debug.Log("Không tìm thấy dữ liệu vocabulary đã học.");
                    onComplete?.Invoke(learnedTopics);
                    return;
                }

                // --- Bóc tách dữ liệu ---
                // Vòng lặp 1: Lặp qua các Topic (ví dụ: "Animals", "Food")
                foreach (var topicSnapshot in snapshot.Children)
                {
                    string topicName = topicSnapshot.Key;
                    var subtopicsList = new List<string>();

                    // Vòng lặp 2: Lặp qua các Subtopic (ví dụ: "Farm Animals", "Fruits")
                    foreach (var subtopicSnapshot in topicSnapshot.Children)
                    {
                        // Kiểm tra xem giá trị có phải là 'true' không
                        if (subtopicSnapshot.Value != null && (bool)subtopicSnapshot.Value == true)
                        {
                            subtopicsList.Add(subtopicSnapshot.Key);
                        }
                    }

                    // Thêm vào kết quả
                    if (subtopicsList.Count > 0)
                    {
                        learnedTopics.Add(topicName, subtopicsList);
                    }
                }

                Debug.Log($"Tải thành công {learnedTopics.Count} topic đã học.");
            }

            // GỌI CALLBACK với kết quả (dù có hay không)
            onComplete?.Invoke(learnedTopics);
        });
    }

    // ----- VÍ DỤ CÁCH GỌI HÀM MỚI -----
    public void TestLoadFunction()
    {
        Debug.Log("Bắt đầu tải danh sách đã học...");

        // Gọi hàm và truyền vào một hàm (lambda) để xử lý kết quả
        LoadLearnedVocabTopics(topicsDictionary => {
            
            // Code bên trong này sẽ chạy KHI CÓ KẾT QUẢ
            
            if (topicsDictionary.Count == 0)
            {
                Debug.Log("Kết quả: Chưa học topic nào.");
                return;
            }

            Debug.Log("--- DANH SÁCH ĐÃ HỌC ---");
            foreach (var topicEntry in topicsDictionary)
            {
                string topic = topicEntry.Key;
                List<string> subtopics = topicEntry.Value;

                // In ra Topic
                Debug.Log($"Topic: {topic}");

                // In ra các Subtopic
                foreach (string sub in subtopics)
                {
                    Debug.Log($"  - Subtopic: {sub}");
                }
            }
            Debug.Log("---------------------------");

        });

        Debug.Log("... Yêu cầu tải đã được gửi đi.");
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