using UnityEngine;
using System.Collections.Generic;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            _instance = new GameObject("UnityMainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>();
            if (Application.isPlaying) 
            {
                DontDestroyOnLoad(_instance.gameObject);
            }
        }
        return _instance;
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
        FirebaseDatabaseManager.Instance.SetUserStatus(GlobalData.STATUS.OFFLINE);
    }
}