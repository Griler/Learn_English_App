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

    
    void OnEnable()
    {
        LoadData();
    }

    public void LoadData()
    {
        StartCoroutine(ApiController.Instance.GetGrammarCategories(Populate));
    }
    
    void Populate(List<GrammarCategory> topics)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        foreach (GrammarCategory topic in topics)
        {
            try
            {
                GameObject topicChild = Instantiate(topicPrefab, contentParent);
                string titleTopic = topic.name.Replace("_","\n");
                topicChild.GetComponentInChildren<TextMeshProUGUI>().text = titleTopic;
                topicChild.GetComponentInChildren<Button>().onClick.AddListener(() => OnTopicSelected(topic.id));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }
    }

    void OnTopicSelected(int topicId)
    {
        PlayerPrefs.SetInt("SelectedGrammarTopic", topicId);
        SceneManager.LoadScene("SentenceBuildingScene");
    }

}