using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic; // Cần thiết để sử dụng List<>

// --- CÁC MODEL DỮ LIỆU ĐỂ XỬ LÝ JSON ---

// Đại diện cho một bản ghi lịch sử ngữ pháp từ JSON
[System.Serializable]
public class UserGrammarHistory
{
    public long id;
    public string userId;
    public long categoryId;
}


public partial class ApiController : MonoBehaviour
{
    // URL gốc của ứng dụng Spring Boot của bạn.
    private const string BaseUrl = "http://localhost:8080/api/user-category-history";

    // Enum để lưu lịch sử
    public enum CategoryType
    {
        Listening,
        Speaking,
        Vocabulary,
        Grammar
    }
    
    public void SaveUserCategoryHistory(string userId, long categoryId, CategoryType categoryType)
    {
        StartCoroutine(PostHistoryRequest(userId, categoryId, categoryType));
    }

    private IEnumerator PostHistoryRequest(string userId, long categoryId, CategoryType categoryType)
    {
        string categoryPath = categoryType.ToString().ToLower();
        string url = $"{BaseUrl}/{categoryPath}?userId={UnityWebRequest.EscapeURL(userId)}&categoryId={categoryId}";
        using (UnityWebRequest webRequest = UnityWebRequest.Post(url, new WWWForm()))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Lỗi khi lưu lịch sử: {webRequest.error}");
            }
            else
            {
                Debug.Log($"Lưu lịch sử thành công! Phản hồi: {webRequest.downloadHandler.text}");
            }
        }
    }
    
}