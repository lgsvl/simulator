/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using System.Reflection;
public class SDFImportMenu : EditorWindow
{
    [SerializeField]
    string WorldFileName;

    SDFDocument sdfRoot = null;
    bool[] spawnLocationSelection = null;

    [MenuItem("Simulator/Import SDF", false, 500)]
    public static void Open()
    {
        var window = GetWindow(typeof(SDFImportMenu), false, "SDF Import");
        var data = EditorPrefs.GetString("Simulator/SDFImport", JsonUtility.ToJson(window, false));
        Debug.Log("data " + data);
        JsonUtility.FromJsonOverwrite(data, window);
        window.Show();
    }

    string SelectedPrefabModel = "";
    string PrefabDescription = "";
    private string PrefabName;

    private void OnGUI()
    {
        // styles
        var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Copy world and media to a subfolder in Assets/External/Environments", MessageType.Info);

        string sourcePath = "Assets/External/Environments";
        var options = new List<string>();

        foreach (var dir in Directory.EnumerateDirectories(sourcePath))
        {
            if (!Directory.Exists(dir + "/worlds")) continue;
            var files = Directory.EnumerateFiles(dir + "/worlds");
            foreach (var world in files)
            {
                if (Path.GetExtension(world) != ".world") continue;
                options.Add(world);
            }
        }

        if (options.Count == 0)
            return;

        int selectedWorldBefore = options.IndexOf(WorldFileName);
        var foundWorlds = options.Select(p => p.Substring(sourcePath.Length + 1).Replace('/', '\u2215')).ToArray();
        var selectedWorld = EditorGUILayout.Popup("World", selectedWorldBefore < 0 ? 0 : selectedWorldBefore, foundWorlds);
        WorldFileName = options[selectedWorld];

        if (selectedWorld != selectedWorldBefore || sdfRoot == null)
        {
            var combined = Path.Combine(Application.dataPath, Path.GetDirectoryName(WorldFileName), "../models");
            combined = Path.GetFullPath(combined);
            var modelPath = combined.Substring(Application.dataPath.Length + 1);
            sdfRoot = new SDFDocument(WorldFileName, modelPath);
            spawnLocationSelection = null;
        }

        EditorGUILayout.HelpBox($"version {sdfRoot.Version}", MessageType.Info);

        (GameObject, SDFModel)[] dynamicModels = null;
        if (GUILayout.Button("Import World"))
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject contentsRoot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HDRPDefaultResources/DefaultSceneRoot.prefab");
            GameObject mapOrigin = new GameObject("MapOrigin");
            mapOrigin.AddComponent<Simulator.Map.MapOrigin>();
            GameObject holder = new GameObject("MapHolder");
            holder.transform.parent = mapOrigin.transform;
            var holderComp = holder.AddComponent<Simulator.Map.MapHolder>();
            holderComp.trafficLanesHolder = holder.transform;
            holderComp.intersectionsHolder = holder.transform;

            var sceneDefaults = PrefabUtility.InstantiatePrefab(contentsRoot) as GameObject;
            var mainCamera = sceneDefaults.GetComponentInChildren<Camera>();
            Debug.Log("instantiating...");

            SDFDocument.cylinderUseMeshRadiusLengthFactor = 0.0f;
            try
            {
                AssetDatabase.StartAssetEditing();
                List<string> failedToDelete = new List<string>();
                AssetDatabase.DeleteAssets(
                    AssetDatabase.FindAssets("sdfgen_*", new[] { sdfRoot.modelPath })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray(),
                    failedToDelete);
                foreach (var asset in failedToDelete)
                {
                    Debug.Log("failed to delete previously generated asset " + asset);
                }
                sdfRoot.LoadWorld(mapOrigin, mainCamera);
                spawnLocationSelection = new bool[sdfRoot.models.Count];
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                dynamicModels = null;
                spawnLocationSelection = null;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log("import completed ");
        }

        dynamicModels = sdfRoot.models.Where(m => m.Item1 != null && !m.Item1.isStatic).ToArray();

        if (sdfRoot != null && dynamicModels.Length > 0)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Convert model to vehicle prefab", titleLabelStyle, GUILayout.ExpandWidth(true));
            Dictionary<string, GameObject> distinctModels = new Dictionary<string, GameObject>();
            foreach (var (go, sdfmodel) in dynamicModels)
            {
                if (go == null || go.isStatic) continue;
                var path = sdfmodel.document.FileName.Substring(sourcePath.Length + 1).Replace('/', '\u2215');
                if (distinctModels.ContainsKey(path)) continue;
                distinctModels.Add(path, go);
            }

            var displayOptions = distinctModels.Select(m => $"{m.Value.name} ({m.Key})").ToArray();
            int selectedModelBefore = System.Array.IndexOf(displayOptions, SelectedPrefabModel);
            int selectedModel = EditorGUILayout.Popup("Select Vehicle", selectedModelBefore < 0 ? 0 : selectedModelBefore, displayOptions);
            SelectedPrefabModel = displayOptions[selectedModel];
            if (selectedModelBefore != selectedModel)
            {
                PrefabName = dynamicModels[selectedModel].Item1.name;
            }

            EditorGUILayout.LabelField("Vehicle Name");
            PrefabName = EditorGUILayout.TextField(PrefabName);
            EditorGUILayout.LabelField("Vehicle Description");
            PrefabDescription = GUILayout.TextArea(PrefabDescription);

            if (GUILayout.Button("create prefab"))
            {
                var model = dynamicModels[selectedModel].Item1;
                if (!model.TryGetComponent(out Simulator.VehicleInfo info))
                {
                    info = model.AddComponent<Simulator.VehicleInfo>();
                }
                info.Description = PrefabDescription;

                string PrefabPath = $"Assets/External/Vehicles/{PrefabName}";

                if (!Directory.Exists(PrefabPath))
                {
                    Directory.CreateDirectory(PrefabPath);
                }

                if (model.GetComponent<IAgentController>() == null)
                {
                    Debug.LogWarning("Please add Controller component");
                }

                if (model.GetComponent<IVehicleDynamics>() == null)
                {
                    Debug.LogWarning("Please add Dynamics component");
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(model, $"{PrefabPath}/{PrefabName}.prefab", out bool success);
                Debug.Log("export prefab success: " + success);
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Convert models to spawn locations", titleLabelStyle, GUILayout.ExpandWidth(true));
            for (int i = 0; i < dynamicModels.Length; i++)
            {
                spawnLocationSelection[i] = GUILayout.Toggle(spawnLocationSelection[i], dynamicModels[i].Item1.name);
            }

            if (GUILayout.Button("convert to spawn info"))
            {
                for (int i = 0; i < dynamicModels.Length; i++)
                {
                    if (spawnLocationSelection[i])
                    {
                        var model = dynamicModels[i].Item1;
                        var spawnpoint = new GameObject("spawninfo from " + model.name);
                        spawnpoint.transform.position = model.transform.position;
                        var spawnInfo = spawnpoint.AddComponent<Simulator.Utilities.SpawnInfo>();
                        DestroyImmediate(model);
                    }
                }
            }
        }
    }
}
