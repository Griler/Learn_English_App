using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public partial class ApiController : MonoBehaviour
{
    public IEnumerator GetListenCategories(System.Action<List<ListenCategory>> callback)
    {
        string url = $"{BASE_URL}/listening-categories";
        Debug.Log($"Fetching listening categories from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<ListenCategory> listenCategories = JsonConvert.DeserializeObject<List<ListenCategory>>(jsonResponse);
                callback?.Invoke(listenCategories);
            }
            else
            {
                Debug.LogError($"Error getting grammar categories: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator GetListenQuestionsByCategoryId(int categoryId, System.Action<List<ListeningQuestion>> callback)
    {
        string url = $"{BASE_URL}/listening-questions?categoryId={categoryId}";
        Debug.Log($"Fetching grammar exercises from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<ListeningQuestion> examples = JsonConvert.DeserializeObject<List<ListeningQuestion>>(jsonResponse);
                callback?.Invoke(examples);
            }
            else
            {
                Debug.LogError($"Error getting grammar example for category {categoryId}: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }
}
