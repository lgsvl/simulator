/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BuildScript
{
    static string buildInfoScriptPath = $"{Application.dataPath}{Path.DirectorySeparatorChar}Scripts{Path.DirectorySeparatorChar}BuildInfo.cs";

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

        string oldText = "";
        try
        {
            //Build bundles
            BuildScript.BuildAssetBundles(
                globalSettings.assetBundleSettings,
                assetBundle,
                Path.Combine(Directory.GetParent(location).ToString(), "AssetBundles"),
                target
                );

            UpdateBuildInfo(out oldText);

            //Build player
            BuildPipeline.BuildPlayer(scenes, location, target, mainBundle);
        }
        catch (Exception e) { throw e; }
        finally
        {
            if (oldText != "")
            {
                ResetBuildInfo(oldText);
            }

            //Reset graphics settings to initial status
            File.WriteAllText(graphicsSettingsPath, backup);
        }

        var files = new string[] { "Map.txt" };
        var source = Application.dataPath + "/../";
        var destination = Directory.GetParent(location) + "/";
        foreach (var f in files)
        {
            string srcFilePath = source + f;
            string destFolderPath = destination + f;
            if (File.Exists(srcFilePath) && Directory.Exists(destFolderPath))
            {
                FileUtil.CopyFileOrDirectory(srcFilePath, destFolderPath);
            }
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
                    assetNames = new string[] { AssetDatabase.GetAssetPath(map.sceneAsset), },
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

    static void UpdateBuildInfo(out string oldText)
    {
        var BUILD_NUMBER = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        var GIT_COMMIT = Environment.GetEnvironmentVariable("GIT_COMMIT");
        oldText = "";
        string buildVersion = "developer-build";
        bool valid = true;

        if (BUILD_NUMBER == null || GIT_COMMIT == null)
        {
            valid = false;
        }
        else if (GIT_COMMIT.Length < 7)
        {
            Debug.Log($"Environment variable {nameof(GIT_COMMIT)} is invalid");
            valid = false;
        }

        if (valid)
        {
            string gitCommitId = GIT_COMMIT.Substring(0, 7);
            buildVersion = $"{BUILD_NUMBER} ({gitCommitId})";
        }
        oldText = File.ReadAllText(buildInfoScriptPath);
        string text = oldText.Replace("${developer-build}", buildVersion);
        File.WriteAllText(buildInfoScriptPath, text);
    }

    static void ResetBuildInfo(string oldText)
    {
        File.WriteAllText(buildInfoScriptPath, oldText);
    }
}
