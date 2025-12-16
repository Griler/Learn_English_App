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

    void LoadSubTopics(string parentCategoryId)
    {
        FirebaseDatabaseManager.Instance.LoadSubTopics(parentCategoryId, Populate);
    }

    void Populate(List<string> subTopics)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (string sub in subTopics)
        {
            GameObject lessonItem = Instantiate(subTopicButtonPrefab, contentParent);
            lessonItem.GetComponent<LessonItem>().setData(topicName:sub);
            lessonItem.GetComponentInChildren<TMP_Text>().text = sub;
            lessonItem.GetComponentInChildren<Button>().onClick.AddListener(() => OnSubTopicSelected(sub));
        }
    }

    void OnSubTopicSelected(string categoryId)
    {
        PlayerPrefs.SetString("SelectedSubCategory", categoryId);
        SceneManager.LoadScene("FlashCardScene");
    }

    private void OnDisable()
    {
        gameObject.SetActive(false);
    }
}
