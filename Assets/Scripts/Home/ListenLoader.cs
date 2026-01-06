﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Cần thư viện này để tìm kiếm danh sách nhanh hơn

public class ListenLoader : MonoBehaviour
{
    [Header("Firebase Config")]
    public string nodeName = "learn_topics"; 

    // Biến lưu trữ dữ liệu sau khi tải về (Cache)
    private List<ListenCategory> cachedTopics = new List<ListenCategory>();
    private List<LearnTopic> allLearnTopics = new List<LearnTopic>();
    Dictionary<string, bool> userProgress = new Dictionary<string, bool>();

    [SerializeField]private GameObject item;

    [SerializeField] private Transform container;
    // Sự kiện để báo cho các script khác biết khi nào tải xong
    public System.Action OnDataLoaded; 
    public string progressKey = "review";
    void OnEnable()
    {
        LoadDataFromFirebase();
    }

    // --- PHẦN 1: TẢI DỮ LIỆU VỀ ---
     async void LoadDataFromFirebase()
    {
        Debug.Log("Đang tải dữ liệu bài nghe...");
        
        // 1. Tải nội dung bài học trước
        allLearnTopics = await FirebaseDatabaseManager.Instance.GetAllLearnTopicsAsync();     
        userProgress = await FirebaseDatabaseManager.Instance.GetUserProgress("review");
        DisplayTopic(allLearnTopics, userProgress);

    }
    void DisplayTopic( List<LearnTopic> allLearnTopics, Dictionary<string, bool> userProgress)
    {
        foreach (Transform c in container)
        {
            c.gameObject.SetActive(false);
            Destroy(c.gameObject);
        }
        
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < allLearnTopics.Count; i++)
        {
            LearnTopic learnTopic = allLearnTopics[i];
            GameObject topicChild = Instantiate(item, container);
            topicChild.GetComponentInChildren<TextMeshProUGUI>().text = learnTopic.label.vi;
    
            if (userProgress.ContainsKey(learnTopic.key) && userProgress[learnTopic.key])
            {
                topicChild.GetComponentsInChildren<Image>()[0].color = Color.white;
            }
            else
            {
                topicChild.GetComponentsInChildren<Image>()[0].color = Color.gray;
            }
    
            int currentIndex = i; // Vẫn cần biến local
            topicChild.GetComponentInChildren<Button>().onClick.AddListener(() => OnTopicClicked(learnTopic.key));
        }
    }
    
    public void OnTopicClicked(string nameTopic)
    {
        PlayerPrefs.SetString("SelectedReviewTopic", nameTopic);
        SceneManager.LoadScene("ReviewManager");
    }

}