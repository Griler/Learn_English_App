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
    public void LoadMainTopics(Action<Dictionary<string, bool>> onComplete)
    {
        // 1. Kiểm tra User đã đăng nhập chưa để lấy UserID
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogError("User chưa đăng nhập!");
            return;
        }

        string userId = currentUser.UserId;

        // 2. Lấy danh sách Topics từ "vocab_topics"
        FirebaseDatabase.DefaultInstance
            .GetReference("vocab_topics")
            .GetValueAsync()
            .ContinueWithOnMainThread(taskTopics =>
            {
                if (taskTopics.IsFaulted || taskTopics.IsCanceled)
                {
                    ToastNetwork.Instance.actionOnClickButton = () => LoadMainTopics(onComplete);
                    ToastNetwork.Instance.showDisconnect();
                    Debug.LogError("Lỗi tải vocab_topics: " + taskTopics.Exception);
                    return;
                }

                DataSnapshot topicsSnapshot = taskTopics.Result;

                // 3. Sau khi có Topics, lấy tiếp dữ liệu tiến độ của User
                // Giả sử cấu trúc data user là: users/{userId}/topics/{topicKey}/isComplete
                FirebaseDatabase.DefaultInstance
                    .GetReference($"users/{userId}/learning_progress/vocab_topics")
                    .GetValueAsync()
                    .ContinueWithOnMainThread(taskUser =>
                    {
                        if (taskUser.IsFaulted || taskUser.IsCanceled)
                        {
                            Debug.LogError("Lỗi tải user data: " + taskUser.Exception);
                            ToastNetwork.Instance.actionOnClickButton = () => LoadMainTopics(onComplete);
                            ToastNetwork.Instance.showDisconnect();
                            return;
                        }

                        DataSnapshot userSnapshot = taskUser.Result;
                        Dictionary<string, bool> result = new Dictionary<string, bool>();

                        // 4. Duyệt qua từng Topic và map với dữ liệu User
                        foreach (var child in topicsSnapshot.Children)
                        {
                            string topicKey = child.Key;
                            bool isComplete = false;

                            // Kiểm tra xem User có dữ liệu về topic này không
                            if (userSnapshot.HasChild(topicKey))
                            {
                                var userTopicData = userSnapshot.Child(topicKey);

                                // Lấy giá trị isComplete (mặc định false nếu không tìm thấy)
                                if (userTopicData.HasChild("isCompleted"))
                                {
                                    // Parse giá trị sang bool an toàn
                                    bool.TryParse(userTopicData.Child("isCompleted").Value.ToString(), out isComplete);
                                }
                            }

                            // Thêm vào Dictionary kết quả
                            result.Add(topicKey, isComplete);
                        }

                        // Trả về kết quả
                        ToastNetwork.Instance.hideDisconnect();
                        onComplete?.Invoke(result);
                    });
            });
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
                    ToastNetwork.Instance.actionOnClickButton = () => LoadWords(mainTopic, category, onComplete);
                    ToastNetwork.Instance.showDisconnect();
                    onComplete?.Invoke(null);
                    return;
                }

                if (task.IsCompleted)
                {
                    ToastNetwork.Instance.hideDisconnect();
                    string data = task.Result.GetRawJsonValue();
                    List<WordData> vocabularies = JsonConvert.DeserializeObject<List<WordData>>(data);
                    onComplete?.Invoke(vocabularies);
                }
            });
    }

    public void LoadSubTopics(string mainTopic, Action<List<string>> onComplete)
    {
        string path = "vocab_topics/" + mainTopic;
        FirebaseDatabase.DefaultInstance
            .GetReference("vocab_topics")
            .Child(mainTopic)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Firebase load failed: " + task.Exception);
                    ToastNetwork.Instance.actionOnClickButton = () => LoadSubTopics(mainTopic, onComplete);
                    ToastNetwork.Instance.showDisconnect();
                    onComplete?.Invoke(null);
                    return;
                }
                
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    List<string> subTopics = new List<string>();
                    foreach (var child in snapshot.Children)
                    {
                        subTopics.Add(child.Key);
                    }

                    onComplete?.Invoke(subTopics);
                }
            });
    }

    public void FetchGrammarData(string grammarCategoryId, Action<GrammarData> cb)
    {
        Debug.Log("Đang tải dữ liệu từ Firebase...");

        FirebaseDatabase.DefaultInstance.GetReference("grammar")
            .Child("topics").Child(grammarCategoryId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    ToastNetwork.Instance.actionOnClickButton = () => FetchGrammarData(grammarCategoryId, cb);
                    ToastNetwork.Instance.showDisconnect();
                    Debug.LogError("Lỗi khi lấy dữ liệu: " + task.Exception);
                    return;
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists && snapshot.HasChildren)
                    {
                        // Cách 1: Parse thủ công (An toàn và dễ debug nhất)
                        ParseDataManually(snapshot, cb);

                        // Cách 2: Nếu dùng thư viện JSON ngoài (như Newtonsoft) có thể parse string JSON
                        // string jsonRaw = snapshot.GetRawJsonValue();
                        // GrammarData data = JsonConvert.DeserializeObject<GrammarData>(jsonRaw);
                    }
                    else
                    {
                        Debug.LogWarning("Không tìm thấy dữ liệu tại node ");
                    }
                }
            });
    }

    void ParseDataManually(DataSnapshot snapshot, Action<GrammarData> cb)
    {
        GrammarData data = new GrammarData();

        // Lấy các trường cơ bản
        if (snapshot.Child("description").Exists)
            data.description = snapshot.Child("description").Value.ToString();

        if (snapshot.Child("rule").Exists)
            data.rule = snapshot.Child("rule").Value.ToString();

        if (snapshot.Child("grammarPointID").Exists)
            data.grammarPointID = snapshot.Child("grammarPointID").Value.ToString();
        // --- LẤY EXAMPLES ---
        DataSnapshot examplesSnapshot = snapshot.Child("examples");
        foreach (DataSnapshot child in examplesSnapshot.Children)
        {
            GrammarFlashcardExmpale ex = new GrammarFlashcardExmpale();
            ex.conjugatedVerb = child.Child("conjugatedVerb").Value?.ToString();
            ex.sentence = child.Child("sentence").Value?.ToString();
            ex.translation = child.Child("translation").Value?.ToString();
            ex.grammarPointID = data.grammarPointID;
            ex.ruleText = data.rule;
            data.examples.Add(ex);
        }

        // --- LẤY MINI EXERCISES ---
        DataSnapshot exercisesSnapshot = snapshot.Child("miniExercises");
        foreach (DataSnapshot child in exercisesSnapshot.Children)
        {
            GrammarFlashcardExercise miniEx = new GrammarFlashcardExercise();
            miniEx.answer = child.Child("answer").Value?.ToString();
            miniEx.difficultyLevel = child.Child("difficultyLevel").Value?.ToString();
            miniEx.question = child.Child("question").Value?.ToString();
            miniEx.grammarPointID = data.grammarPointID;
            miniEx.ruleText = data.rule;
            data.miniExercises.Add(miniEx);
        }

        // --- IN RA KẾT QUẢ ĐỂ KIỂM TRA ---
        Debug.Log($"<color=green>Tải thành công!</color>");
        Debug.Log($"Rule: {data.rule}");
        Debug.Log($"Số lượng ví dụ: {data.examples.Count}");
        Debug.Log($"Số lượng bài tập: {data.miniExercises.Count}");

        // Test thử dữ liệu đầu tiên
        if (data.examples.Count > 0)
        {
            Debug.Log($"Ví dụ 1: {data.examples[0].sentence} -> {data.examples[0].translation}");
        }

        if (data.miniExercises.Count > 0)
        {
            Debug.Log($"Câu hỏi 1: {data.miniExercises[0].question} (Đáp án: {data.miniExercises[0].answer})");
        }

        cb?.Invoke(data);
    }
}