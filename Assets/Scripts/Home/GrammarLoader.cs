using System;
using System.Collections;
using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GrammarLoader : MonoBehaviour
{
    public List<GrammarTopic> loadedTopics = new List<GrammarTopic>();
    [SerializeField] Transform contentParent;
    [SerializeField] GameObject topicPrefab;

    
    void OnEnable()
    { 
        foreach (Transform c in contentParent)
        {
            c.gameObject.SetActive(false);
            Destroy(c.gameObject);
        }
        Canvas.ForceUpdateCanvases();
        LoadingController.Instance.Show();
        LoadTopicsFromFirebase();
    }

   public void LoadTopicsFromFirebase()
    {
        // 1. Tải danh sách bài ngữ pháp
        FirebaseDatabase.DefaultInstance
            .GetReference("grammar")
            .Child("topics")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Lỗi tải topics: " + task.Exception);
                    ToastNetwork.Instance.actionOnClickButton = () => LoadTopicsFromFirebase();
                    ToastNetwork.Instance.showDisconnect();
                    return;
                }

                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    loadedTopics.Clear();
                    
                    foreach (DataSnapshot topicSnap in snapshot.Children)
                    {
                        string json = topicSnap.GetRawJsonValue();
                        GrammarTopic topic = JsonUtility.FromJson<GrammarTopic>(json);
                        loadedTopics.Add(topic);
                    }
                    
                    Debug.Log($"✅ Đã load {loadedTopics.Count} topic grammar.");
                    
                    // 2. Tải tiến độ người dùng
                    LoadUserProgress();
                }
            });
    }

    void LoadUserProgress()
    {
        string userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        
        // Đường dẫn: users/{uid}/learning_progress/grammar
        FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/learning_progress/grammar")
            .GetValueAsync().ContinueWithOnMainThread(task => 
            {
                DataSnapshot progressSnapshot = null;
                if (task.IsCanceled || task.IsFaulted)
                {
                    ToastNetwork.Instance.actionOnClickButton = () => LoadUserProgress();
                    ToastNetwork.Instance.showDisconnect();
                    return;
                }
                
                if (task.IsCompleted && !task.IsFaulted)
                {
                    progressSnapshot = task.Result;
                }
                
                // Truyền dữ liệu tiến độ vào hàm Populate
                Populate(loadedTopics, progressSnapshot);
            });
    }
    
    void Populate(List<GrammarTopic> topics, DataSnapshot userProgress)
    {
        LoadingController.Instance.Hide();
        topics = topics.OrderBy(x =>  x.grammarPointID).Reverse().ToList();
        foreach (GrammarTopic topic in topics)
        {
            try
            {
                GameObject topicChild = Instantiate(topicPrefab, contentParent);
                
                // Hiển thị tên bài học (lấy từ GlobalData mapNameGrammar)
                string displayKey = topic.grammarPointID.ToUpper();
                if (GlobalData.mapNameGrammar.ContainsKey(displayKey))
                {
                     topicChild.GetComponentInChildren<TextMeshProUGUI>().text = GlobalData.mapNameGrammar[displayKey].ToLower();
                }
                else
                {
                     topicChild.GetComponentInChildren<TextMeshProUGUI>().text = topic.grammarPointID.Replace("_", " ");
                }

                // --- CHECK TIẾN ĐỘ ---
                bool isCompleted = false;
                if ( userProgress != null && userProgress.HasChild(topic.grammarPointID))
                {
                    var userTopicData = userProgress.Child(topic.grammarPointID);

                    // Lấy giá trị isComplete (mặc định false nếu không tìm thấy)
                    if (userTopicData.HasChild("isCompleted"))
                    {
                        // Parse giá trị sang bool an toàn
                        bool.TryParse(userTopicData.Child("isCompleted").Value.ToString(), out isCompleted);
                    }
                }

                if (isCompleted)
                {
                    topicChild.GetComponentsInChildren<Image>()[0].color = Color.white;
                }
                else
                {
                    topicChild.GetComponentsInChildren<Image>()[0].color = Color.gray;

                }

                topicChild.GetComponentInChildren<Button>().onClick.AddListener(() => OnTopicSelected(topic.grammarPointID));
            }
            catch (Exception e)
            {
                Debug.LogError("Error populating grammar item: " + e.Message);
            }
        }
    }

    void OnTopicSelected(string topicId)
    {
        PlayerPrefs.SetString("SelectedGrammarTopic", topicId);
        SceneManager.LoadScene("SentenceBuildingScene");
    }

}