using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class BaseCode : MonoBehaviour
{
    protected AssetManager assetManager
    {
        get
        {
            if (_assetManager == null)
                _assetManager = AssetManager.Instance;
            return _assetManager;
        }
    }

    private AssetManager _assetManager;
    
    protected Config config
    {
        get
        {
            if (_config == null)
                _config = Config.Instance;
            return _config;
        }
    }

    private Config _config;
}