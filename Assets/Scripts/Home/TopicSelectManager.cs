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
        StartCoroutine(ApiController.Instance.GetCategoriesByParent(null,Populate));
    }

    void Populate(List<Category> topics)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (Category topic in topics)
        {
            try
            {
                GameObject topicChild = Instantiate(topicButtonPrefab, contentParent);
                topicChild.GetComponentInChildren<TextMeshProUGUI>().text = topic.name;
                topicChild.GetComponentInChildren<Button>().onClick.AddListener(() => OnTopicSelected(topic.id));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }
    }

    void OnTopicSelected(int parentCategoryId)
    {
        PlayerPrefs.SetInt("SelectedMainCategoryId", parentCategoryId);
        viewSubTopic.SetActive(true);
        gameObject.SetActive(false);
    }
}