using UnityEngine;
using System.Collections.Generic;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(gameObject.name);// Hủy cái mới ngay, KHÔNG ĐƯỢC ĐỤNG VÀO Instance cũ
            Destroy(gameObject); // Hủy cái mới ngay, KHÔNG ĐƯỢC ĐỤNG VÀO Instance cũ
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
    
    private void OnApplicationQuit()
    {
        Debug.Log("set Offline");
        FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.OFFLINE);
    }
}