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
        
        int parentCategoryId = PlayerPrefs.GetInt("SelectedMainCategoryId");
        LoadSubTopics(parentCategoryId);
    }

    void LoadSubTopics(int parentCategoryId)
    {
        StartCoroutine(ApiController.Instance.GetCategoriesByParent(parentCategoryId,Populate));

    }

    void Populate(List<Category> subTopics)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (Category sub in subTopics)
        {
            GameObject lessonItem = Instantiate(subTopicButtonPrefab, contentParent);
            lessonItem.GetComponent<LessonItem>().setData(topicName:sub.name);
            lessonItem.GetComponentInChildren<TMP_Text>().text = sub.name;
            lessonItem.GetComponentInChildren<Button>().onClick.AddListener(() => OnSubTopicSelected(sub.id));
        }
    }

    void OnSubTopicSelected(int categoryId)
    {
        PlayerPrefs.SetInt("SelectedSubCategory", categoryId);
        SceneManager.LoadScene("FlashCardScene");
    }
}
