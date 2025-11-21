using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq; // Cần thư viện này để tìm kiếm danh sách nhanh hơn

public class FirebaseTopicLoader : MonoBehaviour
{
    [Header("Firebase Config")]
    public string nodeName = "listening_practice"; 

    // Biến lưu trữ dữ liệu sau khi tải về (Cache)
    private List<TopicData> cachedTopics = new List<TopicData>();
    private DatabaseReference dbReference;

    // Sự kiện để báo cho các script khác biết khi nào tải xong
    public System.Action OnDataLoaded; 

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadDataFromFirebase(); // Tự động tải khi vào game
            }
            else
            {
                Debug.LogError("Lỗi Firebase: " + task.Result);
            }
        });
    }

    // --- PHẦN 1: TẢI DỮ LIỆU VỀ ---
    void LoadDataFromFirebase()
    {
        Debug.Log("Đang tải dữ liệu...");
        dbReference.Child(nodeName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.Value != null)
                {
                    string jsonContent = snapshot.GetRawJsonValue();
                    try 
                    {
                        // Parse JSON thành Object
                        GameData data = JsonUtility.FromJson<GameData>(jsonContent);
                        if (data != null && data.topics != null)
                        {
                            // Lưu vào biến Cache để dùng cho 2 hàm dưới
                            cachedTopics = data.topics;
                            Debug.Log($"Đã tải xong {cachedTopics.Count} chủ đề.");
                            
                            // Báo hiệu đã tải xong (để tạo Menu)
                            OnDataLoaded?.Invoke();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Lỗi Parse JSON: " + e.Message);
                    }
                }
            }
        });
    }

    // ============================================================
    // HÀM 1: LẤY DANH SÁCH TÊN TOPIC (Để tạo Menu chọn bài)
    // ============================================================
    public List<string> GetAllTopicNames()
    {
        List<string> names = new List<string>();

        if (cachedTopics != null)
        {
            foreach (var topic in cachedTopics)
            {
                names.Add(topic.topicName);
            }
        }
        
        return names;
    }

    // ============================================================
    // HÀM 2: LẤY CÂU HỎI THEO TÊN TOPIC (Khi người chơi chọn bài)
    // ============================================================
    public List<ListeningQuestion> GetQuestionsByTopicName(string nameToFind)
    {
        // Dùng Linq để tìm Topic có tên trùng khớp
        // (tương đương với việc for loop tìm kiếm)
        TopicData foundTopic = cachedTopics.FirstOrDefault(t => t.topicName == nameToFind);

        if (foundTopic != null)
        {
            return foundTopic.questions;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy chủ đề tên: " + nameToFind);
            return new List<ListeningQuestion>(); // Trả về list rỗng để không lỗi game
        }
    }
}