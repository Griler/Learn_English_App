using System;
using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class SubTopicSelectManager : MonoBehaviour
{
    [SerializeField] Transform contentParent;
    [SerializeField] GameObject subTopicButtonPrefab;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text statusText;

    void OnEnable()
    {
        if (!PlayerPrefs.HasKey("SelectedMainCategoryId"))
        {
            SceneManager.LoadScene("HomeScene");
            return;
        }
        
        string parentCategoryId = PlayerPrefs.GetString("SelectedMainCategoryId");
        LoadSubTopics(parentCategoryId);
    }

    void Populate(List<string> subTopics, DataSnapshot progressData)
    {
        // Lưu lại vào static để dùng cho bước check hoàn thành (như đã nói ở Bước 2)
        GameSessionData.CurrentSubTopics = subTopics;

        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (string sub in subTopics)
        {
            GameObject lessonItem = Instantiate(subTopicButtonPrefab, contentParent);
            lessonItem.GetComponent<LessonItem>().setData(topicName:sub);
            string name = GlobalData.mapNameVocabulary[sub];
            lessonItem.GetComponentInChildren<TMP_Text>().text = name;
            if (progressData != null && progressData.HasChild(sub))
            {
                lessonItem.GetComponent<LessonItem>().setHightLightStart();
            }
            else
            {
                lessonItem.GetComponent<LessonItem>().setDisableStart();

            }
            lessonItem.GetComponentInChildren<Button>().onClick.AddListener(() => OnSubTopicSelected(sub));
        }
    }

    void OnSubTopicSelected(string categoryId)
    {
        PlayerPrefs.SetString("SelectedSubCategory", categoryId);
        SceneManager.LoadScene("FlashCardScene");
    }
    
    // Sửa lại hàm LoadSubTopics trong SubTopicSelectManager.cs
    void LoadSubTopics(string parentCategoryId)
    {
        // Load danh sách subtopics (như cũ)
        FirebaseDatabaseManager.Instance.LoadSubTopics(parentCategoryId, (subTopics) => 
        {
            // SAU ĐÓ: Load tiếp tiến độ của user để so sánh
            string userId = FirebaseDatabaseManager.Instance.currentUser.UserId;
            
            FirebaseDatabase.DefaultInstance
                .GetReference($"users/{userId}/learning_progress/vocab_topics/{parentCategoryId}")
                .GetValueAsync().ContinueWithOnMainThread(task => 
                {
                    DataSnapshot progressSnapshot = task.Result;
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        ToastNetwork.Instance.actionOnClickButton = () => LoadSubTopics(parentCategoryId);
                        ToastNetwork.Instance.showDisconnect();
                        return;
                    }

                    ToastNetwork.Instance.hideDisconnect();
                    Populate(subTopics, progressSnapshot);
                });
        });
    }
    
    private void OnDisable()
    {
        gameObject.SetActive(false);
    }
}
