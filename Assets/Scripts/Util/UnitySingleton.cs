/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;

/// <summary>
/// Abstract Unity Component Singleton.
/// </summary>
public abstract class UnitySingleton<T> : MonoBehaviour where T : Component
{
    protected static T _instance = default(T);

    public static bool IsInstantiated { get { return _instance != default(T); } }

    public static T Instance
    {
        get
        {
            if(_instance == null)
            {
                T t = (T)FindObjectOfType(typeof(T));
                if(t != null)
                {
                    _instance = t;
                }
            }
            return _instance;
        }
    }

    public static void CreateInstance()
    {
        if (_instance == null)
        {
            T t = (T)FindObjectOfType(typeof(T));
            if (t == null)
            {
                GameObject go = new GameObject();
                _instance = go.AddComponent<T>();
                go.name = typeof(T).ToString();
            }
            else
            {
                _instance = t;
            }
        }
    }

    public static T GetInstance<U>() where U : T {
        
        if(_instance == null || !(_instance is U))
        {
            U u = FindObjectOfType(typeof(U)) as U;
            if(u != null)
            {
                _instance = u;
            }
        }
        return _instance;
    }

    #region Unity events
    protected virtual void Awake()
    {
        _instance = Instance;
        if (_instance != null && _instance != this)
        {
            Destroy(this);
        }
    }


    protected virtual void OnDestroy()
    {
        if(_instance == this)
            _instance = default(T);
    }


    protected virtual void OnApplicationQuit()
    {
        if(_instance == this)
            _instance = default(T);
    }
    #endregion
}


public abstract class PersistentUnitySingleton<T> : UnitySingleton<T> where T : Component
{
    protected override void Awake()
    {
        base.Awake();
        if(_instance == this)
            DontDestroyOnLoad(gameObject);
    }
}