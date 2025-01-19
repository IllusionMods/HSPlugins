using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MainThreadDispatcher>();

                if (_instance == null)
                {
                    GameObject obj = new GameObject("MainThreadDispatcher");
                    _instance = obj.AddComponent<MainThreadDispatcher>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    private readonly Queue<Action> _actions = new Queue<Action>();

    /// <summary>
    /// Schedules an action for execution on the main thread. This is useful for secondary threads to schedule thread unsafe actions that must be executed in the main Unity thread.
    /// </summary>
    public void Dispatch(Action action)
    {
        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }

    private void Update()
    {
        while (_actions.Count > 0)
        {
            Action action = null;
            lock (_actions)
            {
                action = _actions.Dequeue();
            }
            action?.Invoke();
        }
    }
}