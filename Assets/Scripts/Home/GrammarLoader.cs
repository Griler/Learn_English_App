using System;
using System.Collections;
using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
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

    
    IEnumerator Start()
    {
        while (!FirebaseDatabaseManager.IsReady)
        {
            Debug.Log("⏳ Waiting for Firebase init...");
            yield return null;
        }

        // Gọi load data khi Firebase đã sẵn sàng
        LoadTopicsFromFirebase();
        
    }

    public void LoadTopicsFromFirebase()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("grammar")
            .Child("topics")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Lỗi tải topics từ Firebase: " + task.Exception);
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

                    Populate(loadedTopics);
                    Debug.Log($"✅ Đã load {loadedTopics.Count} topic grammar từ Firebase.");
                }
            });
    }
    
    void Populate(List<GrammarTopic> topics)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (GrammarTopic topic in topics)
        {
            try
            {
                GameObject topicChild = Instantiate(topicPrefab, contentParent);
                string titleTopic = topic.grammarPointID.Replace("_","\n");
                topicChild.GetComponentInChildren<TextMeshProUGUI>().text = titleTopic;
                topicChild.GetComponentInChildren<Button>().onClick.AddListener(() => OnTopicSelected(topic.grammarPointID));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }
    }

    void OnTopicSelected(string topicId)
    {
        PlayerPrefs.SetString("SelectedGrammarTopic", topicId);
        SceneManager.LoadScene("FlashCardScene");
    }

}