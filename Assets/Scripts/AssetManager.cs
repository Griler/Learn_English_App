using UnityEngine;
using UnityEngine.U2D;

public class AssetManager : MonoBehaviour
{
    [SerializeField] private SpriteAtlas   asset;
    public static AssetManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // giữ qua scene
    }
    public Sprite getSprite(string name)
    {
        return asset.GetSprite(name);
    }
}
