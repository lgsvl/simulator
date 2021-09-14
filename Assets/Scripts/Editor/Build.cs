/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Simulator.Bridge;
using Simulator.FMU;
using Simulator.Map;
using Simulator.PointCloud.Trees;
using Simulator.Sensors;
using Simulator.Utilities;
using System.Threading;

namespace Simulator.Editor
{
    public class Build : EditorWindow
    {
        private enum BuildTarget
        {
            Windows,
            Linux,
            MacOS,
        }

        public static bool Running;
        public static bool BuildSuccessful;
        private static bool BuildQueued;
        private static string CurrentSelectedEntryName = "nothing";
        private static string HelpMsg = "";
        private static Dictionary<string, BundleData> BuildGroups;

        public const string SceneExtension = "unity";
        public const string ScriptExtension = "cs";
        public const string PrefabExtension = "prefab";

        [SerializeField]
        private BuildTarget Target;
        [SerializeField]
        private static bool BuildPlayer = false;
        [SerializeField]
        private string PlayerFolder = string.Empty;
        [SerializeField]
        private bool DevelopmentPlayer = false;
        private Vector2 ScrollPos;

        #region data
        public class BundleData
        {
            public BundleData(BundleConfig.BundleTypes type, string path = null)
            {
                bundleType = type;
                bundlePath = path ?? BundleConfig.pluralOf(type);
            }

            public class Entry
            {
                public string name;
                public string mainAssetFile;
                public bool selected;
                public bool available;
            }

            public BundleConfig.BundleTypes bundleType;
            public string bundlePath;
            public string sourcePath => Path.Combine(BundleConfig.ExternalBase, bundlePath);
            public bool isOpen = false;

            public Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

            public void OnGUI()
            {
                if (entries.Count == 0)
                {
                    EditorGUILayout.HelpBox($"No {bundlePath} are available", UnityEditor.MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Following {bundlePath} were automatically detected:", UnityEditor.MessageType.None);
                }

                #region unity 2020.3.3f1 fix
                // TODO fix for issues with unity 2020.3.3f1 multiple bundles

                //if (entries.Count != 0)
                //{
                //    EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
                //    if (GUILayout.Button("Select All", GUILayout.ExpandWidth(false)))
                //    {
                //        foreach (var entry in entries)
                //        {
                //            entry.Value.selected = true;
                //        }
                //    }
                //    if (GUILayout.Button("Select None", GUILayout.ExpandWidth(false)))
                //    {
                //        foreach (var entry in entries)
                //        {
                //            entry.Value.selected = false;
                //        }
                //    }
                //    EditorGUILayout.EndHorizontal();
                //}

                foreach (var entry in entries.OrderBy(entry => entry.Key))
                {
                    if (entry.Value.available)
                    {
                        if (GUILayout.Toggle(entry.Value.selected, entry.Key))
                        {
                            BuildPlayer = false;
                            foreach (var group in BuildGroups.Values)
                            {
                                foreach (var e in group.entries.Values)
                                {
                                    e.selected = false;
                                }
                            }
                            entry.Value.selected = true;
                            CurrentSelectedEntryName = entry.Value.name;
                        }
                        else
                        {
                            entry.Value.selected = false;
                        }
                    }
                    else
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        GUILayout.Toggle(false, $"{entry.Key} (missing items/{entry.Value.mainAssetFile}");
                        EditorGUI.EndDisabledGroup();
                    }
                }
                #endregion
            }

            public void Refresh()
            {
                var updated = new HashSet<string>();
                foreach (var entry in Directory.EnumerateDirectories(sourcePath))
                {
                    var name = Path.GetFileName(entry);

                    if (name.StartsWith("."))
                    {
                        continue;
                    }
                    if (!entries.ContainsKey(name))
                    {
                        var extension = bundleType == BundleConfig.BundleTypes.Environment ? SceneExtension : bundleType == BundleConfig.BundleTypes.Bridge ? ScriptExtension : PrefabExtension;
                        var fullPath = Path.Combine(sourcePath, name, $"{name}.{extension}");

                        // NPC type can be both prefab and behaviour script
                        if (bundleType == BundleConfig.BundleTypes.NPC && !File.Exists(fullPath))
                        {
                            extension = ScriptExtension;
                            fullPath = Path.Combine(sourcePath, name, $"{name}.{extension}");
                        }

                        entries.Add(name, new Entry
                        {
                            name = name,
                            mainAssetFile = fullPath,
                            available = File.Exists(fullPath),
                            selected = false
                        });
                    }
                    updated.Add(name);
                }
                entries = entries.Where(entry => updated.Contains(entry.Key)).ToDictionary(p => p.Key, p => p.Value);
            }

            public bool EnableByName(string name)
            {
                if (!entries.ContainsKey(name))
                {
                    var knownKeys = string.Join(",", entries.Keys);
                    Debug.LogWarning($"[BUILD] could not enable entry {name} as it was not found. Known entries of {bundlePath} are {knownKeys}");

                    return false;
                }

                Debug.Log($"[BUILD] Enable entry '{name}' of {bundlePath}");

                entries[name].selected = true;
                return true;
            }

            private void PreparePrefabManifest(Entry prefabEntry, string outputFolder, List<(string, string)> buildArtifacts, Manifest manifest)
            {
                const string vehiclePreviewScenePath = "Assets/PreviewEnvironmentAssets/PreviewEnvironmentScene.unity";

                string assetGuid = Guid.NewGuid().ToString();
                manifest.assetName = prefabEntry.name;
                manifest.assetGuid = assetGuid;
                manifest.assetFormat = BundleConfig.Versions[bundleType];
                manifest.description = "";
                manifest.fmuName = "";
                manifest.bridgeDataTypes = Array.Empty<string>();

                Scene scene = EditorSceneManager.OpenScene(vehiclePreviewScenePath, OpenSceneMode.Additive);
                var scenePath = scene.path;

                try
                {
                    if (bundleType == BundleConfig.BundleTypes.Vehicle)
                    {
                        var vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabEntry.mainAssetFile);
                        var rigidbody = vehiclePrefab.GetComponent<Rigidbody>();
                        var articulationBody = vehiclePrefab.GetComponent<ArticulationBody>();
                        if (rigidbody == null && articulationBody == null)
                        {
                            throw new Exception($"Build failed: Rigidbody or ArticulationBody on {prefabEntry.mainAssetFile} not found. Please add a Rigidbody component and rebuild.");
                        }

                        var controller = vehiclePrefab.GetComponent<IAgentController>();
                        if (controller == null)
                        {
                            throw new Exception($"Build failed: IAgentController implementation on {prefabEntry.mainAssetFile} not found. Please add a component implementing IAgentController and rebuild.");
                        }

                        var info = vehiclePrefab.GetComponent<VehicleInfo>();
                        var fmu = vehiclePrefab.GetComponent<VehicleFMU>();
                        var baseLink = vehiclePrefab.GetComponentInChildren<BaseLink>();

                        if (info == null)
                        {
                            Debug.LogWarning($"Build warning: Vehicle info on {prefabEntry.mainAssetFile} not found. Please add a VehicleInfo component to include meta data in the manifest.");
                        }

                        manifest.description = info != null ? info.Description : "";
                        manifest.assetType = "vehicle";
                        manifest.fmuName = fmu == null ? "" : fmu.FMUData.Name;

                        manifest.baseLink = baseLink != null
                            ? new double[] { baseLink.transform.position.x, baseLink.transform.position.y, baseLink.transform.position.z }
                            : // rotation
                            new double[] { 0, 0, 0 };

                        Dictionary<string, object> files = new Dictionary<string, object>();
                        manifest.attachments = files;

                        using (var exporter = new GltfExporter(prefabEntry.mainAssetFile, manifest.assetGuid, manifest.assetName))
                        {
                            var fileName = exporter.Export(outputFolder);
                            var glbOut = Path.Combine(outputFolder, fileName);
                            buildArtifacts.Add((glbOut, Path.Combine("gltf", fileName)));
                            files.Add("gltf", ZipPath("gltf", fileName));
                        }

                        var textures = new BundlePreviewRenderer.PreviewTextures();
                        BundlePreviewRenderer.RenderVehiclePreview(prefabEntry.mainAssetFile, textures);
                        var bytesLarge = textures.large.EncodeToPNG();
                        textures.Release();

                        string tmpdir = Path.Combine(outputFolder, $"{manifest.assetName}_pictures");
                        Directory.CreateDirectory(tmpdir);
                        File.WriteAllBytes(Path.Combine(tmpdir, "preview-1.png"), bytesLarge);

                        var images = new string[]
                        {
                            ZipPath("images", "preview-1.png"),
                        };
                        manifest.attachments.Add("images", images);

                        buildArtifacts.Add((Path.Combine(tmpdir, "preview-1.png"), images[0]));
                        buildArtifacts.Add((tmpdir, null));
                    }
                }
                finally
                {
                    var openedScene = SceneManager.GetSceneByPath(scenePath);
                    EditorSceneManager.CloseScene(openedScene, true);
                }
            }

            private void PrepareSceneManifest(Entry sceneEntry, string outputFolder, List<(string, string)> buildArtifacts, Manifest manifest)
            {
                Scene scene = EditorSceneManager.OpenScene(sceneEntry.mainAssetFile, OpenSceneMode.Additive);
                var scenePath = scene.path;
                NodeTreeLoader[] loaders = GameObject.FindObjectsOfType<NodeTreeLoader>();
                string dataPath = GameObject.FindObjectOfType<NodeTreeLoader>()?.GetFullDataPath();
                List<Tuple<string, string>> loaderPaths = new List<Tuple<string, string>>();

                foreach (NodeTreeLoader loader in loaders)
                {
                    loaderPaths.Add(new Tuple<string, string>(Utilities.Utility.StringToGUID(loader.GetDataPath()).ToString(), loader.GetFullDataPath()));
                }

                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        MapOrigin origin = root.GetComponentInChildren<MapOrigin>();
                        if (origin != null)
                        {
                            manifest.assetName = sceneEntry.name;
                            manifest.assetType = "map";
                            manifest.assetGuid = Guid.NewGuid().ToString();
                            manifest.mapOrigin = new double[] { origin.OriginEasting, origin.OriginNorthing };
                            manifest.assetFormat = BundleConfig.Versions[BundleConfig.BundleTypes.Environment];
                            manifest.description = origin.Description;
                            manifest.fmuName = "";
                            manifest.attachments = new Dictionary<string, object>();

                            string name = manifest.assetName;

                            var hdMaps = new HdMaps()
                            {
                                apollo50 = ZipPath("hdmaps", "apollo50", "base_map.bin"),
                                autoware = ZipPath("hdmaps", "autoware", "AutowareVectorMap.zip"),
                                lanelet2 = ZipPath("hdmaps", "lanelet2", name + ".osm"),
                                opendrive = ZipPath("hdmaps", "opendrive", name + ".xodr"),
                            };
                            manifest.attachments.Add("hdMaps", hdMaps);

                            string tmpdir = "";
                            Lanelet2MapExporter lanelet2MapExporter = new Lanelet2MapExporter();
                            tmpdir = Path.Combine(outputFolder, $"{name}_lanelet2");
                            Directory.CreateDirectory(tmpdir);
                            if (lanelet2MapExporter.Export(Path.Combine(tmpdir, $"{name}.osm")))
                            {
                                buildArtifacts.Add((Path.Combine(tmpdir, $"{name}.osm"), hdMaps.lanelet2));
                                buildArtifacts.Add((tmpdir, null));
                            }
                            else
                            {
                                Directory.Delete(tmpdir);
                            }

                            OpenDriveMapExporter openDriveMapExporter = new OpenDriveMapExporter();
                            tmpdir = Path.Combine(outputFolder, $"{name}_opendrive");
                            Directory.CreateDirectory(tmpdir);
                            if (openDriveMapExporter.Export(Path.Combine(tmpdir, $"{manifest.assetName}.xodr")))
                            {
                                buildArtifacts.Add((Path.Combine(tmpdir, $"{manifest.assetName}.xodr"), hdMaps.opendrive));
                                buildArtifacts.Add((tmpdir, null));
                            }
                            else
                            {
                                Directory.Delete(tmpdir);
                            }

                            ApolloMapTool apolloMapTool = new ApolloMapTool();
                            tmpdir = Path.Combine(outputFolder, $"{name}_apollomap_5_0");
                            Directory.CreateDirectory(tmpdir);
                            if (apolloMapTool.Export(Path.Combine(tmpdir, "base_map.bin")))
                            {
                                buildArtifacts.Add((Path.Combine(tmpdir, "base_map.bin"), hdMaps.apollo50));
                                buildArtifacts.Add((tmpdir, null));
                            }
                            else
                            {
                                Directory.Delete(tmpdir);
                            }

                            AutowareMapTool autowareMapTool = new AutowareMapTool();
                            tmpdir = Path.Combine(outputFolder, $"{name}_autoware");
                            Directory.CreateDirectory(tmpdir);
                            var autowareMapFolder = Path.Combine(tmpdir, "AutowareVectorMap");
                            Directory.CreateDirectory(autowareMapFolder);
                            if (autowareMapTool.Export(Path.Combine(tmpdir, "AutowareVectorMap")))
                            {
                                var archive = ZipFile.Create(Path.Combine(tmpdir, "AutowareVectorMap.zip"));
                                archive.BeginUpdate();
                                var files = Directory.GetFiles(autowareMapFolder);
                                foreach (var filename in files)
                                {
                                    archive.Add(new StaticDiskDataSource(filename), Path.GetFileName(filename), CompressionMethod.Stored, true);
                                }
                                archive.CommitUpdate();
                                archive.Close();

                                buildArtifacts.Add((Path.Combine(tmpdir, "AutowareVectorMap.zip"), hdMaps.autoware));
                                buildArtifacts.Add((tmpdir, null));

                                Directory.Delete(autowareMapFolder, true);
                            }
                            else
                            {
                                Directory.Delete(tmpdir, true);
                            }

                            var previewOrigin = origin.transform;
                            var spawns = FindObjectsOfType<SpawnInfo>().OrderBy(spawn => spawn.name).ToList();
                            var mapPreview = FindObjectOfType<MapPreview>();
                            var forcePreview = false;
                            if (mapPreview != null)
                            {
                                previewOrigin = mapPreview.transform;
                                forcePreview = true;
                            }
                            else if (spawns.Count > 0)
                            {
                                previewOrigin = spawns[0].transform;
                            }
                            else
                            {
                                Debug.LogError("No spawns or map preview found, preview will be rendered from origin.");
                            }

                            var textures = new BundlePreviewRenderer.PreviewTextures();
                            BundlePreviewRenderer.RenderScenePreview(previewOrigin, textures, forcePreview);
                            var bytesLarge = textures.large.EncodeToPNG();
                            textures.Release();

                            tmpdir = Path.Combine(outputFolder, $"{name}_pictures");
                            Directory.CreateDirectory(tmpdir);
                            File.WriteAllBytes(Path.Combine(tmpdir, "preview-1.png"), bytesLarge);

                            var images = new string[]
                            {
                                ZipPath("images", "preview-1.png"),
                            };
                            manifest.attachments.Add("images", images);
                            buildArtifacts.Add((Path.Combine(tmpdir, "preview-1.png"), images[0]));
                            buildArtifacts.Add((tmpdir, null));

                            foreach (Tuple<string, string> t in loaderPaths)
                            {
                                if (!manifest.attachments.ContainsKey($"pointcloud_{t.Item1}"))
                                {
                                    manifest.attachments.Add($"pointcloud_{t.Item1}", t.Item2);
                                }
                            }

                            return;
                        }
                    }
                    throw new Exception($"Build failed: MapOrigin on {sceneEntry.name} not found. Please add a MapOrigin component.");
                }
                finally
                {
                    var openedScene = SceneManager.GetSceneByPath(scenePath);
                    EditorSceneManager.CloseScene(openedScene, true);
                }
            }

            public void RunBuild(string outputFolder)
            {
                const string loaderScenePath = "Assets/Scenes/LoaderScene.unity";
                string Thing = BundleConfig.singularOf(bundleType);
                string Things = BundleConfig.pluralOf(bundleType);
                string thing = Thing.ToLower();

                outputFolder = Path.Combine(outputFolder, bundlePath);
                Directory.CreateDirectory(outputFolder);
                var openScenePaths = new List<string>();

                var selected = entries.Values.Where(e => e.selected && e.available).ToList();
                if (selected.Count == 0) return;

                if (bundleType == BundleConfig.BundleTypes.Environment)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        Debug.LogWarning("Cancelling the build.");
                        return;
                    }
                }

                var activeScenePath = SceneManager.GetActiveScene().path;
                for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    openScenePaths.Add(scene.path);
                }

                EditorSceneManager.OpenScene(loaderScenePath, OpenSceneMode.Single);

                try
                {
                    foreach (var entry in selected)
                    {
                        BuildSuccessful = false;
                        Manifest manifest = new Manifest();
                        var buildArtifacts = new List<(string source, string archiveName)>();
                        var persistentBuildArtifacts = new List<(string source, string archiveName)>();
                        bool mainAssetIsScript = entry.mainAssetFile.EndsWith("." + ScriptExtension);
                        bool hasPrefabs = AssetDatabase.FindAssets("t:GameObject", new[] { Path.Combine(sourcePath, entry.name) }).Any();
                        if (bundleType == BundleConfig.BundleTypes.Environment)
                        {
                            PrepareSceneManifest(entry, outputFolder, buildArtifacts, manifest);
                            manifest.assetType = "map";
                        }
                        else
                        {
                            PreparePrefabManifest(entry, outputFolder, buildArtifacts, manifest);
                            manifest.assetType = thing;
                        }

                        var asmDefPath = Path.Combine(BundleConfig.ExternalBase, Things, $"Simulator.{Things}.asmdef");
                        AsmdefBody asmDef = null;
                        if (File.Exists(asmDefPath))
                        {
                            asmDef = JsonUtility.FromJson<AsmdefBody>(File.ReadAllText(asmDefPath));
                        }

                        try
                        {
                            Debug.Log($"Building asset: {entry.mainAssetFile} -> " + Path.Combine(outputFolder, $"{thing}_{entry.name}"));

                            if (!File.Exists(Path.Combine(Application.dataPath, "..", entry.mainAssetFile)))
                            {
                                Debug.LogError($"Building of {entry.name} failed: {entry.mainAssetFile} not found");
                                break;
                            }

                            if (asmDef != null)
                            {
                                AsmdefBody asmdefContents = new AsmdefBody();
                                asmdefContents.name = entry.name;
                                asmdefContents.references = asmDef.references;
                                var asmDefOut = Path.Combine(sourcePath, entry.name, $"{entry.name}.asmdef");
                                File.WriteAllText(asmDefOut, JsonUtility.ToJson(asmdefContents));
                                buildArtifacts.Add((asmDefOut, null));
                            }

                            AssetDatabase.Refresh();
                            if (hasPrefabs)
                            {
                                var texturesNames = new List<string>();
                                var assetsNames = new List<string>();
                                switch (bundleType)
                                {
                                    case BundleConfig.BundleTypes.Vehicle:
                                    case BundleConfig.BundleTypes.Environment:
                                    case BundleConfig.BundleTypes.Sensor:
                                    case BundleConfig.BundleTypes.Bridge:
                                    case BundleConfig.BundleTypes.Pedestrian:
                                        ////Include the main asset only
                                        texturesNames.AddRange(AssetDatabase.GetDependencies(entry.mainAssetFile).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg") || a.EndsWith(".tga")).ToArray());
                                        assetsNames.Add(entry.mainAssetFile);
                                        break;
                                    case BundleConfig.BundleTypes.NPC:
                                    case BundleConfig.BundleTypes.Controllable:
                                        //Add all prefabs and required textures to the bundle
                                        var assetPath = Path.Combine(sourcePath, entry.name);
                                        var assetsInDirectory = AssetDatabase.FindAssets("t:GameObject", new[] { assetPath });
                                        foreach (var assetGuid in assetsInDirectory)
                                        {
                                            var asset = AssetDatabase.GUIDToAssetPath(assetGuid);
                                            if (!asset.EndsWith(".prefab"))
                                                continue;
                                            texturesNames.AddRange(AssetDatabase.GetDependencies(asset).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg") || a.EndsWith(".tga")));
                                            assetsNames.Add(asset);
                                        }
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                var textureBuild = new AssetBundleBuild()
                                {
                                    assetBundleName = $"{manifest.assetGuid}_{thing}_textures",
                                    assetNames = texturesNames.Distinct().ToArray()
                                };
                                bool buildTextureBundle = textureBuild.assetNames.Length > 0;

                                var windowsBuild = new AssetBundleBuild()
                                {
                                    assetBundleName = $"{manifest.assetGuid}_{thing}_main_windows",
                                    assetNames = assetsNames.ToArray()
                                };

                                var linuxBuild = new AssetBundleBuild()
                                {
                                    assetBundleName = $"{manifest.assetGuid}_{thing}_main_linux",
                                    assetNames = assetsNames.ToArray()
                                };

                                var builds = new[]
                                {
                                    (build: linuxBuild, platform: UnityEditor.BuildTarget.StandaloneLinux64),
                                    (build: windowsBuild, platform: UnityEditor.BuildTarget.StandaloneWindows64)
                                };

                                foreach (var buildConf in builds)
                                {
                                    var taskItems = new List<AssetBundleBuild>() { buildConf.build };

                                    if (buildTextureBundle)
                                    {
                                        taskItems.Add(textureBuild);
                                    }

                                    BuildPipeline.BuildAssetBundles(
                                        outputFolder,
                                        taskItems.ToArray(),
                                        BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                                        buildConf.platform);

                                    buildArtifacts.Add((Path.Combine(outputFolder, buildConf.build.assetBundleName), buildConf.build.assetBundleName));
                                    buildArtifacts.Add((Path.Combine(outputFolder, buildConf.build.assetBundleName + ".manifest"), null));
                                    if (buildTextureBundle)
                                    {
                                        buildArtifacts.Add((Path.Combine(outputFolder, textureBuild.assetBundleName), textureBuild.assetBundleName));
                                        buildArtifacts.Add((Path.Combine(outputFolder, textureBuild.assetBundleName + ".manifest"), null));
                                    }
                                }
                            }

                            DirectoryInfo prefabDir = new DirectoryInfo(Path.Combine(sourcePath, entry.name));
                            var scripts = prefabDir.GetFiles("*.cs", SearchOption.AllDirectories).Select(script => script.FullName).ToArray();

                            string outputAssembly = null;
                            if (scripts.Length > 0)
                            {
                                outputAssembly = Path.Combine(outputFolder, $"{entry.name}.dll");
                                var assemblyBuilder = new AssemblyBuilder(outputAssembly, scripts);
                                assemblyBuilder.compilerOptions.AllowUnsafeCode = true;
                                assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;
                                assemblyBuilder.buildFinished += delegate (string assemblyPath, CompilerMessage[] compilerMessages)
                                {
                                    var errorCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                                    var warningCount = compilerMessages.Count(m => m.type == CompilerMessageType.Warning);

                                    Debug.Log($"Assembly build finished for {assemblyPath}");
                                    if (errorCount != 0)
                                    {
                                        Debug.Log($"Found {errorCount} errors");

                                        foreach (CompilerMessage message in compilerMessages)
                                        {
                                            if (message.type == CompilerMessageType.Error)
                                            {
                                                Debug.LogError(message.message);
                                                return;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        buildArtifacts.Add((outputAssembly, $"{entry.name}.dll"));
                                        buildArtifacts.Add((Path.Combine(outputFolder, $"{entry.name}.pdb"), null));
                                    }
                                };

                                // Start build of assembly
                                if (!assemblyBuilder.Build())
                                {
                                    Debug.LogErrorFormat("Failed to start build of assembly {0}!", assemblyBuilder.assemblyPath);
                                    return;
                                }

                                while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
                                {
                                    Thread.Sleep(0);
                                }
                            }

                            if (manifest.fmuName != "")
                            {
                                var fmuPathWindows = Path.Combine(sourcePath, manifest.assetName, manifest.fmuName, "binaries", "win64", $"{manifest.fmuName}.dll");
                                var fmuPathLinux = Path.Combine(sourcePath, manifest.assetName, manifest.fmuName, "binaries", "linux64", $"{manifest.fmuName}.so");
                                if (File.Exists(fmuPathWindows))
                                {
                                    buildArtifacts.Add((fmuPathWindows, $"{manifest.fmuName}_windows.dll"));
                                }

                                if (File.Exists(fmuPathLinux))
                                {
                                    buildArtifacts.Add((fmuPathLinux, $"{manifest.fmuName}_linux.so"));
                                }
                            }

                            if (manifest.attachments != null)
                            {
                                foreach (string key in manifest.attachments.Keys)
                                {
                                    if (key.Contains("pointcloud"))
                                    {
                                        foreach (FileInfo fi in new DirectoryInfo(manifest.attachments[key].ToString()).GetFiles())
                                        {
                                            if (fi.Extension == TreeUtility.IndexFileExtension || fi.Extension == TreeUtility.NodeFileExtension || fi.Extension == TreeUtility.MeshFileExtension)
                                            {
                                                persistentBuildArtifacts.Add((fi.FullName, Path.Combine(key, fi.Name)));
                                            }
                                        }
                                    }
                                }
                            }

                            if (bundleType == BundleConfig.BundleTypes.Sensor
                                && outputAssembly != null
                                && !mainAssetIsScript)
                            {
                                var asset = AssetDatabase.LoadAssetAtPath(entry.mainAssetFile, typeof(GameObject)) as GameObject;
                                SensorBase sensor = asset.GetComponent<SensorBase>();
                                if (sensor != null)
                                {
                                    manifest.sensorParams = new List<SensorParam>();
                                    foreach (SensorParam param in SensorTypes.GetConfig(sensor).Parameters)
                                    {
                                        manifest.sensorParams.Add(param);
                                    }
                                    asset = null;
                                }

                                manifest.supportedBridgeTypes = sensor.SupportedBridgeTypes.ToArray();

                                if(sensor.SupportedBridgeTypes.Count == 0)
                                {
                                    Debug.LogWarning($"{manifest.assetName} has no enumerated supported bridge types. Please make sure you list your supported bridge types.");
                                }

                                EditorUtility.UnloadUnusedAssetsImmediate();
                            }

                            if (bundleType == BundleConfig.BundleTypes.Bridge)
                            {
                                // gather information about bridge plugin

                                IBridgeFactory bridgeFactory = null;

                                System.Reflection.Assembly bridgesAssembly = null;
                                if (File.Exists(Path.Combine(BundleConfig.ExternalBase, "Bridges", entry.name, $"{manifest.assetName}.cs")))
                                {
                                    if (bridgesAssembly == null)
                                    {
                                        bridgesAssembly = System.Reflection.Assembly.Load("Simulator.Bridges");
                                    }

                                    foreach (Type ty in bridgesAssembly.GetTypes())
                                    {
                                        if (typeof(IBridgeFactory).IsAssignableFrom(ty) && !ty.IsAbstract)
                                        {
                                            bridgeFactory = Activator.CreateInstance(ty) as IBridgeFactory;
                                            BridgePlugins.Add(bridgeFactory);
                                        }
                                    }
                                }

                                foreach (var factoryType in BridgePlugins.GetBridgeFactories())
                                {
                                    if (BridgePlugins.GetNameFromFactory(factoryType) == entry.name)
                                    {
                                        bridgeFactory = (IBridgeFactory)Activator.CreateInstance(factoryType);
                                        break;
                                    }
                                }
                                if (bridgeFactory == null) { 
                                    throw new Exception($"Cannot find IBridgeFactory for {entry.name} bridge plugin");
                                }

                                var plugin = new BridgePlugin(bridgeFactory);
                                manifest.bridgeDataTypes = plugin.GetSupportedDataTypes();
                                manifest.bridgeType = bridgeFactory.GetType().GetCustomAttribute<BridgeNameAttribute>().Type;
                            }

                            var manifestOutput = Path.Combine(outputFolder, "manifest.json");
                            File.WriteAllText(manifestOutput, JsonConvert.SerializeObject(manifest));
                            buildArtifacts.Add((manifestOutput, "manifest.json"));

                            ZipFile archive = ZipFile.Create(Path.Combine(outputFolder, $"{thing}_{entry.name}"));
                            archive.BeginUpdate();
                            foreach (var file in buildArtifacts.Where(e => e.archiveName != null))
                                archive.Add(new StaticDiskDataSource(file.source), file.archiveName, CompressionMethod.Stored, true);

                            foreach (var file in persistentBuildArtifacts.Where(e => e.archiveName != null))
                                archive.Add(new StaticDiskDataSource(file.source), file.archiveName, CompressionMethod.Stored, true);

                            archive.CommitUpdate();
                            archive.Close();
                            BuildSuccessful = true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to build archive, exception follows:");
                            Debug.LogException(e);
                            BuildSuccessful = false;
                        }
                        finally
                        {
                            foreach (var file in buildArtifacts)
                            {
                                SilentDelete(file.source);
                                SilentDelete(file.source + ".meta");
                            }
                        }

                        Debug.Log("done");
                        Resources.UnloadUnusedAssets();
                    }

                    // these are an artifact of the asset building pipeline and we don't use them
                    SilentDelete(Path.Combine(outputFolder, Path.GetFileName(outputFolder)));
                    SilentDelete(Path.Combine(outputFolder, Path.GetFileName(outputFolder)) + ".manifest");
                }
                finally
                {
                    // Load back previously opened scenes
                    var mainScenePath = string.IsNullOrEmpty(activeScenePath) ? loaderScenePath : activeScenePath;
                    EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
                    foreach (var scenePath in openScenePaths)
                    {
                        if (string.Equals(scenePath, activeScenePath) || string.IsNullOrEmpty(scenePath))
                            continue;

                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    }
                }
            }
        }
        #endregion

        #region asmdef
        class AsmdefBody
        {
            public string name;
            public string[] references;
            // TODO: This will enable 'unsafe' code for all.
            // We may find a better way to unable it only when necessary.
            public bool allowUnsafeCode = true;
        }
        #endregion

        [MenuItem("Simulator/Build...", false, 30)]
        private static void ShowWindow()
        {
            var window = GetWindow<Build>();
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                window.Target = BuildTarget.Windows;
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                window.Target = BuildTarget.Linux;
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                window.Target = BuildTarget.MacOS;
            }

            var data = EditorPrefs.GetString("Simulator/Build", JsonUtility.ToJson(window, false));
            JsonUtility.FromJsonOverwrite(data, window);
            window.titleContent = new GUIContent("Build Maps & Vehicles");

            window.Show();
        }

        private void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("Simulator/Build", data);
        }

        private void OnGUI()
        {
            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
            foreach (var group in BuildGroups.Values)
            {
                group.isOpen = EditorGUILayout.Foldout(group.isOpen, $"{group.bundlePath}");
                if (group.isOpen)
                {
                    group.OnGUI();
                }
            }
            EditorGUILayout.EndScrollView();
            GUILayout.Space(10);
            GUILayout.Label("Binary Options (Not bundles)", EditorStyles.boldLabel);

            Target = (BuildTarget)EditorGUILayout.EnumPopup("Executable Platform:", Target);

            EditorGUILayout.HelpBox("Select Folder to Save...", MessageType.None, true);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            BuildPlayer = GUILayout.Toggle(BuildPlayer, "Build Simulator:", GUILayout.ExpandWidth(false));

            #region unity 2020.3.3f1 fix
            // TODO fix for issues with unity 2020.3.3f1 multiple bundles
            if (BuildPlayer)
            {
                foreach (var group in BuildGroups.Values)
                {
                    foreach (var entry in group.entries.Values)
                    {
                        entry.selected = false;
                    }
                }
                HelpMsg = "Bundle selection disabled when binary selected";
            }
            else
            {
                HelpMsg = $"One bundle can be built at a time: {CurrentSelectedEntryName} selected";
            }
            #endregion

            EditorGUI.BeginDisabledGroup(!BuildPlayer);
            PlayerFolder = GUILayout.TextField(PlayerFolder);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var folder = EditorUtility.SaveFolderPanel("Select Folder", PlayerFolder, string.Empty);
                if (!string.IsNullOrEmpty(folder))
                {
                    PlayerFolder = Path.GetFullPath(folder);
                }
            }
            EditorGUILayout.EndHorizontal();

            DevelopmentPlayer = GUILayout.Toggle(DevelopmentPlayer, "Development Build");
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.HelpBox(HelpMsg, MessageType.Info, true);

            if (GUILayout.Button("Build"))
            {
                void OnComplete()
                {
                    Running = false;
                    if(BuildSuccessful && !BuildPlayer)
                    {
                        BuildCompletePopup.Init();
                    }
                }

                Running = true;
                try
                {
                    var assetBundlesLocation = Path.Combine(Application.dataPath, "..", "AssetBundles");
                    if (BuildPlayer)
                    {
                        if (string.IsNullOrEmpty(PlayerFolder))
                        {
                            Debug.LogError("Please specify simulator build folder!");
                            return;
                        }
                        RunPlayerBuild(Target, PlayerFolder, DevelopmentPlayer);

                        assetBundlesLocation = Path.Combine(PlayerFolder, "AssetBundles");
                    }

                    BuildQueued = true;
                    EditorApplication.delayCall += () => BuildBundles(assetBundlesLocation, OnComplete);
                }
                finally
                {
                    if (!BuildQueued)
                        OnComplete();
                }
            }
        }

        private void Refresh()
        {
            if (BuildGroups == null)
            {
                BuildGroups = new Dictionary<string, BundleData>();
                var data = new BundleData(BundleConfig.BundleTypes.Environment);
                BuildGroups.Add(data.bundlePath, data);
                data = new BundleData(BundleConfig.BundleTypes.Vehicle);
                BuildGroups.Add(data.bundlePath, data);
                data = new BundleData(BundleConfig.BundleTypes.Sensor);
                BuildGroups.Add(data.bundlePath, data);
                data = new BundleData(BundleConfig.BundleTypes.Controllable);
                BuildGroups.Add(data.bundlePath, data);
                data = new BundleData(BundleConfig.BundleTypes.Bridge);
                BuildGroups.Add(data.bundlePath, data);
            }

            BuildGroups = BuildGroups.Where(g => Directory.Exists(g.Value.sourcePath)).ToDictionary(e => e.Key, e => e.Value);
            foreach (var NPCDir in Directory.EnumerateDirectories(Path.Combine(BundleConfig.ExternalBase, BundleConfig.pluralOf(BundleConfig.BundleTypes.NPC))))
            {
                var bundlePath = NPCDir.Substring(BundleConfig.ExternalBase.Length + 1);

                // Ignore temp folders created by Jenkins
                if (bundlePath.EndsWith("@tmp"))
                {
                    continue;
                }

                if (!BuildGroups.ContainsKey(bundlePath))
                {
                    var data = new BundleData(BundleConfig.BundleTypes.NPC, bundlePath);
                    BuildGroups.Add(data.bundlePath, data);
                }
            }

            foreach (var PedDir in Directory.EnumerateDirectories(Path.Combine(BundleConfig.ExternalBase, BundleConfig.pluralOf(BundleConfig.BundleTypes.Pedestrian))))
            {
                var bundlePath = PedDir.Substring(BundleConfig.ExternalBase.Length + 1);

                // Ignore temp folders created by Jenkins
                if (bundlePath.EndsWith("@tmp"))
                {
                    continue;
                }

                if (!BuildGroups.ContainsKey(bundlePath))
                {
                    var data = new BundleData(BundleConfig.BundleTypes.Pedestrian, bundlePath);
                    BuildGroups.Add(data.bundlePath, data);
                }
            }

            foreach (var group in BuildGroups.Values)
            {
                group.Refresh();
            }
        }

        private void OnFocus()
        {
            Refresh();
        }

        public static string ZipPath(params string[] elements)
        {
            return string.Join(Path.AltDirectorySeparatorChar.ToString(), elements);
        }

        static string GetFileSizeAsString(string url)
        {
            try
            {
                var req = WebRequest.Create(url);
                req.Method = "HEAD";
                using (var resp = req.GetResponse())
                {
                    if (long.TryParse(resp.Headers.Get("Content-Length"), out long size))
                    {
                        return EditorUtility.FormatBytes(size);
                    }
                }
            }
            catch
            {
                // ignore failed
            }
            return "unknown";
        }

        static void RunPlayerBuild(BuildTarget target, string folder, bool development)
        {
            var oldGraphicsJobSetting = PlayerSettings.graphicsJobs;
            try
            {
                UnityEditor.BuildTarget buildTarget;
                if (target == BuildTarget.Windows)
                {
                    buildTarget = UnityEditor.BuildTarget.StandaloneWindows64;
                    PlayerSettings.graphicsJobs = true;
                }
                else if (target == BuildTarget.Linux)
                {
                    buildTarget = UnityEditor.BuildTarget.StandaloneLinux64;
                    PlayerSettings.graphicsJobs = false;
                }
                else if (target == BuildTarget.MacOS)
                {
                    buildTarget = UnityEditor.BuildTarget.StandaloneOSX;
                    PlayerSettings.graphicsJobs = false;
                }
                else
                {
                    throw new Exception($"Unsupported build target {target}");
                }

                string location;
                if (target == BuildTarget.Linux)
                {
                    location = Path.Combine(folder, "simulator");
                }
                else if (target == BuildTarget.Windows)
                {
                    location = Path.Combine(folder, "simulator.exe");
                }
                else if (target == BuildTarget.MacOS)
                {
                    location = Path.Combine(folder, "simulator");
                }
                else
                {
                    Debug.LogError($"Target {target} is not supported");
                    return;
                }

                var build = new BuildPlayerOptions()
                {
                    scenes = new[] { "Assets/Scenes/LoaderScene.unity", "Assets/Scenes/ScenarioEditor.unity" },
                    locationPathName = location,
                    targetGroup = BuildTargetGroup.Standalone,
                    target = buildTarget,
                    options = BuildOptions.CompressWithLz4 | BuildOptions.StrictMode,
                };

                if (development)
                {
                    build.options |= BuildOptions.Development;
                }

                var r = BuildPipeline.BuildPlayer(build);
                if (r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    Debug.Log("Player build succeeded!");
                }
                else
                {
                    Debug.LogError($"Player build result: {r.summary.result}!");
                    throw new Exception($"Player build result: {r.summary.result}!");
                }

            }
            finally
            {
                PlayerSettings.graphicsJobs = oldGraphicsJobSetting;
            }
        }

        private void BuildBundles(string outputFolder, Action onComplete = null)
        {
            try
            {
                foreach (var group in BuildGroups.Values)
                {
                    group.RunBuild(outputFolder);
                }
            }
            finally
            {
                BuildQueued = false;
                onComplete?.Invoke();
            }
        }

        // Called from command line
        private static void Run()
        {
            var hasQuitArg = Environment.GetCommandLineArgs().Contains("-quit");

            try {
                RunImpl();
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
            finally {
                if (!hasQuitArg)
                {
                    EditorApplication.Exit(0);
                }
            }
        }

        private static void RunImpl()
        {
            BuildTarget? buildTarget = null;

            string buildPlayer = null;
            bool buildBundles = false;

            bool developmentBuild = false;

            Build build = new Build();
            build.Refresh();

            var buildBundleParam = new Regex("^-build(Environment|Vehicle|Sensor|Controllable|NPC|Bridge|Pedestrian)s$");
            int bundleSum = 0;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-buildTarget")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        if (args[i] == "Win64")
                        {
                            buildTarget = BuildTarget.Windows;
                        }
                        else if (args[i] == "Linux64")
                        {
                            buildTarget = BuildTarget.Linux;
                        }
                        else if (args[i] == "OSXUniversal")
                        {
                            buildTarget = BuildTarget.MacOS;
                        }
                        else
                        {
                            throw new Exception($"Unsupported '{args[i]}' build target!");
                        }
                    }
                    else
                    {
                        throw new Exception("-buildTarget expects Win64 or Linux64 argument!");
                    }
                }
                else if (args[i] == "-buildPlayer")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        buildPlayer = args[i];
                    }
                    else
                    {
                        throw new Exception("-buildPlayer expects output folder!");
                    }
                }
                else if (args[i] == "-buildBundles")
                {
                    buildBundles = true;
                }
                else if (args[i] == "-developmentBuild")
                {
                    developmentBuild = true;
                }
                else
                {
                    Match match = buildBundleParam.Match(args[i]);
                    if (match.Success)
                    {
                        var val = match.Groups[1].Captures[0].Value;
                        var bundleType = (BundleConfig.BundleTypes) Enum.Parse(typeof(BundleConfig.BundleTypes), val);
                        if (i == args.Length - 1)
                        {
                            throw new Exception($"-build{val} expects comma seperated environment names!");
                        }

                        var bundleGroups = BuildGroups.Values.Where(g => g.bundleType == bundleType);
                        i++;
                        foreach (var name in args[i].Split(','))
                        {
                            bool isAssetEnabled = false;

                            foreach (var buildGroup in bundleGroups)
                            {
                                if (name == "all")
                                {
                                    foreach (var entry in buildGroup.entries.Values)
                                    {
                                        isAssetEnabled = true;
                                        entry.selected = true;
                                        bundleSum++;

                                        Debug.Log($"[BUILD] Enable {bundleType} entry '{entry.name}'");
                                    }
                                }
                                else
                                {
                                    if (buildGroup.EnableByName(name) == true) {
                                        bundleSum++;
                                        isAssetEnabled = true;
                                    }
                                }
                            }

                            if (!isAssetEnabled) {
                                throw new Exception($"could not enable entry {name} of {bundleType}");
                            }
                        }
                    }
                }
            }

            if (!buildTarget.HasValue && !string.IsNullOrEmpty(buildPlayer))
            {
                throw new Exception("-buildTarget not specified!");
            }

            if (buildBundles && bundleSum == 0)
            {
                throw new Exception($"No environments, vehicles, sensors, controllables or bridges to build");
            }

            Running = true;

            try
            {
                if (!string.IsNullOrEmpty(buildPlayer))
                {
                    RunPlayerBuild(buildTarget.Value, buildPlayer, developmentBuild);
                }

                if (buildBundles)
                {
                    var assetBundlesLocation = Path.Combine(Application.dataPath, "..", "AssetBundles");
                    build.BuildBundles(assetBundlesLocation);
                }
            }
            finally
            {
                Running = false;
            }
        }

        private static void SilentDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }
        }
    }

    public class BuildCompletePopup : EditorWindow
    {
        public static void Init()
        {
            BuildCompletePopup window = ScriptableObject.CreateInstance<BuildCompletePopup>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
            window.ShowPopup();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField($"Would you like to go to {Web.Config.CloudUrl} to upload built asset?", EditorStyles.wordWrappedLabel);
            GUILayout.Space(30);
            if (GUILayout.Button("Yes"))
            {
                Application.OpenURL(Web.Config.CloudUrl);
                this.Close();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("No"))
            {
                this.Close();
            }
        }
    }
}
