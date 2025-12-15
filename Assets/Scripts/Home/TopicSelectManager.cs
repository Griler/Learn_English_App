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
    
    void Start()
    {
        LoadMainTopics();
    }

    void LoadMainTopics()
    {
        FirebaseDatabaseManager.Instance.LoadMainTopics(Populate);
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

    void OnTopicSelected(string parentCategoryId)
    {
        PlayerPrefs.SetString("SelectedMainCategoryId", parentCategoryId);
        viewSubTopic.SetActive(true);
        gameObject.SetActive(false);
    }
}