/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class CustomEditorWindowTools : EditorWindow
{
    [MenuItem("Window/Custom Editor Tools/Select Unreadable Scene Textures Used For Lidar Sensor")]
    static void SelectUnreadableSceneTexturesUsedForLidarSensor()
    {
        FixUnreadableSceneTexturesUsedForLidarSensor(true);
    }

    [MenuItem("Window/Custom Editor Tools/Auto Fix Unreadable Scene Textures Used For Lidar Sensor")]
    static void AutoFixUnreadableSceneTexturesUsedForLidarSensor()
    {
        FixUnreadableSceneTexturesUsedForLidarSensor(false);
    }

    static void FixUnreadableSceneTexturesUsedForLidarSensor(bool selectOnly = true)
    {
        var tex2D_texImporter_map = new Dictionary<Texture2D, AssetImporter>();
        var guids = AssetDatabase.FindAssets("t:texture2d", null);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tex2D = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex2D != null)
            {
                tex2D_texImporter_map.Add(tex2D, AssetImporter.GetAtPath(path) as TextureImporter);
            }
        }

        var rends = FindObjectsOfType<Renderer>();
        var unreadableTexturesUsedForLidar = new List<Texture2D>();
        var visited = new HashSet<Texture2D>();

        foreach (var rend in rends)
        {
            var meshCol = rend.GetComponent<MeshCollider>();
            if (meshCol != null && meshCol.enabled)
            {
                var mainTex = rend.sharedMaterial?.mainTexture;
                var tex2D = mainTex != null ? (mainTex as Texture2D) : null;
                if (tex2D != null)
                {
                    if (tex2D_texImporter_map.ContainsKey(tex2D))
                    {
                        var astImporter = tex2D_texImporter_map[tex2D] as TextureImporter;
                        if (!astImporter.isReadable)
                        {
                            if (!visited.Contains(tex2D))
                            {
                                unreadableTexturesUsedForLidar.Add(tex2D);
                                visited.Add(tex2D);
                                if (selectOnly)
                                {
                                    Debug.Log($"{tex2D.name} was not readable, put in selection.");
                                }
                                else
                                {
                                    astImporter.isReadable = true;
                                    EditorUtility.SetDirty(astImporter);
                                    astImporter.SaveAndReimport();
                                    Debug.Log($"{tex2D.name} was not readable, enabled now.");
                                }
                            }
                        }
                    }
                }
            }
        }

        if (selectOnly)
        {
            Selection.objects = unreadableTexturesUsedForLidar.ToArray();
        }

        Debug.Log($"#################### processed {unreadableTexturesUsedForLidar.Count} texture assets ####################");
    }
}
