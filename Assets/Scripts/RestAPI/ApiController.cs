using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

// Helper class for JSON deserialization
public static class JsonHelper
{
    public static List<T> FromJson<T>(string json)
    {
        string newJson = "{\"items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public List<T> items;
    }
}

public class ApiController : MonoBehaviour
{
    #region Singleton
    
    public static ApiController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    private const string BASE_URL = "http://localhost:8080/api"; 

    public IEnumerator GetCategoriesByParent(int? parentId = null, System.Action<List<Category>> callback = null)
    {
        string url = parentId.HasValue
            ? $"{BASE_URL}/vocabulary-categories?parentId={parentId.Value}"
            : $"{BASE_URL}/vocabulary-categories";

        Debug.Log($"Fetching categories from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<Category> categories = JsonHelper.FromJson<Category>(jsonResponse);
                callback?.Invoke(categories);
            }
            else
            {
                Debug.LogError($"Error getting categories from {url}: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }


    public IEnumerator GetVocabulariesByCategoryId(int categoryId, System.Action<List<WordData>> callback)
    {
        string url = $"{BASE_URL}/vocabularies?categoryId={categoryId}";
        Debug.Log($"Fetching vocabularies from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<WordData> vocabularies = JsonHelper.FromJson<WordData>(jsonResponse);
                callback?.Invoke(vocabularies);
            }
            else
            {
                Debug.LogError($"Error getting vocabularies for category {categoryId}: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }
}
