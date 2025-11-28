using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;
using UnityEngine.SceneManagement; // Cần thư viện này để tìm kiếm danh sách nhanh hơn

public class ListenLoader : MonoBehaviour
{
    [Header("Firebase Config")]
    public string nodeName = "listen_topics"; 

    // Biến lưu trữ dữ liệu sau khi tải về (Cache)
    private List<ListenCategory> listenCategories = new List<ListenCategory>();
    private DatabaseReference dbReference;
    
    [SerializeField]private GameObject item;

    [SerializeField] private Transform container;
    // Sự kiện để báo cho các script khác biết khi nào tải xong
    public System.Action OnDataLoaded; 

    void OnEnable()
    {
        StartCoroutine(ApiController.Instance.GetListenCategories(LoadItem));
    }

    void LoadItem(List<ListenCategory> categories)
    {
        foreach (Transform c in container) Destroy(c.gameObject);
        for (int i = 0; i < categories.Count; i++)
        {
            GameObject go = Instantiate(item, container);
            string nameTopic  = categories[i].topicName;
            int id = categories[i].id;
            go.GetComponent<SpeakingItem>().setName(nameTopic);
            go.GetComponent<SpeakingItem>().setOnClickButton(() => OnTopicClicked(id));
        }
    }

    public void OnTopicClicked(int categoryId)
    { 
        PlayerPrefs.SetInt("SelectedListenTopic", categoryId);
        SceneManager.LoadScene("ListenScene");
    }

}