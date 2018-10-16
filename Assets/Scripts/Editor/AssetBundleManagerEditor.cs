/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(AssetBundleManager))]
public class AssetBundleManagerEditor : Editor
{
    AssetBundleManager assetBundleMgr;
    private BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        assetBundleMgr = (AssetBundleManager)target;
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Target Platform: ", buildTarget);
        if (GUILayout.Button("Build Bundles"))
        {
            BuildAssetBundles();
        }
    }

    public void BuildAssetBundles()
    {        
        var path = Path.Combine(Application.dataPath.Replace("Assets", ""), "AssetBundles");
        if (Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        BuildScript.BuildAssetBundles(assetBundleMgr.assetBundleSettings, BuildAssetBundleOptions.ChunkBasedCompression, path, buildTarget);
    }    
}