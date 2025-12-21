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
    
    void OnEnable()
    {
        LoadMainTopics();
    }

    void LoadMainTopics()
    {
        FirebaseDatabaseManager.Instance.LoadMainTopics(Populate);
    }

    void Populate(Dictionary<string,bool> topics)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (var topic in topics)
        {
            try
            {
                GameObject topicChild = Instantiate(topicButtonPrefab, contentParent);
                topicChild.GetComponentInChildren<TextMeshProUGUI>().text = GlobalData.mapNameVocabulary[topic.Key];
                if (topic.Value)
                {
                    topicChild.GetComponentsInChildren<Image>()[0].color = Color.white;
                }
                else
                {
                    topicChild.GetComponentsInChildren<Image>()[0].color = Color.gray;
                }
                topicChild.GetComponentInChildren<Button>().onClick.AddListener(() => OnTopicSelected(topic.Key));
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