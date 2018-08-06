/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DestroyOnLoadManager : Singleton<DestroyOnLoadManager>
{

    private List<GameObject> destroyList;

    public void Init()
    {
        destroyList = new List<GameObject>();
    }

    public void Destroy()
    {
        if (destroyList == null)
            return;

        foreach (var g in destroyList)
        {
            GameObject.Destroy(g);
        }
        destroyList.Clear();
    }

    public void Add(GameObject go)
    {
        destroyList.Add(go);
    }

}

public class DontDestroyOnLoadDriving : MonoBehaviour {

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        DestroyOnLoadManager.Instance.Add(gameObject);
    }
}
