/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BuildScript
{
    //Portal function to get various global data 
    static GlobalSettings GetGlobalSettings()
    {
        return Resources.Load<GlobalSettings>("GlobalSettings");
    }

    static string GetBuildDestination()
    {
        string buildDestination = null;

        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-buildDestination" && i + 1 < args.Length)
            {
                buildDestination = args[i + 1];
                break;
            }
        }

        if (buildDestination == null)
        {
            throw new Exception("No -buildDestination specified on command-line!");
        }

        return buildDestination;
    }

    static void Build()
    {
        var globalSettings = GetGlobalSettings();

        // Use this for real release builds - more compression, but much slower build
        // var mainBundle = BuildOptions.None;
        // var assetBundle = BuildAssetBundleOptions.None;
        var mainBundle = BuildOptions.CompressWithLz4;
        var assetBundle = BuildAssetBundleOptions.ChunkBasedCompression;

        int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        var scenes = new string[]
        {
            // only main scene
            UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(0),
        };

        var target = EditorUserBuildSettings.activeBuildTarget;
        var location = GetBuildDestination();
       
        //Back up initial graphics settings
        var graphicsSettingsPath = Path.Combine(Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "ProjectSettings"), "GraphicsSettings.asset");
        var backup = File.ReadAllText(graphicsSettingsPath);

        //Change shader inclusion settings for specific target
        ChangeShaderInclusionSettings(globalSettings.shaderInclusionSettings, GlobalSettings.BundleBuildTarget(target));

        try
        {
            //Build bundles
            BuildScript.BuildAssetBundles(
                globalSettings.assetBundleSettings,
                assetBundle,
                Path.Combine(Directory.GetParent(location).ToString(), "AssetBundles"),
                target
                );

            //Build player
            BuildPipeline.BuildPlayer(scenes, location, target, mainBundle);
        }
        catch (Exception) { }

        //Reset graphics settings to initial status
        File.WriteAllText(graphicsSettingsPath, backup);

        var files = new string[] { "Map.txt" };
        var source = Application.dataPath + "/../";
        var destination = Directory.GetParent(location) + "/";
        foreach (var f in files)
        {
            FileUtil.CopyFileOrDirectory(source + f, destination + f);
        }
    }

    public static void BuildAssetBundles(AssetBundleSettings assetBundleSettings, BuildAssetBundleOptions assetBundle, string location, BuildTarget buildTarget)
    {
        if (assetBundleSettings == null)
        {
            return;
        }

        //Prepare asset bundle build assets
        var assets = new List<AssetBundleBuild>();
        foreach (var map in assetBundleSettings.maps)
        {
            var scn = map.sceneAsset as UnityEditor.SceneAsset;
            if (scn != null)
            {       
                assets.Add(new AssetBundleBuild()
                {
                    assetBundleName = "map_" + map.sceneAsset.name,
                    assetNames = new string[]{ AssetDatabase.GetAssetPath(map.sceneAsset), },
                });

                assets.Add(new AssetBundleBuild()
                {
                    assetBundleName = "mapimage_" + map.sceneAsset.name,
                    assetNames = new string[] { AssetDatabase.GetAssetPath(map.spriteImg), },
                    addressableNames = new string[] { "mapimage_" + map.sceneAsset.name, },
                });
            }
        }

        BuildScript.BuildAssetBundles(assets.ToArray(), assetBundle, location, buildTarget);       
    }

    static void BuildAssetBundles(AssetBundleBuild[] assets, BuildAssetBundleOptions assetBundle, string assetsPath, BuildTarget buildTarget)
    {
        if (!Directory.Exists(assetsPath))
        {
            Directory.CreateDirectory(assetsPath);
        }
        Debug.Log("building asset bundles for " + EditorUserBuildSettings.activeBuildTarget);
        BuildPipeline.BuildAssetBundles(assetsPath, assets.ToArray(), assetBundle, buildTarget);
    }

    static void ChangeShaderInclusionSettings(ShaderInclusionBuildSettings shaderInclusionSettings, ShaderInclusionBuildSettings.BundleTarget targetPlatform)
    {
        var shaderList = shaderInclusionSettings.GetShaderInclusionList(targetPlatform);
        if (shaderInclusionSettings != null && shaderList != null)
        {
            SerializedObject graphicsSettings = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty m_AlwaysIncludedShaders = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");

            var oldSize = m_AlwaysIncludedShaders.arraySize;
            var oldShaders = new List<Shader>(oldSize);
            for (int i = 0; i < oldSize; i++)
            {
                oldShaders.Add(m_AlwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader);
            }

            m_AlwaysIncludedShaders.ClearArray();
            for (int i = 0; i < shaderList.Count; i++)
            {
                m_AlwaysIncludedShaders.InsertArrayElementAtIndex(i);
                m_AlwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue = shaderList[i];
            }
            graphicsSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}
