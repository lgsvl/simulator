/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ICSharpCode.SharpZipLib.Zip;
using Simulator.Map;
using YamlDotNet.Serialization;
using System.Net;
using UnityEditor.Compilation;
using Simulator.Controllable;
using System.Reflection;
using System.Text;
using Simulator.FMU;
using Simulator.PointCloud.Trees;
using System.Text.RegularExpressions;
using System.Threading;
using Simulator.Bridge;

namespace Simulator.Editor
{
    public class Build : EditorWindow
    {
        static public bool Running;

        enum BuildTarget
        {
            Windows,
            Linux,
            MacOS,
        }

        class AsmdefBody
        {
            public string name;
            public string[] references;
            // TODO: This will enable 'unsafe' code for all.
            // We may find a better way to unable it only when necessary.
            public bool allowUnsafeCode = true;
        }

        public const string SceneExtension = "unity";
        public const string ScriptExtension = "cs";
        public const string PrefabExtension = "prefab";

        class BundleData
        {
            public BundleData(BundleConfig.BundleTypes type, string path = null)
            {
                bundleType = type;
                bundlePath = path ?? BundleConfig.pluralOf(type);
            }
            public Vector2 scroll;
            public BundleConfig.BundleTypes bundleType;
            public string bundlePath;
            public string sourcePath => Path.Combine(BundleConfig.ExternalBase, bundlePath);
            public class Entry
            {
                public string name;
                public string mainAssetFile;
                public bool selected;
                public bool available;
            }

            public Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
            public void OnGUI()
            {
                string header = bundlePath;
                GUILayout.Label(header, EditorStyles.boldLabel);
                if (entries.Count == 0)
                {
                    EditorGUILayout.HelpBox($"No {bundlePath} are available", UnityEditor.MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Following {bundlePath} were automatically detected:", UnityEditor.MessageType.None);
                }
                scroll = EditorGUILayout.BeginScrollView(scroll);

                if (entries.Count != 0)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
                    if (GUILayout.Button("Select All", GUILayout.ExpandWidth(false)))
                    {
                        foreach (var entry in entries)
                        {
                            entry.Value.selected = true;
                        }
                    }
                    if (GUILayout.Button("Select None", GUILayout.ExpandWidth(false)))
                    {
                        foreach (var entry in entries)
                        {
                            entry.Value.selected = false;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                foreach (var entry in entries.OrderBy(entry => entry.Key))
                {
                    if (entry.Value.available)
                    {
                        entry.Value.selected = GUILayout.Toggle(entry.Value.selected, entry.Key);
                    }
                    else
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        GUILayout.Toggle(false, $"{entry.Key} (missing items/{entry.Value.mainAssetFile}");
                        EditorGUI.EndDisabledGroup();
                    }
                }

                EditorGUILayout.EndScrollView();
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
                entries = entries.Where(entry => updated.Contains(entry.Key)).ToDictionary(p=>p.Key, p=>p.Value);
            }

            public void EnableByName(string name)
            {
                if (!entries.ContainsKey(name))
                {
                    var knownKeys = string.Join(",", entries.Keys);
                    throw new Exception($"could not enable entry {name} as it was not found. Known entirs of {bundlePath} are {knownKeys}");
                }
                entries[name].selected = true;
            }

            Manifest PreparePrefabManifest(Entry prefabEntry)
            {
                string assetGuid = Guid.NewGuid().ToString();
                Manifest manifest = new Manifest
                {
                    assetName = prefabEntry.name,
                    assetGuid = assetGuid,
                    bundleFormat = BundleConfig.Versions[bundleType],
                    description = "",
                    licenseName = "",
                    authorName = "",
                    authorUrl = "",
                    fmuName = "",
                    bridgeDataTypes = Array.Empty<string>(),
                };

                if (bundleType == BundleConfig.BundleTypes.Vehicle)
                {
                    var info = AssetDatabase.LoadAssetAtPath<GameObject>(prefabEntry.mainAssetFile).GetComponent<VehicleInfo>();
                    var fmu = AssetDatabase.LoadAssetAtPath<GameObject>(prefabEntry.mainAssetFile).GetComponent<VehicleFMU>();
                    var baseLink = AssetDatabase.LoadAssetAtPath<GameObject>(prefabEntry.mainAssetFile).GetComponent<BaseLink>();

                    if (info == null)
                    {
                        throw new Exception($"Build failed: Vehicle info on {prefabEntry.mainAssetFile} not found. Please add a VehicleInfo component and rebuild.");
                    }
                    manifest.licenseName = info.LicenseName;
                    manifest.description = info.Description;
                    manifest.fmuName = fmu == null ? "" : fmu.FMUData.Name;
                    manifest.baseLink = baseLink != null ?
                        new double[] { baseLink.transform.position.x, baseLink.transform.position.y, baseLink.transform.position.z } : // rotation
                        new double[] { 0, 0, 0 };
                }
                return manifest;
            }

            Manifest PrepareSceneManifest(Entry sceneEntry, HashSet<Scene> currentScenes)
            {
                Scene scene = EditorSceneManager.OpenScene(sceneEntry.mainAssetFile, OpenSceneMode.Additive);
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
                            var manifest = new Manifest
                            {
                                assetName = sceneEntry.name,
                                assetGuid = Guid.NewGuid().ToString(),
                                bundleFormat = BundleConfig.Versions[BundleConfig.BundleTypes.Environment],
                                description = origin.Description,
                                licenseName = origin.LicenseName,
                                authorName = "",
                                authorUrl = "",
                                fmuName = "",
                            };
                            manifest.additionalFiles = new Dictionary<string, string>();
                            foreach (Tuple<string, string> t in loaderPaths)
                            {
                                if (!manifest.additionalFiles.ContainsKey($"pointcloud_{t.Item1}"))
                                {
                                    manifest.additionalFiles.Add($"pointcloud_{t.Item1}", t.Item2);
                                }
                            }

                            return manifest;
                        }
                    }
                    throw new Exception($"Build failed: MapOrigin on {sceneEntry.name} not found. Please add a MapOrigin component.");
                }
                finally
                {
                    if (!currentScenes.Contains(scene))
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
            public void RunBuild(string outputFolder)
            {
                string Thing = Enum.GetName(typeof(BundleConfig.BundleTypes), bundleType);
                string Things = BundleConfig.pluralOf(bundleType);
                string thing = Thing.ToLower();

                outputFolder = Path.Combine(outputFolder, bundlePath);
                Directory.CreateDirectory(outputFolder);
                var currentScenes = new HashSet<Scene>();

                var selected = entries.Values.Where(e => e.selected && e.available).ToList();
                if (selected.Count == 0) return;

                if (bundleType == BundleConfig.BundleTypes.Environment)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        Debug.LogWarning("Cancelling the build.");
                        return;
                    }
                    for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
                    {
                        currentScenes.Add(EditorSceneManager.GetSceneAt(i));
                    }
                }

                foreach (var entry in selected)
                {
                    Manifest manifest;
                    if (bundleType == BundleConfig.BundleTypes.Environment)
                    {
                        manifest = PrepareSceneManifest(entry, currentScenes);
                    }
                    else
                    {
                        manifest = PreparePrefabManifest(entry);
                    }

                    var asmDefPath = Path.Combine(BundleConfig.ExternalBase, Things, $"Simulator.{Things}.asmdef");
                    AsmdefBody asmDef = null;
                    if (File.Exists(asmDefPath))
                    {
                        asmDef = JsonUtility.FromJson<AsmdefBody>(File.ReadAllText(asmDefPath));
                    }


                    var buildArtifacts = new List<(string source, string archiveName)>();
                    bool mainAssetIsScript = entry.mainAssetFile.EndsWith("."+ScriptExtension);
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
                        if (!mainAssetIsScript)
                        {
                            var textureBuild = new AssetBundleBuild()
                            {
                                assetBundleName = $"{manifest.assetGuid}_{thing}_textures",
                                assetNames = AssetDatabase.GetDependencies(entry.mainAssetFile).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg")).ToArray()
                            };

                            bool buildTextureBundle = textureBuild.assetNames.Length > 0;

                            var windowsBuild = new AssetBundleBuild()
                            {
                                assetBundleName = $"{manifest.assetGuid}_{thing}_main_windows",
                                assetNames = new[] { entry.mainAssetFile },
                            };

                            var linuxBuild = new AssetBundleBuild()
                            {
                                assetBundleName = $"{manifest.assetGuid}_{thing}_main_linux",
                                assetNames = new[] { entry.mainAssetFile },
                            };

                            var builds = new[]
                            {
                                (build: linuxBuild,     platform: UnityEditor.BuildTarget.StandaloneLinux64),
                                (build: windowsBuild,   platform: UnityEditor.BuildTarget.StandaloneWindows64)
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

                        if (scripts.Length > 0)
                        {
                            var outputAssembly = Path.Combine(outputFolder, $"{entry.name}.dll");
                            var assemblyBuilder = new AssemblyBuilder(outputAssembly, scripts);
                            assemblyBuilder.compilerOptions.AllowUnsafeCode = true;

                            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                            var modules = assemblies.Where(asm =>
                                                            asm.GetName().Name == "UnityEngine" ||
                                                            asm.GetName().Name == "UnityEngine.JSONSerializeModule" ||
                                                            asm.GetName().Name == "UnityEngine.CoreModule" ||
                                                            asm.GetName().Name == "UnityEngine.PhysicsModule").ToArray();

                            assemblyBuilder.additionalReferences = modules.Select(a => a.Location).ToArray();

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
                                Thread.Sleep(1);
                            }
                        }

                        if (manifest.fmuName != "")
                        {
                            var fmuPathWindows = Path.Combine(sourcePath, manifest.fmuName, "binaries", "win64", $"{manifest.fmuName}.dll");
                            var fmuPathLinux = Path.Combine(sourcePath, manifest.fmuName, "binaries", "linux64", $"{manifest.fmuName}.so");
                            if (File.Exists(fmuPathWindows))
                            {
                                buildArtifacts.Add((fmuPathWindows, $"{manifest.fmuName}_windows.dll"));
                            }
                            if (File.Exists(fmuPathLinux))
                            {
                                buildArtifacts.Add((fmuPathLinux, $"{manifest.fmuName}_linux.so"));
                            }
                        }
                        if (manifest.additionalFiles != null)
                        {
                            foreach (string key in manifest.additionalFiles.Keys)
                            {
                                if (key.Contains("pointcloud"))
                                {
                                    foreach (FileInfo fi in new DirectoryInfo(manifest.additionalFiles[key]).GetFiles())
                                    {
                                        if (fi.Extension == TreeUtility.IndexFileExtension || fi.Extension == TreeUtility.NodeFileExtension || fi.Extension == TreeUtility.MeshFileExtension)
                                        {
                                            buildArtifacts.Add((fi.FullName, Path.Combine(key, fi.Name)));
                                        }
                                    }
                                }
                            }
                        }

                        if (bundleType == BundleConfig.BundleTypes.Bridge)
                        {
                            // gather information about bridge plugin

                            IBridgeFactory bridgeFactory = null;
                            foreach (var factoryType in BridgePlugins.GetBridgeFactories())
                            {
                                if (BridgePlugins.GetNameFromFactory(factoryType) == entry.name)
                                {
                                    bridgeFactory = (IBridgeFactory)Activator.CreateInstance(factoryType);
                                    break;
                                }
                            }
                            if (bridgeFactory == null)
                            {
                                throw new Exception($"Cannot find IBridgeFactory for {entry.name} bridge plugin");
                            }

                            var plugin = new BridgePlugin(bridgeFactory);
                            manifest.bridgeDataTypes = plugin.GetSupportedDataTypes();
                        }

                        var manifestOutput = Path.Combine(outputFolder, "manifest");
                        File.WriteAllText(manifestOutput, new Serializer().Serialize(manifest));
                        buildArtifacts.Add((manifestOutput, "manifest"));

                        ZipFile archive = ZipFile.Create(Path.Combine(outputFolder, $"{thing}_{entry.name}"));
                        archive.BeginUpdate();
                        foreach(var file in buildArtifacts.Where(e => e.archiveName != null))
                        {
                            archive.Add(new StaticDiskDataSource(file.source), file.archiveName, CompressionMethod.Stored, true);
                        }
                        archive.CommitUpdate();
                        archive.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Failed to build archive: {e.Message} {e.StackTrace}");
                    }
                    finally
                    {
                        foreach(var file in buildArtifacts) 
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
                SilentDelete(Path.Combine(outputFolder, Path.GetFileName(outputFolder))+".manifest");
            }
        }

        Dictionary<string, BundleData> buildGroups;

        [SerializeField] BuildTarget Target;
        [SerializeField] bool BuildPlayer = false;
        [SerializeField] string PlayerFolder = string.Empty;
        [SerializeField] bool DevelopmentPlayer = false;

        [MenuItem("Simulator/Build...", false, 30)]
        static void ShowWindow()
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

        void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("Simulator/Build", data);
        }

        void OnGUI()
        {
            foreach (var group in buildGroups.Values)
            {
                group.OnGUI();   
            }

            GUILayout.Label("Options", EditorStyles.boldLabel);

            Target = (BuildTarget)EditorGUILayout.EnumPopup("Executable Platform:", Target);

            EditorGUILayout.HelpBox("Select Folder to Save...", MessageType.Info);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            BuildPlayer = GUILayout.Toggle(BuildPlayer, "Build Simulator:", GUILayout.ExpandWidth(false));

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

            if (GUILayout.Button("Build"))
            {
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
                    foreach (var group in buildGroups)
                    {
                        group.Value.RunBuild(assetBundlesLocation);
                    }
                }
                finally
                {
                    Running = false;
                }
            }
        }

        void Refresh()
        {
            if (buildGroups == null)
            {
                buildGroups = new Dictionary<string, BundleData>();
                var data = new BundleData(BundleConfig.BundleTypes.Environment);
                buildGroups.Add(data.bundlePath, data);
                data = new BundleData(BundleConfig.BundleTypes.Vehicle);
                buildGroups.Add(data.bundlePath, data);
                data = new BundleData(BundleConfig.BundleTypes.Sensor);
                buildGroups.Add(data.bundlePath, data);
                data = new BundleData(BundleConfig.BundleTypes.Controllable);
                buildGroups.Add(data.bundlePath, data);
                data = new BundleData(BundleConfig.BundleTypes.Bridge);
                buildGroups.Add(data.bundlePath, data);
            }


            buildGroups = buildGroups.Where(g => Directory.Exists(g.Value.sourcePath)).ToDictionary(e => e.Key, e => e.Value);
            foreach (var NPCDir in Directory.EnumerateDirectories(Path.Combine(BundleConfig.ExternalBase, BundleConfig.pluralOf(BundleConfig.BundleTypes.NPC))))
            {
                var bundlePath = NPCDir.Substring(BundleConfig.ExternalBase.Length + 1);

                // Ignore temp folders created by Jenkins
                if (bundlePath.EndsWith("@tmp"))
                {
                    continue;
                }

                if (!buildGroups.ContainsKey(bundlePath))
                {
                    var data = new BundleData(BundleConfig.BundleTypes.NPC, bundlePath);
                    buildGroups.Add(data.bundlePath, data);
                }
            }

            foreach (var group in buildGroups.Values)
            {
                group.Refresh();
            }
        }

        void OnFocus()
        {
            Refresh();
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

        static void SaveBundleLinks(string filename)
        {
            var gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");
            var gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
            var gitTag = Environment.GetEnvironmentVariable("GIT_TAG");
            var downloadHost = Environment.GetEnvironmentVariable("S3_DOWNLOAD_HOST");

            if (string.IsNullOrEmpty(gitCommit))
            {
                Debug.LogError("Cannot save bundle links - GIT_COMMIT is not set");
                return;
            }

            if (string.IsNullOrEmpty(downloadHost))
            {
                Debug.LogError("Cannot save bundle links - S3_DOWNLOAD_HOST is not set");
                return;
            }

            using (var f = File.CreateText(filename))
            {
                f.WriteLine("<html><body>");

                var dt = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);

                f.WriteLine("<h1>Info</h1>");
                f.WriteLine("<ul>");
                f.WriteLine($"<li>Build Date: {dt}</li>");
                if (!string.IsNullOrEmpty(gitCommit))
                {
                    f.WriteLine($"<li>Git Commit: {gitCommit}</li>");
                }
                if (!string.IsNullOrEmpty(gitBranch))
                {
                    f.WriteLine($"<li>Git Branch: {gitBranch}</li>");
                }
                if (!string.IsNullOrEmpty(gitTag))
                {
                    f.WriteLine($"<li>Git Tag: {gitTag}</li>");
                }
                f.WriteLine("</ul>");

                f.WriteLine("<h1>Environments</h1>");
                f.WriteLine("<ul>");

                var simEnvironments = Environment.GetEnvironmentVariable("SIM_ENVIRONMENTS");
                if (!string.IsNullOrEmpty(simEnvironments))
                {
                    foreach (var line in simEnvironments.Split('\n'))
                    {
                        var items = line.Split(new[] { ' ' }, 2);
                        var id = items[0];
                        var name = items[1];

                        var url = $"https://{downloadHost}/{id}/environment_{name}";
                        var size = GetFileSizeAsString(url);
                        f.WriteLine($"<li><a href='{url}'>{name}</a> ({size})</li>");
                    }
                }
                f.WriteLine("</ul>");

                f.WriteLine("<h1>Vehicles</h1>");
                f.WriteLine("<ul>");

                var simVehicles = Environment.GetEnvironmentVariable("SIM_VEHICLES");
                if (!string.IsNullOrEmpty(simVehicles))
                {
                    foreach (var line in simVehicles.Split('\n'))
                    {
                        var items = line.Split(new[] { ' ' }, 2);
                        var id = items[0];
                        var name = items[1];

                        var url = $"https://{downloadHost}/{id}/vehicle_{name}";
                        var size = GetFileSizeAsString(url);
                        f.WriteLine($"<li><a href='{url}'>{name}</a> ({size})</li>");
                    }
                }
                f.WriteLine("</ul>");

                // TODO api objects

                f.WriteLine("</body></html>");
            }
        }

        static void RunPlayerBuild(BuildTarget target, string folder, bool development)
        {
            // TODO: this is temporary until we learn how to build WebUI output directly in Web folder
            var webui = Path.Combine(Application.dataPath, "..", "WebUI", "dist");
            if (!File.Exists(Path.Combine(webui, "index.html")))
            {
                throw new Exception($"WebUI files are missing! Please build WebUI at least once before building Player");
            }

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
                    scenes = new[] { "Assets/Scenes/LoaderScene.unity" },
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
                    // TODO: this is temporary until we learn how to build WebUI output directly in Web folder
                    var webFolder = target == BuildTarget.MacOS ? Path.Combine(folder, "simulator.app") : folder;
                    var web = Path.Combine(webFolder, "Web");
                    Directory.CreateDirectory(web);

                    var files = new[] { "index.html", "main.css", "main.js", "favicon.png" };
                    foreach (var file in files)
                    {
                        File.Copy(Path.Combine(webui, file), Path.Combine(web, file), true);
                    }

                    Debug.Log("Player build succeeded!");
                }
                else
                {
                    Debug.LogError($"Player build result: {r.summary.result}!");
                }

            }
            finally
            {
                PlayerSettings.graphicsJobs = oldGraphicsJobSetting;
            }
        }

        static void Run()
        {
            BuildTarget? buildTarget = null;

            string buildPlayer = null;
            bool buildBundles = false;
            string saveBundleLinks = null;

            bool developmentBuild = false;

            Build build = new Build();
            build.Refresh();

            var buildBundleParam = new Regex("^-build(Environment|Vehicle|Sensor|Controllable|NPC|Bridge)s$");
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
                else if (args[i] == "-saveBundleLinks")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        saveBundleLinks = args[i];
                    }
                    else
                    {
                        throw new Exception("-saveBundleLinks expects output filename!");
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

                        var bundleGroups = build.buildGroups.Values.Where(g => g.bundleType == bundleType);
                        i++;
                        foreach (var name in args[i].Split(','))
                        {
                            foreach (var buildGroup in bundleGroups)
                            {
                                if (name == "all")
                                {
                                    foreach (var entry in buildGroup.entries.Values)
                                    {
                                        entry.selected = true;
                                        bundleSum++;
                                    }
                                }
                                else
                                {
                                    buildGroup.EnableByName(name);
                                    bundleSum++;
                                }
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
                    foreach (var group in build.buildGroups.Values)
                    {
                        group.RunBuild(assetBundlesLocation);
                    }
                }

                if (saveBundleLinks != null)
                {
                    SaveBundleLinks(saveBundleLinks);
                }
            }
            finally
            {
                Running = false;
            }
        }

        static void SilentDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
