using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A helper class to deserialize JSON arrays using JsonUtility.
/// JsonUtility cannot deserialize a root array directly, so we wrap it in an object.
/// </summary>
public static class JsonHelper
{
    public static List<T> FromJson<T>(string json)
    {
        // Wrap the json array string into a JSON object with an "items" key
        string newJson = "{\"items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        // The field name "items" must match the key used in the wrapper string.
        public List<T> items;
    }
}

public class ApiController : MonoBehaviour
{
    // IMPORTANT: Make sure this URL is correct for your local server
    private const string BASE_URL = "http://localhost:8080/api"; 

    // Example method to get all vocabularies
    public IEnumerator GetVocabularies(System.Action<List<Vocabulary>> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(BASE_URL + "/vocabularies"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                // Use the corrected JsonHelper to deserialize the JSON array
                List<Vocabulary> vocabularies = JsonHelper.FromJson<Vocabulary>(jsonResponse);
                callback(vocabularies);
            }
            else
            {
                Debug.LogError("Error getting vocabularies: " + webRequest.error);
                callback(null);
            }
        }
    }

    // Example method to get all categories
    public IEnumerator GetCategories(System.Action<List<Category>> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(BASE_URL + "/categories"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                // Use the corrected JsonHelper to deserialize the JSON array
                List<Category> categories = JsonHelper.FromJson<Category>(jsonResponse);
                callback(categories);
            }
            else
            {
                Debug.LogError("Error getting categories: " + webRequest.error);
                callback(null);
            }
        }
    }
}
