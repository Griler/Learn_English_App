using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public partial class ApiController : MonoBehaviour
{
    public IEnumerator GetGrammarExercisesByCategoryId(int categoryId, System.Action<List<GrammarExercise>> callback, int limit = 10)
    {
        string url = $"{BASE_URL}/grammar-exercises/random?categoryId={categoryId}&quantity={limit}";
        Debug.Log($"Fetching grammar exercises from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<GrammarExercise> exercises = JsonConvert.DeserializeObject<List<GrammarExercise>>(jsonResponse);
                callback?.Invoke(exercises);
            }
            else
            {
                Debug.LogError($"Error getting grammar exercises for category {categoryId}: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator GetGrammarExamByCategoryId(int categoryId, System.Action<List<GrammarExample>> callback, int limit = 10)
    {
        string url = $"{BASE_URL}/grammar-examples/random?categoryId={categoryId}&quantity={limit}";
        Debug.Log($"Fetching grammar exercises from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<GrammarExample> examples = JsonConvert.DeserializeObject<List<GrammarExample>>(jsonResponse);
                callback?.Invoke(examples);
            }
            else
            {
                Debug.LogError($"Error getting grammar example for category {categoryId}: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator GetGrammarCategories(System.Action<List<GrammarCategory>> callback)
    {
        string url = $"{BASE_URL}/grammar-categories";
        Debug.Log($"Fetching grammar categories from URL: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                List<GrammarCategory> grammarCategories = JsonHelper.FromJson<GrammarCategory>(jsonResponse);
                callback?.Invoke(grammarCategories);
            }
            else
            {
                Debug.LogError($"Error getting grammar categories: {webRequest.error}");
                callback?.Invoke(null);
            }
        }
    }
}
