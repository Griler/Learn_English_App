using UnityEngine;

public partial class ApiController : MonoBehaviour
{
    #region Singleton
    
    public static ApiController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    private const string BASE_URL = "http://localhost:8080/api"; 
}
