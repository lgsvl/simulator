/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    #region Singelton
    private static DebugManager _instance = null;
    public static DebugManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<DebugManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>DebugManager" +
                        " Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    //
    #endregion

    #region mono
    void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        //
    }

    void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion
}
