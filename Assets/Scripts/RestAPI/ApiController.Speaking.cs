using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public partial class ApiController : MonoBehaviour
{
    public IEnumerator GetSpeakingCategories(System.Action<List<SpeakingCategory>> callback)
    {
        string url = $"{BASE_URL}/speaking-categories";
        Debug.Log($"Fetching speaking categories from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<SpeakingCategory> categories = JsonConvert.DeserializeObject<List<SpeakingCategory>>(jsonResponse);
                callback?.Invoke(categories);
            }
            else
            {
                Debug.LogError($"Error getting speaking categories: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator GetSpeakingQuestionsByCategoryId(int categoryId, System.Action<List<SpeakingQuestion>> callback)
    {
        string url = $"{BASE_URL}/speaking-questions?categoryId={categoryId}";
        Debug.Log($"Fetching speaking questions from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<SpeakingQuestion> questions = JsonConvert.DeserializeObject<List<SpeakingQuestion>>(jsonResponse);
                callback?.Invoke(questions);
            }
            else
            {
                Debug.LogError($"Error getting speaking questions for category {categoryId}: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }
}
