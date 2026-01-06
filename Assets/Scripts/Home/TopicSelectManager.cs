using System;
using System.Collections;
using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class TopicSelectManager : MonoBehaviour
{
    [SerializeField] Transform contentParent;
    [SerializeField] GameObject topicButtonPrefab;
    [SerializeField] GameObject subTopicPrefab;
    [SerializeField] GameObject viewSubTopic;
    [SerializeField] Transform contentParentSub;
    private List<LearnTopic> allLearnTopics = new List<LearnTopic>();
    Dictionary<string, bool> userProgress = new Dictionary<string, bool>();
    void OnEnable()
    {
        LoadAndMapVocabulary();
    }
    
    async void LoadAndMapVocabulary()
    {
        allLearnTopics = await FirebaseDatabaseManager.Instance.GetAllLearnTopicsAsync();
        userProgress = await FirebaseDatabaseManager.Instance.GetUserProgress("vocab_topics");
        DisplayTopic(allLearnTopics, userProgress);
    }

    void DisplayTopic( List<LearnTopic> allLearnTopics, Dictionary<string, bool> userProgress)
    {
        foreach (Transform c in contentParent)
        {
            c.gameObject.SetActive(false);
            Destroy(c.gameObject);
        }
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < allLearnTopics.Count; i++)
        {
            LearnTopic learnTopic = allLearnTopics[i];
            GameObject topicChild = Instantiate(topicButtonPrefab, contentParent);
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
            topicChild.GetComponentInChildren<Button>().onClick.AddListener(() => OnTopicSelected(currentIndex, learnTopic.key));
        }
    }

    void DisplaySubTopic(int index)
    {
        LoadingController.Instance.Hide();
        foreach (Transform c in contentParentSub)
        {
            c.gameObject.SetActive(false);
            Destroy(c.gameObject);
        }  
        Canvas.ForceUpdateCanvases();
        int indexSubTopic = 0;
        foreach (var subTopic in allLearnTopics[index].subs)
        {
            GameSessionData.CurrentSubTopics.Add(subTopic.Key);
            GameObject lessonItem = Instantiate(subTopicPrefab, contentParentSub);
            GameSessionData.mapSubTopics[subTopic.Key] = indexSubTopic;
            lessonItem.GetComponent<LessonItem>().setData(topicName: subTopic.Value.vi);
            if (userProgress.ContainsKey(subTopic.Key) && userProgress[subTopic.Key])
            {
                lessonItem.GetComponent<LessonItem>().setHightLightStart();
            }
            else
            {
                lessonItem.GetComponent<LessonItem>().setDisableStart();
            }
            lessonItem.GetComponentInChildren<Button>().onClick.AddListener(() => OnClickLearn(subTopic.Key));
        }
    }

    void OnTopicSelected(int index, string mainTopic )
    {
        PlayerPrefs.SetString("SelectedMainCategoryId", mainTopic);
        gameObject.SetActive(false);
        viewSubTopic.SetActive(true);
        DisplaySubTopic(index);
    }

    void OnClickLearn(string categoryId)
    {
        PlayerPrefs.SetString("SelectedSubCategory", categoryId);
        SceneManager.LoadScene("FlashCardScene");
    }
    
}
// Class cho Label (hỗ trợ đa ngôn ngữ)
[Serializable]
public class Localization 
{
    public string en;
    public string vi;
}

// Class cho SubTopic
[Serializable]
public class SubTopic
{
    public string key;
    public Localization label;
}

// Class cho Topic
[Serializable]
public class LearnTopic
{
    public string key;
    public Localization label;
    public Dictionary<string,Localization> subs;
}