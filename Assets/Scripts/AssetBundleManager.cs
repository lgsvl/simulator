/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MapInfo
{
    public Object sceneAsset;
    public Sprite spriteImg;
}

public class AssetBundleManager : MonoBehaviour
{
    private static AssetBundleManager instance = null;
    public static AssetBundleManager Instance
    {
        get
        {
            return instance;
        }
    }

    public AssetBundleSettings assetBundleSettings;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            DestroyImmediate(gameObject);
        }
    }
}
