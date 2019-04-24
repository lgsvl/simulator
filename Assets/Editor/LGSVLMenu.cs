using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using Web;

public class LGSVLMenu
{
    [MenuItem("LGSVL/Build WebUI %b")]
    public static void BuildWebUI()
    {
        try
        {
            UnityEngine.Debug.Log("Building WebUI...");

            // Run npm with parameters
            Process proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = "run pack-p",
                WorkingDirectory = Path.Combine(Application.dataPath, "..", "WebUI"),
                UseShellExecute = true
            };

            bool result = proc.Start();
            if (!result)
            {
                throw new SystemException("could not start npm process");
            }

            proc.WaitForExit();

            UnityEngine.Debug.Log("WebUI build is completed.");

        } catch (Exception ex)
        {
            UnityEngine.Debug.LogError(
                $"Please make sure <b>npm</b> is installed. Check <color=#ff0000>README.md</color> file.\n" +
                $"Failed to build WebUI: {ex.Message}\n");
        }
    }

    [MenuItem("LGSVL/Build Environments")]
    public static void BuildEnvironments()
    {
        try
        {
            // TODO: Add code to build 3D environments
            UnityEngine.Debug.Log("Building 3D Environments...");

            string assetBundleDirectory = "Assets/AssetBundles";
            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to build Environments: {ex.Message}\n");
        }
    }

    [MenuItem("LGSVL/Build Vehicles")]
    public static void Buildvehicles()
    {
        try
        {
            // TODO: Add code to build vehicles
            UnityEngine.Debug.Log("Building Vehicles...");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to build Vehicles: {ex.Message}\n");
        }
    }
}