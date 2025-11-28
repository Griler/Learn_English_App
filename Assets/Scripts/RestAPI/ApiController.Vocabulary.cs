using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public partial class ApiController : MonoBehaviour
{
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
