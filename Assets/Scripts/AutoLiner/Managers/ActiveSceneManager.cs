/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TestState
{
    None,
    Init,
    Warmup,
    Running
};

public class ActiveSceneManager : MonoBehaviour
{
    #region Singelton
    private static ActiveSceneManager _instance = null;
    public static ActiveSceneManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<ActiveSceneManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>ActiveSceneManager" +
                        " Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public TestState currentTestState { get; private set; }
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
        SetTestState(TestState.None);
    }

    void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region methods
    public void SetTestState(TestState state)
    {
        currentTestState = state;
        TestStateMissive missive = new TestStateMissive
        {
            state = currentTestState
        };
        Missive.Send(missive);
    }
    #endregion
}

#region missives
public class TestStateMissive : Missive
{
    public TestState state = TestState.None;
}
#endregion
