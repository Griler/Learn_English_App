using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpeakingLoader : MonoBehaviour
{
    [SerializeField]private EnglishData data;
    [SerializeField]private List<string> key;
    [SerializeField]private List<Sprite> sprite;
    [SerializeField]private GameObject item;
    [SerializeField]private Transform container;
    Dictionary<string, int> topicIndexDict = new Dictionary<string, int>();

    void OnEnable()
    {
        LoadTopicsFromFirebase();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   public void LoadTopicsFromFirebase()
    {
        // 1. Tải danh sách chủ đề Speaking
        FirebaseDatabase.DefaultInstance
            .GetReference("speaking")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    ToastNetwork.Instance.actionOnClickButton = () => LoadTopicsFromFirebase();
                    ToastNetwork.Instance.showDisconnect();
                    return;
                }
                
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    key.Clear(); // Xóa list cũ trước khi add mới
                    
                    foreach (var child in snapshot.Children)
                    {
                       key.Add(child.Key); // Ví dụ: "greetings", "travel"
                    }

                    string json = snapshot.GetRawJsonValue();
                    try {
                        data = JsonUtility.FromJson<EnglishData>(json);
                        
                        // 2. Sau khi có danh sách, tải tiếp tiến độ User
                        LoadUserProgress();
                    }
                    catch (System.Exception e) {
                         Debug.LogError("JSON Error: " + e.Message);
                    }
                }
            });
    }

    void LoadUserProgress()
    {
        string userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
        
        // Đường dẫn: users/{uid}/learning_progress/speaking
        FirebaseDatabaseManager.Instance.dbReference.Database
            .GetReference($"users/{userId}/learning_progress/speaking")
            .GetValueAsync().ContinueWithOnMainThread(task => 
            {
                DataSnapshot progressSnapshot = null;
                if (task.IsCanceled || task.IsFaulted)
                {
                    ToastNetwork.Instance.actionOnClickButton = () => LoadTopicsFromFirebase();
                    ToastNetwork.Instance.showDisconnect();
                    return;
                }
                
                if (task.IsCompleted && !task.IsFaulted)
                {
                    progressSnapshot = task.Result;
                    ToastNetwork.Instance.hideDisconnect();
                }
                
                // Truyền snapshot tiến độ vào hàm tạo UI
                LoadItem(progressSnapshot);
            });
    }

    void LoadItem(DataSnapshot userProgress)
    {
        foreach (Transform c in container) Destroy(c.gameObject);
        
        for (int i = 0; i < key.Count; i++)
        {
            GameObject go = Instantiate(item, container);
            
            // Set hình ảnh (nếu có đủ sprite)
            if (i < sprite.Count) go.GetComponent<SpeakingItem>().setImage(sprite[i]);
            
            // Xử lý tên hiển thị (bỏ dấu gạch dưới nếu có)
            string currentKey = key[i];
            string displayName = currentKey.Split('_')[0]; 
            
            go.GetComponent<SpeakingItem>().setName(displayName);
            go.GetComponent<SpeakingItem>().setOnClickButton(() => OnTopicClicked(currentKey));

            // --- CHECK TIẾN ĐỘ ---
            bool isCompleted = false;
            if ( userProgress != null && userProgress.HasChild(currentKey))
            {
                var userTopicData = userProgress.Child(currentKey);

                // Lấy giá trị isComplete (mặc định false nếu không tìm thấy)
                if (userTopicData.HasChild("isCompleted"))
                {
                    // Parse giá trị sang bool an toàn
                    bool.TryParse(userTopicData.Child("isCompleted").Value.ToString(), out isCompleted);
                }
            }

            if (isCompleted)
            {
                go.GetComponentsInChildren<Image>()[0].color = Color.white;
            }
            else
            {
                go.GetComponentsInChildren<Image>()[0].color = Color.gray;

            }
        }
    }

    public void OnTopicClicked(string topicKey)
    {
        PlayerPrefs.SetString("CurrentSpeakingTopic", topicKey);
        SceneManager.LoadScene("SpeakingScene");
    }
}
[System.Serializable]
public class SentenceItem
{
    public string vn;
    public string en;
}

[System.Serializable]
public class EnglishData
{
    public List<SentenceItem> greetings;
    public List<SentenceItem> introductions;
    public List<SentenceItem> daily_conversation;
    public List<SentenceItem> shopping;
    public List<SentenceItem> travel;
    public List<SentenceItem> restaurant;
    public List<SentenceItem> feelings;
}