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
    public class Check : EditorWindow
    {
        static readonly Dictionary<string, string[]> UnityFolders = new Dictionary<string, string[]>()
        {
            // folder => allowed extensions
            { "Animations", new [] { ".controller", ".anim", ".playable" } },
            { "Audio", new [] { ".wav", ".mp3" } },
            { "Effects", new [] { ".png" } },
            { "GlobalSettings", new [] { ".asset" } },
            { "Materials", new [] { ".mat", ".png", ".jpg" } },
            { "Models", new [] { ".fbx", ".st" } },
            { "Physics", new [] { ".physicMaterial" } },
            { "Pefabs", new [] { ".prefab" } },
            { "Resources", new [] { ".txt", ".prefab", ".asset" } }, // TODO: does this need prefab?
            { "ScriptableObjects", new [] { ".asset" } },
            { "Scenes", new [] { ".unity" } },
            { "Shaders", new [] { ".shader", ".hlsl" } },
            { "Textures", new [] { ".png", ".exr", ".jpg" } },
        };

        public enum Category
        {
            Info,
            Warning,
            Error,
        };

        GUIStyle Style;
        Vector2 ScrollPosition;
        string Output;

        [MenuItem("Simulator/Check...", false, 10)]
        static void ShowWindow()
        {
            var window = GetWindow<Check>();
            window.Style = new GUIStyle(EditorStyles.textField);
            window.Style.richText = true;
            window.Show();
        }

        void OnEnable()
        {
            RunCheck();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Check", GUILayout.ExpandWidth(false)))
            {
                RunCheck();
            }

            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);
            GUILayout.TextArea(Output, Style, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        void RunCheck()
        {
            Output = string.Empty;
            RunCheck((category, message) =>
            {
                if (category == Category.Error)
                {
                    Output += $"<color=red><b>ERROR:</b></color> {message}\n";
                }
                else if (category == Category.Warning)
                {
                    Output += $"<color=yellow><b>WARNING:</b></color> {message}\n";
                }
                else
                {
                    Output += $"{message}\n";
                }
            });
        }

        static void RunCheck(Action<Category, string> log)
        {
            log(Category.Info, "Checking...");

            var rootFolders = new[]
            {
                // allowed or generated folders
                "AssetBundles",
                "Docker",
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

            CheckFolders(log, "/", rootPath, rootFolders, rootFoldersReq, false);
            CheckExtensions(log, "/ProjectSettings", Path.Combine(rootPath, "ProjectSettings"), new[] { ".asset", ".txt" });

            CheckAssets(log, Path.Combine(rootPath, "Assets"));
            CheckSpaces(log, "", rootPath);

            CheckMainDependencies(log, "Assets/Scenes/LoaderScene.unity");

            log(Category.Info, "Done!");
        }

        static void CheckAssets(Action<Category, string> log, string assetsFolder)
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

            CheckFolders(log, "/Assets", assetsFolder, assetFolders, assetFoldersReq, true);
            CheckFiles(log, "/Assets", assetsFolder, assetFiles, Array.Empty<string>(), true);

            CheckScripts(log, "/Assets/Scripts", Path.Combine(assetsFolder, "Scripts"));
            CheckScripts(log, "/Assets/Tests", Path.Combine(assetsFolder, "Tests"));

            CheckPlugins(log, "/Assets/Plugins", Path.Combine(assetsFolder, "Plugins"));
            CheckModels(log, "/Assets/Models", Path.Combine(assetsFolder, "Models"));

            var environments = Path.Combine(assetsFolder, "External", "Environments");
            if (Directory.Exists(environments))
            {
                foreach (var environment in Directory.EnumerateDirectories(environments))
                {
                    if (Path.GetFileName(environment).EndsWith("@tmp"))
                    {
                        continue;
                    }
                    CheckEnvironment(log, environment);
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
                    CheckVehicle(log, vehicle);
                }
            }

            CheckMetaFiles(log, "/Assets", assetsFolder);
        }

        static void CheckEnvironment(Action<Category, string> log, string environment)
        {
            var name = Path.GetFileName(environment);
            var folderName = $"/Assets/External/Environments/{name}";
            var scene = Path.Combine(environment, $"{name}.unity");

            if (File.Exists(scene))
            {
                CheckExternalDependencies(log, folderName, $"{folderName}/{name}.unity");
            }
            else
            {
                log(Category.Error, $"Environment scene '{folderName}/{name}.unity' does not exist");
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
            };

            CheckFolders(log, folderName, environment, folders, Array.Empty<string>(), true);

            var models = Path.Combine(environment, "Models");
            if (Directory.Exists(models))
            {
                foreach (var model in Directory.EnumerateDirectories(models))
                {
                    var modelName = Path.GetFileName(model);
                    var modelFolder = $"{folderName}/Models/{modelName}";

                    CheckExtensions(log, modelFolder, model, UnityFolders["Models"]);

                   foreach (var asset in Directory.EnumerateDirectories(model))
                   {
                        var assetName = Path.GetFileName(asset);
                        if (!name.StartsWith("."))
                        {
                            var assetFolderName = $"{modelFolder}/{assetName}";
                            if (assetName == "Materials")
                            {
                                CheckFolders(log, assetFolderName, asset, new[] { "Materials" }, Array.Empty<string>(), true);
                            }
                            else
                            {
                                CheckExtensions(log, assetFolderName, asset, UnityFolders["Models"]);
                                CheckFolders(log, assetFolderName, asset, new[] { "Materials" }, Array.Empty<string>(), true);
                            }
                        }
                    }
                }
            }
        }

        static void CheckVehicle(Action<Category, string> log, string vehicle)
        {
            var name = Path.GetFileName(vehicle);
            var folderName = $"/Assets/External/Vehicles/{name}";
            var prefab = Path.Combine(vehicle, $"{name}.prefab");

            if (File.Exists(prefab))
            {
                CheckExternalDependencies(log, folderName, $"{folderName}/{name}.prefab");
            }
            else
            {
                log(Category.Error, $"Vehicle prefab '{folderName}/{name}.prefab' does not exist");
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

            CheckFolders(log, folderName, vehicle, folders, Array.Empty<string>(), true);

            var models = Path.Combine(vehicle, "Models");
            if (Directory.Exists(models))
            {
                var modelFolder = $"{folderName}/Models";
                CheckExtensions(log, modelFolder, models, UnityFolders["Models"]);
                CheckFolders(log, modelFolder, models, Array.Empty<string>(), new[] { "Materials" }, true);

                var materialFolder = $"{modelFolder}/Materials";
                var materials = Path.Combine(models, "Materials");
                if (Directory.Exists(materials))
                {
                    CheckExtensions(log, materialFolder, materials, UnityFolders["Materials"]);
                }
            }
        }

        static void CheckMetaFiles(Action<Category, string> log, string folderName, string folder)
        {
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
                    log(Category.Error, $"Meta file '{name}.meta' does not exist for '{targetFolder}' folder");
                }
                CheckMetaFiles(log, targetFolder, f);

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
                    log(Category.Error, $"Meta file '{name}.meta' does not exist for '{folderName}/{name}' file");
                }
                metas.Add(name);
            }

            if (empty)
            {
                log(Category.Warning, $"Folder '{folderName}' is empty");
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
                    log(Category.Error, $"Meta file '{folderName}/{name}' should be deleted");
                }
            }
        }

        static void CheckSpaces(Action<Category, string> log, string folderName, string folder)
        {
            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);
                if (name.IndexOf(" ") != -1)
                {
                    log(Category.Error, $"Folder name '{name}' contains spaces in '{folderName}'");
                }
                else
                {
                    var target = $"{folderName}/{name}";
                    if (target != "/Library" && target != "/Temp" && target != "/obj" &&
                        target != "/WebUI/node_modules" &&
                        !target.StartsWith("/Packages"))
                    {
                        CheckSpaces(log, $"{folderName}/{name}", f);
                    }
                }
            }

            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (name.IndexOf(" ") != -1)
                {
                    log(Category.Error, $"File name '{name}' contains spaces in '{folderName}'");
                }
            }
        }

        static void CheckScripts(Action<Category, string> log, string folderName, string folder)
        {
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
                    CheckScripts(log, subfolder, f);
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
                    log(Category.Error, $"File '{name}' does not have allowed extension inside '{folderName}' folder");
                }

                if (extension == ".cs")
                {
                    var header = new byte[3];
                    using (var fs = File.OpenRead(f))
                    {
                        fs.Read(header, 0, header.Length);
                    }
                    if (header[0] < 32 || header[0] >= 0x80 ||
                        header[1] < 32 || header[1] >= 0x80 ||
                        header[2] < 32 || header[2] >= 0x80)
                    {
                        log(Category.Warning, $"File '{folderName}/{name}' starts with non-ASCII characters, check if you need to remove UTF-8 BOM");
                    }

                    using (var fs = File.OpenText(f))
                    {
                        fs.ReadLine();
                        var copyright = fs.ReadLine();
                        if (!copyright.StartsWith(" * Copyright"))
                        {
                            log(Category.Error, $"File '{folderName}/{name}' does not have correct copyright header");
                        }
                    }
                }
            }
        }

        static void CheckModels(Action<Category, string> log, string folderName, string folder)
        {
            if (Directory.GetFiles(folder, "*.fbx", SearchOption.TopDirectoryOnly).Length == 0)
            {
                foreach (var f in Directory.EnumerateDirectories(folder))
                {
                    var name = Path.GetFileName(f);
                    CheckModels(log, $"{folderName}/{name}", f);
                }
            }
            else
            {
                CheckExtensions(log, folderName, folder, UnityFolders["Models"]);
                CheckFolders(log, folderName, folder, new[] { "Materials" }, Array.Empty<string>(), true);

                var materialFolder = $"{folderName}/Materials";
                var materials = Path.Combine(folder, "Materials");
                if (Directory.Exists(materials))
                {
                    CheckExtensions(log, materialFolder, materials, UnityFolders["Materials"]);
                }
            }
        }

        static void CheckPlugins(Action<Category, string> log, string folderName, string folder)
        {
            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith(".") && Path.GetExtension(f) != ".meta" && Path.GetExtension(f) != ".asmdef")
                {
                    log(Category.Error, $"File '{name}' is not allowed inside '{folderName}'");
                }
            }

            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);
                var files = Directory.EnumerateFiles(f);

                bool hasLicense = files.Count(fname => fname.ToLowerInvariant().Contains("license")) != 0;
                if (!hasLicense)
                {
                    log(Category.Warning, $"Plugin '{folderName}/{name}' does not have license file");
                }

                // TODO: extra checks for native plugins?
            }
        }

        static void CheckUnityFolders(Action<Category, string> log, string folderName, string folder)
        {
            var name = Path.GetFileName(folder);
            string[] extensions;
            if (UnityFolders.TryGetValue(name, out extensions))
            {
                CheckExtensions(log, folderName, folder, extensions);
            }
        }

        static void CheckFolders(Action<Category, string> log, string folderName, string folder, string[] allowedFolders, string[] requiredFolders, bool error)
        {
            var found = new HashSet<string>();

            if (!Directory.Exists(folder))
            {
                log(Category.Error, $"Folder '{folderName}' does not exist");
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
                    CheckUnityFolders(log, $"{folderName}/{name}", f);
                }
                else
                {
                    log(error ? Category.Error : Category.Warning, $"Folder '{name}' should not be inside of '{folderName}'");
                }

                found.Add(name);
            }

            foreach (var f in requiredFolders)
            {
                if (!found.Contains(f))
                {
                    log(Category.Error, $"Folder '{f}' does not exist inside '{folderName}'");
                }
            }
        }

        static void CheckFiles(Action<Category, string> log, string folderName, string folder, string[] allowedFiles, string[] requiredFiles, bool error)
        {
            var found = new HashSet<string>();

            if (!Directory.Exists(folder))
            {
                log(Category.Error, $"Folder '{folderName}' does not exist");
                return;
            }

            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith(".") && Path.GetExtension(f) != ".meta")
                {
                    if (!allowedFiles.Contains(name) && !requiredFiles.Contains(name))
                    {
                        log(error ? Category.Error : Category.Warning, $"File '{name}' should not be inside '{folderName}'");
                    }
                    found.Add(name);
                }
            }

            foreach (var f in requiredFiles)
            {
                if (!found.Contains(f))
                {
                    log(Category.Error, $"File '{f}' does not exist inside '{folderName}'");
                }
            }
        }

        static void CheckExtensions(Action<Category, string> log, string rootName, string root, string[] allowed)
        {
            if (!Directory.Exists(root))
            {
                log(Category.Error, $"Folder '{rootName}' does not exist");
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
                        log(Category.Error, $"File '{name}' with '{ext}' extension is not allowed inside '{rootName}'");
                    }
                }
            }
        }

        public static void CheckMainDependencies(Action<Category, string> log, string scene)
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
                log(Category.Error, $"Main scene depends on '/{dep}'");
            }
        }

        public static void CheckExternalDependencies(Action<Category, string> log, string externalFolder, string externalAsset)
        {
            externalFolder = externalFolder.Substring(1);
            externalAsset = externalAsset.Substring(1);

            var dependencies = new List<string>();
            foreach (var dep in AssetDatabase.GetDependencies(externalAsset, true))
            {
                if (dep.StartsWith("Packages/") || dep.StartsWith("Assets/GlobalSettings"))
                {
                    continue;
                }
                if (dep.StartsWith("Assets/Scripts/"))
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
                log(Category.Error, $"Asset '/{externalAsset}' depends on '/{dep}'");
            }
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
                RunCheck((category, message) =>
                {
                    if (category == Category.Error)
                    {
                        Console.WriteLine($"ERROR: {message}");
                        sw?.WriteLine($"<b><font color='#C00'>ERROR</font></b>: {message}");
                    }
                    else if (category == Category.Warning)
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
