/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SDFImportMenu : EditorWindow
{
    [SerializeField]
    string WorldFileName;

    SDFDocument sdfRoot = null;
    HashSet<GameObject> spawnLocationSelection = new HashSet<GameObject>();

    [MenuItem("Simulator/Import SDF", false, 500)]
    public static void Open()
    {
        var window = GetWindow(typeof(SDFImportMenu), false, "SDF Import");
        var data = EditorPrefs.GetString("Simulator/SDFImport", JsonUtility.ToJson(window, false));
        JsonUtility.FromJsonOverwrite(data, window);
        window.Show();
    }

    string SelectedPrefabModel = string.Empty;
    string PrefabDescription = string.Empty;
    private string PrefabName;

    IEnumerable<Type> VehicleDynamics = Enumerable.Empty<Type>();
    IEnumerable<Type> AgentControllers = Enumerable.Empty<Type>();

    void OnEnable()
    {
        var assembly = System.Reflection.Assembly.Load("Simulator");
        IEnumerable<Type> types = assembly.GetTypes();
        try
        {
            var sensorAssembly = System.Reflection.Assembly.Load("Simulator.Vehicles");
            types = types.Concat(sensorAssembly.GetTypes());
        }
        catch { }
        VehicleDynamics = types.Where(t => !t.IsAbstract && typeof(IVehicleDynamics).IsAssignableFrom(t));
        AgentControllers = types.Where(t => !t.IsAbstract && typeof(IAgentController).IsAssignableFrom(t));
    }

    public static string MakeRelativePath(string fromPath, string toPath)
    {
        if (string.IsNullOrWhiteSpace(fromPath) || string.IsNullOrWhiteSpace(toPath)) throw new ArgumentException("path should not be null or whitespace");

        var from = new Uri(fromPath);
        var to = new Uri(toPath);

        var relative = from.MakeRelativeUri(to);
        var relativePath = Uri.UnescapeDataString(relative.ToString());

        return relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private void OnGUI()
    {
        // styles
        var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Import world file", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Copy world and media to a subfolder in Assets/External/Environments. Worlds should sit in 'worlds' subfolder", MessageType.Info);

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
        var worldDir = Path.GetDirectoryName(WorldFileName);

        if (selectedWorld != selectedWorldBefore || sdfRoot == null)
        {
            string modelPath;
            if (sdfRoot != null)
            {
                modelPath = sdfRoot.ModelPath;
            }
            else
            {
                var defaultLocations = new List<string> { "models", "../models", "assets/models", "../assets/models", "." };
                modelPath = defaultLocations
                .Select(p => Path.Combine(worldDir, p))
                .FirstOrDefault(p => Directory.Exists(p));
            }
            // get rid of ..
            modelPath = Path.GetFullPath(Path.Combine(Application.dataPath, modelPath)).Substring(Application.dataPath.Length + 1);
            spawnLocationSelection.Clear();
            sdfRoot = new SDFDocument(WorldFileName, modelPath);
        }

        EditorGUILayout.HelpBox($"version {sdfRoot.Version}", MessageType.Info);

        if (sdfRoot != null)
        {
            GUILayout.Label("SDF model include path");
            GUILayout.BeginHorizontal();
            GUILayout.Label(sdfRoot.ModelPath);
            if (GUILayout.Button("set"))
            {
                var path = EditorUtility.OpenFolderPanel("Add SDF include search path", sourcePath, "");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    sdfRoot.ModelPath = MakeRelativePath(Application.dataPath, path + "/");
                }
            }
            GUILayout.EndHorizontal();
        }

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
            SelectedPrefabModel = string.Empty;
            Debug.Log("instantiating...");

            SDFDocument.cylinderUseMeshRadiusLengthFactor = 0.0f;
            try
            {
                AssetDatabase.StartAssetEditing();
                List<string> failedToDelete = new List<string>();
                AssetDatabase.DeleteAssets(
                    AssetDatabase.FindAssets("sdfgen_*", new[] { sdfRoot.ModelPath })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray(),
                    failedToDelete);
                foreach (var asset in failedToDelete)
                {
                    Debug.Log("failed to delete previously generated asset " + asset);
                }
                sdfRoot.LoadWorld(mapOrigin, mainCamera);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                dynamicModels = null;
                spawnLocationSelection.Clear();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log("import completed ");
        }

        dynamicModels = sdfRoot.Models.Where(m => m.Item1 != null && !m.Item1.isStatic).ToArray();

        if (sdfRoot != null && dynamicModels.Length > 0)
        {
            GUILayout.Space(20);
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
            int selectedModelBefore = Array.IndexOf(displayOptions, SelectedPrefabModel);
            int selectedModel = EditorGUILayout.Popup("Select Vehicle", selectedModelBefore < 0 ? 0 : selectedModelBefore, displayOptions);
            SelectedPrefabModel = displayOptions[selectedModel];
            if (selectedModelBefore != selectedModel)
            {
                PrefabName = dynamicModels[selectedModel].Item1.name;
                Selection.activeGameObject = dynamicModels[selectedModel].Item1;
                SceneView.FrameLastActiveSceneView();
            }

            PrefabName = EditorGUILayout.TextField("Name", PrefabName);
            EditorGUILayout.LabelField("Description");
            PrefabDescription = GUILayout.TextArea(PrefabDescription);
            GUILayout.Space(10);

            var model = dynamicModels[selectedModel].Item1;
            var agentController = model.GetComponent<IAgentController>();
            var vehicleDynamics = model.GetComponent<IVehicleDynamics>();
            if (agentController == null)
            {
                EditorGUILayout.HelpBox($"You need to add and configure an Agent Controller component for the vehicle to work", MessageType.Warning);
                int selectedController = EditorGUILayout.Popup("Select Agent Controller", 0, AgentControllers.Select(t => t.Name).Prepend("Select to add...").ToArray());
                if (selectedController > 0)
                {
                    model.AddComponent(AgentControllers.ElementAt(selectedController - 1));
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Don't forget to configure {agentController} in the inspector view", MessageType.Info);
            }

            GUILayout.Space(10);
            if (vehicleDynamics == null)
            {
                EditorGUILayout.HelpBox($"You need to add and configure a Vehicle Dynamics component for the vehicle to work", MessageType.Warning);
                int selectedDynamics = EditorGUILayout.Popup("Select Dynamics", 0, VehicleDynamics.Select(t => t.Name).Prepend("Select to add...").ToArray());
                if (selectedDynamics > 0)
                {
                    model.AddComponent(VehicleDynamics.ElementAt(selectedDynamics - 1));
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Don't forget to configure {vehicleDynamics} in the inspector view", MessageType.Info);
            }

            EditorGUI.BeginDisabledGroup(agentController == null || vehicleDynamics == null);
            GUILayout.Space(10);
            if (GUILayout.Button("create prefab"))
            {
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
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Convert models to spawn locations", titleLabelStyle, GUILayout.ExpandWidth(true));

            foreach ((GameObject, SDFModel) v in dynamicModels)
            {
                bool has = spawnLocationSelection.Contains(v.Item1);
                bool want = GUILayout.Toggle(has, v.Item1.name);
                if (want && !has) spawnLocationSelection.Add(v.Item1);
                if (!want && has) spawnLocationSelection.Remove(v.Item1);
            }

            EditorGUILayout.HelpBox($"Selected models will be removed from the scene and replaced with SpawnInfo objects. Perform this step last, before saving the scene under Assets/External/Maps/YourMapName", MessageType.Info);

            if (GUILayout.Button("convert to spawn info"))
            {
                foreach ((GameObject, SDFModel) v in dynamicModels)
                {
                    var targetModel = v.Item1;
                    if (spawnLocationSelection.Contains(v.Item1))
                    {
                        var spawnpoint = new GameObject("spawninfo from " + targetModel.name);
                        spawnpoint.transform.position = targetModel.transform.position;
                        var spawnInfo = spawnpoint.AddComponent<Simulator.Utilities.SpawnInfo>();
                        DestroyImmediate(targetModel);
                    }
                }
            }
        }
    }
}
