/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Simulator.Editor
{
    class Checker
    {
        static readonly Dictionary<string, string[]> UnityFolders = new Dictionary<string, string[]>()
        {
            // folder => allowed extensions
            { "Animations", new [] { ".controller", ".anim", ".playable" } },
            { "Audio", new [] { ".wav", ".mp3" } },
            { "Effects", new [] { ".png" } },
            { "GlobalSettings", new [] { ".asset" } },
            { "Materials", new [] { ".mat", ".png", ".jpg", ".asset" } },
            { "Models", new [] { ".fbx", ".st" } },
            { "Physics", new [] { ".physicMaterial" } },
            { "Pefabs", new [] { ".prefab" } },
            { "Resources", new [] { ".txt", ".prefab", ".asset" } }, // TODO: does this need prefab?
            { "ScriptableObjects", new [] { ".asset" } },
            { "Scenes", new [] { ".unity" } },
            { "Shaders", new [] { ".shader", ".hlsl", ".shadergraph", ".shadersubgraph", ".compute" } },
            { "Textures", new [] { ".png", ".exr", ".jpg" } },
        };

        public enum Category
        {
            Info,
            Warning,
            Error,
        };

        string BaseFolder;
        Action<Category, string> Log;

        public Checker(string baseFolder, Action<Category, string> log)
        {
            BaseFolder = baseFolder;
            Log = log;
        }

        public void Run()
        {
            Log(Category.Info, "Checking...");

            var rootFolders = new[]
            {
                // allowed or generated folders
                "AssetBundles",
                "Docs",
                "Docker",
                "Jenkins",
                "Library",
                "obj",
                "Temp",
                "Web",
            };

            var rootFoldersReq = new[]
            {
                // required folders
                "Assets",
                "Packages",
                "ProjectSettings",
                "WebUI",
            };

            var rootPath = Path.Combine(Application.dataPath, "..");

            CheckFolders("/", rootPath, rootFolders, rootFoldersReq, false);
            CheckExtensions("/ProjectSettings", Path.Combine(rootPath, "ProjectSettings"), new[] { ".asset", ".txt" });

            CheckAssets(Path.Combine(rootPath, "Assets"));
            CheckSpaces("", rootPath);

            if (!string.IsNullOrEmpty(BaseFolder))
            {
                CheckMainDependencies("Assets/Scenes/LoaderScene.unity");
            }

            Log(Category.Info, "Done!");
        }

        void CheckAssets(string assetsFolder)
        {
            var assetFolders = new[]
            {
                // allowed folders
                "Animations",
                "Audio",
                "Editor",
                "Effects",
                "GlobalSettings",
                "Materials",
                "Models",
                "Physics",
                "Plugins",
                "Prefabs",
                "Resources",
                "ScriptableObjects",
                "Scenes",
                "Settings",
                "Shaders",
                "Textures",
                "UI",
            };

            var assetFoldersReq = new[]
            {
                // required folders
                "External",
                "Scripts",
                "Tests",
            };

            var assetFiles = new[]
            {
                // allowed files
                "csc.rsp",
            };

            CheckFolders("/Assets", assetsFolder, assetFolders, assetFoldersReq, true);
            CheckFiles("/Assets", assetsFolder, assetFiles, Array.Empty<string>(), true);

            CheckScripts("/Assets/Scripts", Path.Combine(assetsFolder, "Scripts"));
            CheckScripts("/Assets/Tests", Path.Combine(assetsFolder, "Tests"));

            CheckPlugins("/Assets/Plugins", Path.Combine(assetsFolder, "Plugins"));
            CheckModels("/Assets/Models", Path.Combine(assetsFolder, "Models"));

            var environments = Path.Combine(assetsFolder, "External", "Environments");
            if (Directory.Exists(environments))
            {
                foreach (var environment in Directory.EnumerateDirectories(environments))
                {
                    if (Path.GetFileName(environment).EndsWith("@tmp"))
                    {
                        continue;
                    }
                    CheckEnvironment(environment);
                }
            }

            var vehicles = Path.Combine(assetsFolder, "External", "Vehicles");
            if (Directory.Exists(vehicles))
            {
                foreach (var vehicle in Directory.EnumerateDirectories(vehicles))
                {
                    if (Path.GetFileName(vehicle).EndsWith("@tmp"))
                    {
                        continue;
                    }
                    CheckVehicle(vehicle);
                }
            }

            CheckMetaFiles("/Assets", assetsFolder);
        }

        void CheckEnvironment(string environment)
        {
            var name = Path.GetFileName(environment);
            var folderName = $"/Assets/External/Environments/{name}";
            var scene = Path.Combine(environment, $"{name}.unity");

            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            if (File.Exists(scene))
            {
                CheckExternalDependencies(folderName, $"{folderName}/{name}.unity");
            }
            else
            {
                Log(Category.Error, $"Environment scene '{folderName}/{name}.unity' does not exist");
            }

            var folders = new[]
            {
                "Animations",
                "Materials",
                "Media",
                "Models",
                "Prefabs",
                "Scenes",
                "Shaders",
                "Textures",
                name,
            };

            CheckFolders(folderName, environment, folders, Array.Empty<string>(), true);

            var models = Path.Combine(environment, "Models");
            if (Directory.Exists(models))
            {
                foreach (var model in Directory.EnumerateDirectories(models))
                {
                    var modelName = Path.GetFileName(model);
                    var modelFolder = $"{folderName}/Models/{modelName}";
                    CheckModels(modelFolder, model);
                }
            }
        }

        void CheckVehicle(string vehicle)
        {
            var name = Path.GetFileName(vehicle);
            var folderName = $"/Assets/External/Vehicles/{name}";
            var prefab = Path.Combine(vehicle, $"{name}.prefab");

            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            if (File.Exists(prefab))
            {
                CheckExternalDependencies(folderName, $"{folderName}/{name}.prefab");
            }
            else
            {
                Log(Category.Error, $"Vehicle prefab '{folderName}/{name}.prefab' does not exist");
            }

            var folders = new[]
            {
                "Animations",
                "Media",
                "Models",
                "Prefabs",
                "Scenes",
                "Shaders",
            };

            CheckFolders(folderName, vehicle, folders, Array.Empty<string>(), true);

            var models = Path.Combine(vehicle, "Models");
            if (Directory.Exists(models))
            {
                var modelFolder = $"{folderName}/Models";
                CheckModels(modelFolder, models);
            }
        }

        void CheckMetaFiles(string folderName, string folder)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            var metas = new HashSet<string>();

            // each folder should have meta file
            bool empty = true;
            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                empty = false;
                var name = Path.GetFileName(f);

                if (name.StartsWith(".") || name.EndsWith("@tmp"))
                {
                    continue;
                }

                var meta = Path.Combine(folder, name) + ".meta";
                var targetFolder = $"{folderName}/{name}";
                if (!File.Exists(meta))
                {
                    Log(Category.Error, $"Meta file '{name}.meta' does not exist for '{targetFolder}' folder");
                }
                CheckMetaFiles(targetFolder, f);

                metas.Add(name);
            }

            // each file should have meta file
            foreach (var f in Directory.EnumerateFiles(folder))
            {
                empty = false;
                var name = Path.GetFileName(f);

                if (name.StartsWith(".") || Path.GetExtension(name) == ".meta")
                {
                    continue;
                }

                var meta = Path.Combine(folder, name) + ".meta";
                if (!File.Exists(meta))
                {
                    Log(Category.Error, $"Meta file '{name}.meta' does not exist for '{folderName}/{name}' file");
                }
                metas.Add(name);
            }

            if (empty)
            {
                Log(Category.Warning, $"Folder '{folderName}' is empty");
            }

            // there should be no other meta files left
            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                var metaName = Path.GetFileNameWithoutExtension(name);
                if (Path.GetExtension(name) != ".meta" || metaName.EndsWith("@tmp"))
                {
                    continue;
                }
                if (!metas.Contains(metaName))
                {
                    Log(Category.Error, $"Meta file '{folderName}/{name}' should be deleted");
                }
            }
        }

        void CheckSpaces(string folderName, string folder)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);
                if (name.IndexOf(" ") != -1)
                {
                    Log(Category.Error, $"Folder name '{name}' contains spaces in '{folderName}'");
                }
                else
                {
                    var target = $"{folderName}/{name}";
                    if (target != "/Library" && target != "/Temp" && target != "/obj" &&
                        target != "/WebUI/node_modules" &&
                        !target.StartsWith("/Packages") &&
                        target != "/Assets/GlobalSettings/HDRPDefaultResources")
                    {
                        CheckSpaces($"{folderName}/{name}", f);
                    }
                }
            }

            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (name.IndexOf(" ") != -1)
                {
                    Log(Category.Error, $"File name '{name}' contains spaces in '{folderName}'");
                }
            }
        }

        void CheckScripts(string folderName, string folder)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);
                if (name.StartsWith("."))
                {
                    continue;
                }

                var subfolder = $"{folderName}/{name}";
                if (subfolder == "/Assets/Scripts/Bridge/Cyber/Protobuf")
                {
                    continue;
                }

                if (name != "Generated")
                {
                    CheckScripts(subfolder, f);
                }
            }

            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                var extension = Path.GetExtension(f);
                if (name.StartsWith("."))
                {
                    continue;
                }
                if (extension == ".meta")
                {
                    continue;
                }
                if (folderName == "/Assets/Scripts" && name == "csc.rsp")
                {
                    continue;
                }

                if (extension != ".cs" && extension != ".asmdef" && extension != ".inputactions")
                {
                    Log(Category.Error, $"File '{name}' does not have allowed extension inside '{folderName}' folder");
                }

                if (extension == ".cs")
                {
                    var header = new byte[3];
                    using (var fs = File.OpenRead(f))
                    {
                        fs.Read(header, 0, header.Length);
                    }
                    if (header[0] < 32 || header[0] >= 0x80 || header[1] < 32 || header[1] >= 0x80)
                    {
                        Log(Category.Warning, $"File '{folderName}/{name}' starts with non-ASCII characters, check if you need to remove UTF-8 BOM");
                    }

                    using (var fs = File.OpenText(f))
                    {
                        fs.ReadLine();
                        var copyright = fs.ReadLine();
                        if (!copyright.StartsWith(" * Copyright"))
                        {
                            var exceptions = new[]
                            {
                                "/Assets/Scripts/Editor/Lanelet2MapImporter.ComputeCenterLine.cs",
                                "/Assets/Scripts/Map/MapOrigin.Conversion.cs",
                                "/Assets/Scripts/Editor/OdrSpiral.cs",
                                "/Assets/Scripts/Editor/OpenDRIVE_1.4H.cs",
                            };

                            if (!exceptions.Contains($"{folderName}/{name}"))
                            {
                                Log(Category.Error, $"File '{folderName}/{name}' does not have correct copyright header");
                            }
                        }
                    }
                }
            }
        }

        void CheckModels(string folderName, string folder)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            if (Directory.GetFiles(folder, "*.fbx", SearchOption.TopDirectoryOnly).Length == 0)
            {
                foreach (var f in Directory.EnumerateDirectories(folder))
                {
                    var name = Path.GetFileName(f);
                    CheckModels($"{folderName}/{name}", f);
                }
            }
            else
            {
                CheckExtensions(folderName, folder, UnityFolders["Models"]);
                CheckFolders(folderName, folder, new[] { "Materials" }, Array.Empty<string>(), true);

                var materialFolder = $"{folderName}/Materials";
                var materials = Path.Combine(folder, "Materials");
                if (Directory.Exists(materials))
                {
                    CheckExtensions(materialFolder, materials, UnityFolders["Materials"]);
                }
            }
        }

        void CheckPlugins(string folderName, string folder)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith(".") && Path.GetExtension(f) != ".meta" && Path.GetExtension(f) != ".asmdef")
                {
                    Log(Category.Error, $"File '{name}' is not allowed inside '{folderName}'");
                }
            }

            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);
                var files = Directory.EnumerateFiles(f);

                int licenses =
                    files.Count(fname => fname.ToLowerInvariant().Contains("license")) +
                    files.Count(fname => fname.ToLowerInvariant().Contains("licence")) +
                    files.Count(fname => fname.ToLowerInvariant().Contains("copying"));
                if (licenses == 0)
                {
                    Log(Category.Warning, $"Plugin '{folderName}/{name}' does not have license file");
                }

                // TODO: extra checks for native plugins?
            }
        }

        void CheckUnityFolders(string folderName, string folder)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            var name = Path.GetFileName(folder);
            string[] extensions;
            if (UnityFolders.TryGetValue(name, out extensions))
            {
                CheckExtensions(folderName, folder, extensions);
            }
        }

        void CheckFolders(string folderName, string folder, string[] allowedFolders, string[] requiredFolders, bool error)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            var found = new HashSet<string>();

            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            if (!Directory.Exists(folder))
            {
                Log(Category.Error, $"Folder '{folderName}' does not exist");
                return;
            }

            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);

                if (name.StartsWith(".") || name.EndsWith("@tmp"))
                {
                    continue;
                }

                if (allowedFolders.Contains(name) || requiredFolders.Contains(name))
                {
                    CheckUnityFolders($"{folderName}/{name}", f);
                }
                else
                {
                    Log(error ? Category.Error : Category.Warning, $"Folder '{name}' should not be inside of '{folderName}'");
                }

                found.Add(name);
            }

            foreach (var f in requiredFolders)
            {
                if (!found.Contains(f))
                {
                    Log(Category.Error, $"Folder '{f}' does not exist inside '{folderName}'");
                }
            }
        }

        void CheckFiles(string folderName, string folder, string[] allowedFiles, string[] requiredFiles, bool error)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !folderName.StartsWith(BaseFolder))
            {
                return;
            }

            var found = new HashSet<string>();

            if (!Directory.Exists(folder))
            {
                Log(Category.Error, $"Folder '{folderName}' does not exist");
                return;
            }

            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith(".") && Path.GetExtension(f) != ".meta")
                {
                    if (!allowedFiles.Contains(name) && !requiredFiles.Contains(name))
                    {
                        Log(error ? Category.Error : Category.Warning, $"File '{name}' should not be inside '{folderName}'");
                    }
                    found.Add(name);
                }
            }

            foreach (var f in requiredFiles)
            {
                if (!found.Contains(f))
                {
                    Log(Category.Error, $"File '{f}' does not exist inside '{folderName}'");
                }
            }
        }

        void CheckExtensions(string rootName, string root, string[] allowed)
        {
            if (!string.IsNullOrEmpty(BaseFolder) && !rootName.StartsWith(BaseFolder))
            {
                return;
            }

            if (!Directory.Exists(root))
            {
                Log(Category.Error, $"Folder '{rootName}' does not exist");
                return;
            }

            foreach (var f in Directory.EnumerateFiles(root))
            {
                var name = Path.GetFileName(f);
                var ext = Path.GetExtension(f);
                if (!name.StartsWith(".") && ext != ".meta")
                {
                    if (!allowed.Contains(ext))
                    {
                        Log(Category.Error, $"File '{name}' with '{ext}' extension is not allowed inside '{rootName}'");
                    }
                }
            }
        }

        void CheckMainDependencies(string scene)
        {
            foreach (var dep in AssetDatabase.GetDependencies(scene.Substring(1), true))
            {
                if (dep.StartsWith("Packages/"))
                {
                    continue;
                }
                if (dep.StartsWith("Assets/") && !dep.StartsWith("Assets/External/"))
                {
                    continue;
                }
                Log(Category.Error, $"Main scene depends on '/{dep}'");
            }
        }

        void CheckExternalDependencies(string externalFolder, string externalAsset)
        {
            externalFolder = externalFolder.Substring(1);
            externalAsset = externalAsset.Substring(1);

            if (!string.IsNullOrEmpty(BaseFolder) && !externalFolder.StartsWith(BaseFolder))
            {
                return;
            }

            var dependencies = new List<string>();
            foreach (var dep in AssetDatabase.GetDependencies(externalAsset, true))
            {
                if (dep.StartsWith("Packages/") || dep.StartsWith("Assets/GlobalSettings"))
                {
                    continue;
                }
                if (dep.StartsWith("Assets/Scripts/") || dep.StartsWith("Assets/Shaders/"))
                {
                    continue;
                }
                if (dep.StartsWith($"{externalFolder}/"))
                {
                    continue;
                }
                dependencies.Add(dep);
            }

            dependencies.Sort();
            foreach (var dep in dependencies)
            {
                Log(Category.Error, $"Asset '/{externalAsset}' depends on '/{dep}'");
            }
        }
    }

    public class Check : EditorWindow
    {
        GUIStyle Style;
        Vector2 ScrollPosition;
        string Output;

        [SerializeField]
        string BaseFolder;

        [MenuItem("Simulator/Check...", false, 10)]
        static void ShowWindow()
        {
            var window = GetWindow<Check>(false, "Consistency Check");
            window.BaseFolder = string.Empty;
            window.Style = new GUIStyle(EditorStyles.textField);
            window.Style.richText = true;

            var data = EditorPrefs.GetString("Simulator/ConsistencyCheck", JsonUtility.ToJson(window, false));
            JsonUtility.FromJsonOverwrite(data, window);

            window.Show();

            window.RunCheck();
        }

        void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("Simulator/ConsistencyCheck", data);
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check", GUILayout.ExpandWidth(false)))
            {
                RunCheck();
            }
            GUILayout.Label("Base folder:", GUILayout.ExpandWidth(false));
            BaseFolder = GUILayout.TextField(BaseFolder);
            EditorGUILayout.EndHorizontal();

            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);
            GUILayout.TextArea(Output, Style, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        void RunCheck()
        {
            Output = string.Empty;
            var checker = new Checker(BaseFolder, (category, message) =>
            {
                if (category == Checker.Category.Error)
                {
                    Output += $"<color=red><b>ERROR:</b></color> {message}\n";
                }
                else if (category == Checker.Category.Warning)
                {
                    Output += $"<color=yellow><b>WARNING:</b></color> {message}\n";
                }
                else
                {
                    Output += $"{message}\n";
                }
            });
            checker.Run();
        }

        static void Run()
        {
            string saveCheck = null;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-saveCheck")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        saveCheck = args[i];
                    }
                    else
                    {
                        throw new Exception("-saveCheck expects filename!");
                    }
                }
            }

            StreamWriter sw = null;
            if (saveCheck != null)
            {
                sw = new StreamWriter(saveCheck, false, Encoding.UTF8);
                sw.WriteLine("<html><body>");
                sw.WriteLine($"<p>Check at {DateTime.Now}</p>");
                sw.Write("<div style='font-family: monospace; white-space: pre;'>");
            }
            try
            {
                var checker = new Checker("", (category, message) =>
                {
                    if (category == Checker.Category.Error)
                    {
                        Console.WriteLine($"ERROR: {message}");
                        sw?.WriteLine($"<b><font color='#C00'>ERROR</font></b>: {message}");
                    }
                    else if (category == Checker.Category.Warning)
                    {
                        Console.WriteLine($"WARNING: {message}");
                        sw?.WriteLine($"<b><font color='#CC0'>WARNING</font></b>: {message}");
                    }
                    else
                    {
                        Console.WriteLine(message);
                        sw?.WriteLine(message);
                    }
                });
                checker.Run();
            }
            finally
            {
                if (sw != null)
                {
                    sw.WriteLine("</div></body></html>");
                    sw.Dispose();
                }
            }
        }
    }
}
