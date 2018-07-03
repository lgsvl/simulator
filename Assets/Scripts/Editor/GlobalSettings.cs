/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

public class GlobalSettings : SingletonScriptableObject<GlobalSettings>
{
    [MenuItem("Assets/Create/Custom/GlobalSettings")]
    public static void CreateMyAsset()
    {
        if (Instance == null)
        {
            _instance = ScriptableObject.CreateInstance<GlobalSettings>();
            AssetDatabase.CreateAsset(_instance, "Assets/NewGlobalSettings.asset");
            AssetDatabase.SaveAssets();
        }
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = Instance;
    }

    public AssetBundleSettings assetBundleSettings;
    public static ShaderInclusionBuildSettings.BundleTarget BundleBuildTarget(BuildTarget target)
    {
        switch (target)
        {
            case UnityEditor.BuildTarget.StandaloneWindows64:
                return ShaderInclusionBuildSettings.BundleTarget.Win64;
            case UnityEditor.BuildTarget.StandaloneLinux64:
                return ShaderInclusionBuildSettings.BundleTarget.Linux64;
            default:
                return ShaderInclusionBuildSettings.BundleTarget.Win64;
        }
    }
    public ShaderInclusionBuildSettings shaderInclusionSettings;
}
