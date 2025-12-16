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

    
    public void LoadMainTopics(Action<List<string>> onComplete)
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("vocab_topics")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    List<string> mainTopics = new List<string>();
                    foreach (var child in snapshot.Children)
                    {
                        mainTopics.Add(child.Key);
                    }

                    onComplete?.Invoke(mainTopics);
                }
                else
                {
                    Debug.LogError("Error loading topics!");
                }
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
                    onComplete?.Invoke(null);
                    return;
                }

                if (task.IsCompleted)
                {
                    string data = task.Result.GetRawJsonValue();
                    List<WordData> vocabularies = JsonConvert.DeserializeObject<List<WordData>>(data);
                    onComplete?.Invoke(vocabularies);
                }
            });
    }
    
    public void LoadSubTopics(string mainTopic,Action<List<string>> onComplete )
    {
        string path = "vocab_topics/" +  mainTopic;
        FirebaseDatabase.DefaultInstance
            .GetReference("vocab_topics")
            .Child(mainTopic)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
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
            .Child("topics").Child(grammarCategoryId).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Lỗi khi lấy dữ liệu: " + task.Exception);
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
    }}