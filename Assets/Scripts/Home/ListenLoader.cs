﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Cần thư viện này để tìm kiếm danh sách nhanh hơn

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
    public string progressKey = "listen";
    void OnEnable()
    {
        LoadDataFromFirebase();
    }

    // --- PHẦN 1: TẢI DỮ LIỆU VỀ ---
    void LoadDataFromFirebase()
    {
        Debug.Log("Đang tải dữ liệu bài nghe...");
        
        // 1. Tải nội dung bài học trước
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
                            cachedTopics = listCategory.topics;
                            Debug.Log($"Đã tải xong {cachedTopics.Count} chủ đề nghe.");

                            // 2. SAU KHI CÓ DATA -> TẢI TIẾP TIẾN ĐỘ CỦA USER
                            LoadUserProgress();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Lỗi Parse JSON: " + e.Message);
                    }
                }
            }
            else
            {
                Debug.LogError("Không tải được dữ liệu Listen Topics");
            }
        });
    }

    // Hàm tải tiến độ riêng để code gọn gàng
    void LoadUserProgress()
    {
        string userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        
        // Đường dẫn: users/{uid}/learning_progress/listen
        DatabaseReference progressRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/learning_progress/{progressKey}");

        progressRef.GetValueAsync().ContinueWithOnMainThread(task => 
        {
            DataSnapshot progressSnapshot = null;
            if (task.IsCompleted && !task.IsFaulted)
            {
                progressSnapshot = task.Result;
            }

            // Gọi hàm tạo UI và truyền dữ liệu tiến độ vào
            LoadItem(progressSnapshot);
        });
    }

    // ============================================================
    // HÀM TẠO UI (Đã sửa để check tiến độ)
    // ============================================================
    void LoadItem(DataSnapshot userProgress)
    {
        foreach (Transform c in container) Destroy(c.gameObject);
        
        for (int i = 0; i < cachedTopics.Count; i++)
        {
            GameObject go = Instantiate(item, container);
            string nameTopic = cachedTopics[i].topicName;
            
            // Set tên bài học
            // Lưu ý: Script 'SpeakingItem' của bạn cần có hàm setName public
            SpeakingItem speakingItemScript = go.GetComponent<SpeakingItem>();
            speakingItemScript.setName(nameTopic);
            speakingItemScript.setOnClickButton(() => OnTopicClicked(nameTopic));

            // --- KIỂM TRA ĐÃ HỌC CHƯA ---
            bool isCompleted = false;
            if ( userProgress != null && userProgress.HasChild(nameTopic))
            {
                var userTopicData = userProgress.Child(nameTopic);

                // Lấy giá trị isComplete (mặc định false nếu không tìm thấy)
                if (userTopicData.HasChild("isCompleted"))
                {
                    // Parse giá trị sang bool an toàn
                    bool.TryParse(userTopicData.Child("isCompleted").Value.ToString(), out isCompleted);
                }
            }

            // --- HIỂN THỊ TRẠNG THÁI ---
            if (isCompleted)
            {
                // Cách 1: Đổi màu nút thành xanh lá
                go.GetComponentsInChildren<Image>()[0].color = Color.white;
            }
            else
            {
                go.GetComponentsInChildren<Image>()[0].color = Color.gray;
            }
        }
    }

    // ============================================================
    // CÁC HÀM HỖ TRỢ LOGIC
    // ============================================================
    public List<ListeningQuestion> GetQuestionsByTopicName(string nameToFind)
    {
        ListenCategory foundTopic = cachedTopics.FirstOrDefault(t => t.topicName == nameToFind);

        if (foundTopic != null)
        {
            return foundTopic.questions;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy chủ đề tên: " + nameToFind);
            return new List<ListeningQuestion>(); 
        }
    }

    public void OnTopicClicked(string nameTopic)
    {
        GlobalData.questionsToListen = GetQuestionsByTopicName(nameTopic);
        PlayerPrefs.SetString("SelectedListenTopic", nameTopic);
        SceneManager.LoadScene("ListenScene");
    }

}