using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class AssetManager : MonoBehaviour
{
    [SerializeField] private SpriteAtlas   assetAnimal;
    [SerializeField] private SpriteAtlas   assetAvatar;
    [SerializeField] private SpriteAtlas   assetBorder;
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
    public Sprite getSpriteAnimal(string name)
    {
        return assetAnimal.GetSprite(name);
    }
    
    public Sprite getSpriteAvatar(string name)
    {
        return assetAvatar.GetSprite(name);
    } 
    public Sprite getSpriteBorder(string name)
    {
        return assetBorder.GetSprite(name);
    }
}
