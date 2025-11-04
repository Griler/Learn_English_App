using System;
using System.Collections;
using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class TopicSelectManager : MonoBehaviour
{
    [SerializeField] Transform contentParent;
    [SerializeField] GameObject topicButtonPrefab;
    [SerializeField] GameObject viewSubTopic;
    
    IEnumerator Start()
    {
        while (!FirebaseDatabaseManager.IsReady)
        {
            Debug.Log("⏳ Waiting for Firebase init...");
            yield return null;
        }

        // Gọi load data khi Firebase đã sẵn sàng
        LoadMainTopics();
        
    }

    void LoadMainTopics()
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

                    Populate(mainTopics);
                }
                else
                {
                    Debug.LogError("Error loading topics!");
                }
            });
    }

    void Populate(List<string> topics)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (string topic in topics)
        {
            try
            {
                GameObject topicChild = Instantiate(topicButtonPrefab, contentParent);
                topicChild.GetComponentInChildren<TextMeshProUGUI>().text = topic;
                topicChild.GetComponentInChildren<Button>().onClick.AddListener(() => OnTopicSelected(topic));
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
        PlayerPrefs.SetString("SelectedMainTopic", topicId);
        viewSubTopic.SetActive(true);
        gameObject.SetActive(false);
    }
}