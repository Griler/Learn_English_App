﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.SceneManagement; // Cần thư viện này để tìm kiếm danh sách nhanh hơn

public class ListenLoader : MonoBehaviour
{
    [Header("Firebase Config")]
    public string nodeName = "listen_topics"; 

    // Biến lưu trữ dữ liệu sau khi tải về (Cache)
    private List<ListenCategory> cachedTopics = new List<ListenCategory>();
    
    [SerializeField]private GameObject item;

    [SerializeField] private Transform container;
    // Sự kiện để báo cho các script khác biết khi nào tải xong
    public System.Action OnDataLoaded; 

    void OnEnable()
    {
        LoadDataFromFirebase();
    }

    // --- PHẦN 1: TẢI DỮ LIỆU VỀ ---
    void LoadDataFromFirebase()
    {
        Debug.Log("Đang tải dữ liệu...");
        FirebaseDatabase.DefaultInstance.RootReference.Child(nodeName).GetValueAsync().ContinueWithOnMainThread(task =>
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
                        ListCategory listCategory = JsonConvert.DeserializeObject<ListCategory>(jsonContent);
                        if (listCategory != null && listCategory.topics != null)
                        {
                            // Lưu vào biến Cache để dùng cho 2 hàm dưới
                            cachedTopics = listCategory.topics;
                            Debug.Log($"Đã tải xong {cachedTopics.Count} chủ đề.");
                            // Báo hiệu đã tải xong (để tạo Menu)
                            LoadItem();
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
        ListenCategory foundTopic = cachedTopics.FirstOrDefault(t => t.topicName == nameToFind);

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

    void LoadItem()
    {
        foreach (Transform c in container) Destroy(c.gameObject);
        for (int i = 0; i < cachedTopics.Count; i++)
        {
            GameObject go = Instantiate(item, container);
            string nameTopic  = cachedTopics[i].topicName;
            go.GetComponent<SpeakingItem>().setName(nameTopic);
            go.GetComponent<SpeakingItem>().setOnClickButton(() => OnTopicClicked(nameTopic));
        }
    }

    public void OnTopicClicked(string nameTopic)
    {
        GlobalData.questionsToListen = GetQuestionsByTopicName(nameTopic);
        SceneManager.LoadScene("ListenScene");
    }

}