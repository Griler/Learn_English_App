using UnityEngine;

public static class ResourceManager
{
    /// <summary>
    /// Load 1 asset trong thư mục Resources
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu muốn load (Sprite, Prefab, SO...)</typeparam>
    /// <param name="path">Đường dẫn tính từ thư mục Resources, KHÔNG có đuôi file</param>
    public static T Load<T>(string path) where T : Object
    {
        T asset = Resources.Load<T>(path);

        if (asset == null)
        {
            Debug.LogError($"❌ Không tìm thấy asset tại path: {path}");
        }

        return asset;
    }

    /// <summary>
    /// Load tất cả asset trong 1 folder trong Resources
    /// </summary>
    public static T[] LoadAll<T>(string folderPath) where T : Object
    {
        T[] assets = Resources.LoadAll<T>(folderPath);

        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning($"⚠️ Không có asset nào trong folder: {folderPath}");
        }

        return assets;
    }

    /// <summary>
    /// Instantiate 1 prefab từ Resources
    /// </summary>
    public static GameObject InstantiatePrefab(string path, Transform parent = null)
    {
        GameObject prefab = Load<GameObject>(path);
        if (prefab == null) return null;

        return Object.Instantiate(prefab, parent);
    }
}