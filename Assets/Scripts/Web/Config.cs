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
using System.Reflection;
using Simulator.Api;
using Simulator.Bridge;
using Simulator.Controllable;
using Simulator.Database;
using Simulator.Database.Services;
using Simulator.Sensors;
using Simulator.Utilities;
using UnityEditor;
using UnityEngine;
using VirtualFileSystem;
using YamlDotNet.Serialization;

namespace Simulator.Web
{
    public static class Config
    {
        public static int sessionTimeout = 60 * 60 * 24 * 365;

        public static string ApiHost = "localhost";
        public static int ApiPort = 8181;

        public static string CloudUrl = "https://wise.svlsimulator.com";
        public static string CloudProxy;
        public static string SessionGUID;
        public static string SimID;

        public static bool Headless = false;

        public static bool RetryForever = false;

        public static string Root;
        public static string PersistentDataPath;

        public static List<Manifest> LoadedAssets = new List<Manifest>();
        public static List<SensorBase> SensorPrefabs;
        public static List<SensorConfig> Sensors;
        public static Dictionary<string, SensorBase> SensorTypeLookup = new Dictionary<string, SensorBase>();

        public static Dictionary<string, IControllable> Controllables = new Dictionary<string, IControllable>();
        public static Dictionary<IControllable, List<GameObject>> ControllableAssets = new Dictionary<IControllable, List<GameObject>>();
        public static Dictionary<string, Type> NPCBehaviours = new Dictionary<string, Type>();
        public static Dictionary<Type, GameObject> CustomManagers = new Dictionary<Type, GameObject>();

        public class NPCAssetData
        {
            [NonSerialized]
            public GameObject Prefab;
            public Map.NPCSizeType NPCType;
            public string Name;
            public string AssetGuid;
            public bool Enabled = true;
        }
        public static Dictionary<string, NPCAssetData> NPCVehicles = new Dictionary<string, NPCAssetData>();
        public class PedAssetData
        {
            [NonSerialized]
            public GameObject Prefab;
            public string Name;
            public string AssetGuid;
            public bool Enabled = true;
        }
        public static Dictionary<string, PedAssetData> Pedestrians = new Dictionary<string, PedAssetData>();
        public static Dictionary<string, IntPtr> FMUs = new Dictionary<string, IntPtr>(); // managed by FMU.cs

        public static int DefaultPageSize = 100;

        public static FileStream LockFile;

        public static bool DeveloperDebugModeEnabled = false;

        public static string SentryDSN = "";

        private static AssetService AssetService;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void Initialize()
        {
            Root = Path.Combine(Application.dataPath, "..");
            PersistentDataPath = Application.persistentDataPath;
            PersistentDataPath += "-" + CloudAPI.GetInfo().version;

            AssetService = new AssetService();

            ParseConfigFile();
            if (!Application.isEditor)
            {
                ParseCommandLine();
            }

            CreatePersistentPath();

            if (!Application.isEditor)
            {
                CreateLockFile();
            }

            AssetBundle.UnloadAllAssetBundles(false);
            Sensors = new List<SensorConfig>();
            SensorPrefabs = new List<SensorBase>();

            BridgePlugins.Load();
            LoadBuiltinAssets();
            LoadExternalAssets();
            Sensors = SensorTypes.ListSensorFields(SensorPrefabs);

            DatabaseManager.Init();

            ClientSettingsService csservice = new ClientSettingsService();
            if (string.IsNullOrEmpty(SimID))
            {
                SimID = csservice.GetOrMake().simid;
            }

            csservice.SetSimID(SimID);
            AssetBundle.UnloadAllAssetBundles(false);
        }

        public static void RegenerateSimID()
        {
            SimID = Guid.NewGuid().ToString();
            ClientSettingsService csservice = new ClientSettingsService();
            csservice.SetSimID(SimID);
        }

        public delegate void AssetLoadFunc(Manifest manifest, VfsEntry dir);

        public static void CheckDir(VfsEntry dir, AssetLoadFunc loadFunc)
        {
            if (dir == null)
            {
                return;
            }

            var manifestFile = dir.Find("manifest.json");
            if (manifestFile != null && manifestFile.IsFile)
            {
                Debug.Log($"found manifest at {manifestFile.Path}");
                using (var reader = new StreamReader(manifestFile.SeekableStream()))
                {
                    try
                    {
                        if (reader == null)
                        {
                            Debug.Log("no reader cannot open stream");
                        }
                        Manifest manifest;
                        try
                        {
                            var buffer = reader.ReadToEnd();
                            manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(buffer);
                        }
                        catch
                        {
                            throw new Exception("Out of date AssetBundle, rebuild or download latest AssetBundle.");
                        }
                        loadFunc(manifest, dir);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"failed to load asset from {manifestFile.Path}");
                        Debug.LogException(ex);
                    }
                }
            }
            else
            {
                foreach (var entry in dir)
                {
                    CheckDir(entry, loadFunc);
                }
            }
        }

        private static void LoadBuiltinAssets() // TODO remove
        {
            var behaviours = new[]
            {
                typeof(NPCLaneFollowBehaviour),
                typeof(NPCWaypointBehaviour),
                typeof(NPCManualBehaviour),
            };
            foreach (var b in behaviours)
            {
                NPCBehaviours.Add(b.ToString(), b);
            }

#if UNITY_EDITOR
            if (EditorPrefs.GetBool("Simulator/Developer Debug Mode", false) == true)
            {
                try
                {
                    var npcAssembly = Assembly.Load("Simulator.NPCs");
                    foreach (var ty in npcAssembly.GetTypes())
                    {
                        if (ty.IsAbstract)
                        {
                            continue;
                        }
                        if (typeof(NPCBehaviourBase).IsAssignableFrom(ty))
                        {
                            NPCBehaviours.Add(ty.ToString(), ty);
                        }
                        else if (typeof(ICommand).IsAssignableFrom(ty))
                        {
                            var cmd = Activator.CreateInstance(ty) as ICommand;
                            ApiManager.Commands.Add(cmd.Name, cmd);
                        }
                        else if (typeof(ICustomManager).IsAssignableFrom(ty))
                        {
                            var npcDir = Path.Combine(BundleConfig.ExternalBase, BundleConfig.pluralOf(BundleConfig.BundleTypes.NPC));
                            var prefabGuid = AssetDatabase.FindAssets($"t:GameObject {ty.Name}", new[] { npcDir }).FirstOrDefault();
                            if (prefabGuid != null)
                            {
                                var path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                                var prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                                CustomManagers.Add(ty, prefab);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Development Mode NPCs not loaded: " + e.Message);
                }

            }
#endif
        }

        private static void SaveConfigFile()
        {
            try
            {
                File.WriteAllText(Path.Combine(Root, "config.yml"), new Serializer().Serialize(ConvertConfigFile()));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public static void LoadExternalAssets()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var dir = Path.Combine(Application.dataPath, "..", "AssetBundles");
            Debug.Log("Pre-loading Assetbundles from root path " + dir);
            var vfs = VfsEntry.makeRoot(dir);

            // descend into each known dir looking for only specific asset types. todo: add asset type to manifest?
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Controllable)), LoadControllablePlugin);
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Bridge)), LoadBridgePlugin); // NOTE: bridges must be loaded before sensor plugins
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Sensor)), LoadSensorPlugin);
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.NPC)), LoadNPCAsset);
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Pedestrian)), LoadPedestrianAsset);
            Debug.Log($"Loaded NPCs behaviours: {NPCBehaviours.Count}  NPC models: {NPCVehicles.Count}  Pedestrians: {Pedestrians.Count} Controllables: {Controllables.Count} Bridges: {BridgePlugins.All.Count } Sensors: {Sensors.Count} in {sw.Elapsed}");
        }

        private static Assembly LoadAssembly(VfsEntry dir, string name)
        {
            var dll = dir.Find(name);
            if (dll == null)
            {
                return null;
            }
            byte[] buffer = new byte[dll.Size];
            dll.GetStream().Read(buffer, 0, (int)dll.Size);
            return Assembly.Load(buffer);
        }


        public static void LoadBridgePlugin(Manifest manifest, VfsEntry dir)
        {
#if UNITY_EDITOR
            if (EditorPrefs.GetBool("Simulator/Developer Debug Mode", false) == true)
            {
                Assembly bridgesAssembly = null;
                if (File.Exists(Path.Combine(BundleConfig.ExternalBase, "Bridges", manifest.assetName, $"{manifest.assetName}.cs")))
                {
                    if (bridgesAssembly == null) bridgesAssembly = Assembly.Load("Simulator.Bridges");
                    foreach (Type ty in bridgesAssembly.GetTypes())
                    {
                        if (typeof(IBridgeFactory).IsAssignableFrom(ty) && !ty.IsAbstract && ty.GetCustomAttribute<BridgeNameAttribute>().Name == manifest.bridgeType)
                        {
                            Debug.LogWarning($"Loading {manifest.bridgeType} ({manifest.assetGuid}) in Developer Debug Mode. If you wish to use this bridge plugin from WISE, disable Developer Debug Mode in Simulator->Developer Debug Mode or remove the bridge from Assets/External/Bridges");
                            var bridgeFactory = Activator.CreateInstance(ty) as IBridgeFactory;
                            BridgePlugins.Add(bridgeFactory);

                            LoadedAssets.Add(manifest);
                        }
                    }
                    return;
                }
            }
#endif

            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Bridge])
            {
                throw new Exception($"Manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Bridge]}, got {manifest.assetFormat}");
            }

            var pluginSource = LoadAssembly(dir, $"{manifest.assetName}.dll");
            foreach (Type ty in pluginSource.GetTypes())
            {

                if (typeof(IBridgeFactory).IsAssignableFrom(ty) && !ty.IsAbstract)
                {
                    var bridgeFactory = Activator.CreateInstance(ty) as IBridgeFactory;
                    BridgePlugins.Add(bridgeFactory);
                    LoadedAssets.Add(manifest);
                }
            }
        }

        public static void LoadSensorPlugin(Manifest manifest, VfsEntry dir)
        {
#if UNITY_EDITOR
            if (EditorPrefs.GetBool("Simulator/Developer Debug Mode", false) == true)
            {
                if (File.Exists(Path.Combine(BundleConfig.ExternalBase, "Sensors", manifest.assetName, $"{manifest.assetName}.prefab")))
                {
                    Debug.LogWarning($"Loading {manifest.assetName} ({manifest.assetGuid}) in Developer Debug Mode. If you wish to use this sensor plugin from WISE, disable Developer Debug Mode in Simulator->Developer Debug Mode or remove the sensor from Assets/External/Sensors");
                    var path = Path.Combine(BundleConfig.ExternalBase, "Sensors", manifest.assetName, $"{manifest.assetName}.prefab");
                    var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                    if (prefab == null)
                    {
                        Debug.LogWarning("prefab is null for " + path);
                    }
                    SensorPrefabs.Add(prefab.GetComponent<SensorBase>());
                    var sensorConfig = SensorTypes.GetConfig(prefab.GetComponent<SensorBase>());
                    sensorConfig.AssetGuid = manifest.assetGuid;
                    Sensors.Add(sensorConfig);
                    LoadedAssets.Add(manifest);
                    if (!SensorTypeLookup.ContainsKey(manifest.assetGuid))
                    {
                        SensorTypeLookup.Add(manifest.assetGuid, prefab.GetComponent<SensorBase>());
                    }

                    var pluginType = prefab.GetComponent<SensorBase>().GetDataBridgePlugin();
                    if (pluginType != null)
                    {
                        var sensorBridgePlugin = Activator.CreateInstance(pluginType) as ISensorBridgePlugin;
                        foreach (var kv in BridgePlugins.All)
                        {
                            sensorBridgePlugin.Register(kv.Value);
                        }
                    }

                    return;
                }
            }
#endif
            if (SensorTypeLookup.ContainsKey(manifest.assetGuid))
            {
                return;
            }

            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Sensor])
            {
                throw new Exception($"Manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Sensor]}, got {manifest.assetFormat}");
            }

            if (Sensors.FirstOrDefault(s => s.AssetGuid == manifest.assetGuid) != null)
            {
                return;
            }

            Assembly pluginSource = LoadAssembly(dir, $"{manifest.assetName}.dll");
            foreach (Type ty in pluginSource.GetTypes())
            {
                if (typeof(ISensorBridgePlugin).IsAssignableFrom(ty) && !ty.IsAbstract)
                {
                    var sensorBridgePlugin = Activator.CreateInstance(ty) as ISensorBridgePlugin;
                    foreach (var kv in BridgePlugins.All)
                    {
                        sensorBridgePlugin.Register(kv.Value);
                    }
                }
            }

            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
            var pluginStream = dir.Find($"{manifest.assetGuid}_sensor_main_{platform}").SeekableStream();
            AssetBundle pluginBundle = AssetBundle.LoadFromStream(pluginStream);
            var pluginAssets = pluginBundle.GetAllAssetNames();

            var texDir = dir.Find($"{manifest.assetGuid}_sensor_textures");
            if (texDir != null)
            {
                var texStream = dir.Find($"{manifest.assetGuid}_sensor_textures").SeekableStream();
                var textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                if (!AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
                {
                    textureBundle.LoadAllAssets();
                }
            }

            SensorBase pluginBase = pluginBundle.LoadAsset<GameObject>(pluginAssets[0]).GetComponent<SensorBase>();
            SensorConfig config = SensorTypes.GetConfig(pluginBase);
            config.AssetGuid = manifest.assetGuid;
            Sensors.Add(config);
            SensorPrefabs.Add(pluginBase);
            LoadedAssets.Add(manifest);
            if (!SensorTypeLookup.ContainsKey(manifest.assetGuid))
            {
                SensorTypeLookup.Add(manifest.assetGuid, pluginBase);
            }
        }

        public static void LoadControllablePlugin(Manifest manifest, VfsEntry dir)
        {
            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Controllable])
            {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Controllable]}, got {manifest.assetFormat}");
            }

            Assembly pluginSource = LoadAssembly(dir, $"{manifest.assetName}.dll");

            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
            var pluginStream = dir.Find($"{manifest.assetGuid}_controllable_main_{platform}").SeekableStream();
            AssetBundle pluginBundle = AssetBundle.LoadFromStream(pluginStream);
            var pluginAssets = pluginBundle.GetAllAssetNames();

            var texDir = dir.Find($"{manifest.assetGuid}_controllable_textures");
            if (texDir != null)
            {
                var texStream = dir.Find($"{manifest.assetGuid}_controllable_textures").SeekableStream();
                var textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                if (!AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
                {
                    textureBundle.LoadAllAssets();
                }
            }

            var prefabName = $"{manifest.assetName}.prefab";
            //Find a prefab with main asset name ignoring the characters case
            var mainPrefabName = pluginAssets.First(name => name.IndexOf(prefabName, StringComparison.InvariantCultureIgnoreCase) >= 0);
            var controllable = pluginBundle.LoadAsset<GameObject>(mainPrefabName).GetComponent<IControllable>();

#if UNITY_EDITOR
            if (EditorPrefs.GetBool("Simulator/Developer Debug Mode", false) == true)
            {
                if (File.Exists(Path.Combine(BundleConfig.ExternalBase, "Controllables", manifest.assetName, $"{manifest.assetName}.prefab")))
                {
                    var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(Path.Combine(BundleConfig.ExternalBase, "Controllables", manifest.assetName, $"{manifest.assetName}.prefab"), typeof(GameObject));
                    Controllables.Add(manifest.assetName, prefab.GetComponent<IControllable>());

                    var controllableAssets = new List<GameObject>();
                    foreach (var pluginAsset in pluginAssets)
                    {
                        if (pluginAsset == mainPrefabName)
                            continue;
                        controllableAssets.Add(pluginBundle.LoadAsset<GameObject>(pluginAsset));
                    }
                    ControllableAssets.Add(controllable, controllableAssets);
                    LoadedAssets.Add(manifest);

                    return;
                }
            }
#endif

            Controllables.Add(manifest.assetName, controllable);
            var additionalAssets = new List<GameObject>();
            foreach (var pluginAsset in pluginAssets)
            {
                if (pluginAsset == mainPrefabName)
                    continue;
                additionalAssets.Add(pluginBundle.LoadAsset<GameObject>(pluginAsset));
            }
            ControllableAssets.Add(controllable, additionalAssets);
            LoadedAssets.Add(manifest);
        }

        private static void LoadNPCAsset(Manifest manifest, VfsEntry dir)
        {
            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.NPC])
            {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.NPC]}, got {manifest.assetFormat}");
            }
            List<Type> customManagers = new List<Type>();
            Assembly pluginSource = LoadAssembly(dir, $"{manifest.assetName}.dll");
            if (pluginSource != null)
            {
                foreach (Type ty in pluginSource.GetTypes())
                {
                    if (ty.IsAbstract)
                    {
                        continue;
                    }
                    if (typeof(NPCBehaviourBase).IsAssignableFrom(ty))
                    {
                        if (!NPCBehaviours.ContainsKey(ty.ToString()))
                        {
                            NPCBehaviours.Add(ty.ToString(), ty);
                        }
                    }
                    else if (typeof(ICommand).IsAssignableFrom(ty))
                    {
                        var cmd = Activator.CreateInstance(ty) as ICommand;
                        ApiManager.Commands.Add(cmd.Name, cmd);
                    }
                    else if (typeof(ICustomManager).IsAssignableFrom(ty))
                    {
                        customManagers.Add(ty);
                    }
                }
            }

            var texEntry = dir.Find($"{manifest.assetGuid}_npc_textures");
            AssetBundle textureBundle = null;
            if (texEntry != null)
            {
                var texStream = VirtualFileSystem.VirtualFileSystem.EnsureSeekable(texEntry.SeekableStream(), (int)texEntry.Size);
                textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                texStream.Close();
                texStream.Dispose();
            }

            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
            var pluginEntry = dir.Find($"{manifest.assetGuid}_npc_main_{platform}");

#if UNITY_EDITOR
            if (EditorPrefs.GetBool("Simulator/Developer Debug Mode", false) == true)
            {
                foreach (var NPCDir in Directory.EnumerateDirectories(Path.Combine(BundleConfig.ExternalBase, BundleConfig.pluralOf(BundleConfig.BundleTypes.NPC))))
                {
                    var assemblyExists = true;
                    try
                    {
                        // Try loading into reflection context before loading into AppDomain. If this fails, it means
                        // there are no scripts in the assembly and it was not compiled - skip loading it.
                        Assembly.ReflectionOnlyLoad("Simulator.NPCs");
                    }
                    catch
                    {
                        assemblyExists = false;
                    }

                    if (assemblyExists)
                        Assembly.Load("Simulator.NPCs");

                    if (File.Exists(Path.Combine(NPCDir, manifest.assetName, $"{manifest.assetName}.prefab")))
                    {
                        var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(Path.Combine(NPCDir, manifest.assetName, $"{manifest.assetName}.prefab"), typeof(GameObject));
                        if (prefab != null)
                        {
                            var meta = prefab.GetComponent<NPCMetaData>();
                            if (meta != null)
                            {
                                NPCVehicles.Add(manifest.assetName, new NPCAssetData()
                                {
                                    Prefab = prefab,
                                    Name = manifest.assetName,
                                    AssetGuid = manifest.assetGuid,
                                    NPCType = meta.SizeType,
                                });
                            }
                            else
                            {
                                Debug.LogWarning($"NPC {manifest.assetName} missing meta info, setting default type");
                            }
                        }

                        if (textureBundle && !AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
                        {
                            textureBundle.LoadAllAssets();
                        }

                        LoadedAssets.Add(manifest);
                        return;
                    }
                }
            }
#endif

            if (pluginEntry != null)
            {
                AssetBundle pluginBundle = AssetBundle.LoadFromStream(pluginEntry.SeekableStream());
                var pluginAssets = pluginBundle.GetAllAssetNames();
                var prefabName = $"{manifest.assetName}.prefab";
                var mainPrefabName =
                    pluginAssets.FirstOrDefault(name => name.IndexOf(prefabName, StringComparison.InvariantCultureIgnoreCase) >= 0);
                if (mainPrefabName != null)
                {
                    GameObject prefab = pluginBundle.LoadAsset<GameObject>(mainPrefabName);

                    var meta = prefab.GetComponent<NPCMetaData>();

                    if (meta != null)
                    {
                        NPCVehicles.Add(manifest.assetName, new NPCAssetData()
                        {
                            Prefab = prefab,
                            Name = manifest.assetName,
                            AssetGuid = manifest.assetGuid,
                            NPCType = meta.SizeType,
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"NPC {manifest.assetName} missing meta info, setting default type");
                    }
                }

                LoadCustomManagers(customManagers, pluginBundle);
            }

            if (pluginEntry == null && pluginSource == null)
            {
                Debug.LogWarning("Neither assembly nor prefab found in " + manifest.assetName);
            }

            if (textureBundle && !AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
            {
                textureBundle.LoadAllAssets();
            }

            LoadedAssets.Add(manifest);
        }

        private static void LoadPedestrianAsset(Manifest manifest, VfsEntry dir)
        {
            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Pedestrian])
            {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Pedestrian]}, got {manifest.assetFormat}");
            }

            var texEntry = dir.Find($"{manifest.assetGuid}_pedestrian_textures");
            AssetBundle textureBundle = null;
            if (texEntry != null)
            {
                var texStream = VirtualFileSystem.VirtualFileSystem.EnsureSeekable(texEntry.SeekableStream(), (int)texEntry.Size);
                textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                texStream.Close();
                texStream.Dispose();
            }

            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
            var pluginEntry = dir.Find($"{manifest.assetGuid}_pedestrian_main_{platform}");

#if UNITY_EDITOR
            if (EditorPrefs.GetBool("Simulator/Developer Debug Mode", false) == true)
            {
                var assemblyExists = true;
                try
                {
                    // Try loading into reflection context before loading into AppDomain. If this fails, it means
                    // there are no scripts in the assembly and it was not compiled - skip loading it.
                    Assembly.ReflectionOnlyLoad("Simulator.Pedestrians");
                }
                catch
                {
                    assemblyExists = false;
                }

                if (assemblyExists)
                    Assembly.Load("Simulator.Pedestrians");

                foreach (var PedDir in Directory.EnumerateDirectories(Path.Combine(BundleConfig.ExternalBase, BundleConfig.pluralOf(BundleConfig.BundleTypes.Pedestrian))))
                {
                    if (File.Exists(Path.Combine(PedDir, manifest.assetName, $"{manifest.assetName}.prefab")))
                    {
                        var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(Path.Combine(PedDir, manifest.assetName, $"{manifest.assetName}.prefab"), typeof(GameObject));

                        Pedestrians.Add(manifest.assetName, new PedAssetData()
                        {
                            Prefab = prefab,
                            Name = manifest.assetName,
                            AssetGuid = manifest.assetGuid,
                        });

                        if (textureBundle && !AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
                        {
                            textureBundle.LoadAllAssets();
                        }

                        LoadedAssets.Add(manifest);
                        return;
                    }
                }
            }
#endif

            if (pluginEntry != null)
            {
                AssetBundle pluginBundle = AssetBundle.LoadFromStream(pluginEntry.SeekableStream());
                var pluginAssets = pluginBundle.GetAllAssetNames();
                var prefabName = $"{manifest.assetName}.prefab";
                var mainPrefabName = pluginAssets.First(name => name.IndexOf(prefabName, StringComparison.InvariantCultureIgnoreCase) >= 0);
                GameObject prefab = pluginBundle.LoadAsset<GameObject>(mainPrefabName);

                Pedestrians.Add(manifest.assetName, new PedAssetData()
                {
                    Prefab = prefab,
                    Name = manifest.assetName,
                    AssetGuid = manifest.assetGuid,
                });
            }

            if (pluginEntry == null)
            {
                Debug.LogWarning("No prefab found in " + manifest.assetName);
            }

            if (textureBundle && !AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
            {
                textureBundle.LoadAllAssets();
            }

            LoadedAssets.Add(manifest);
        }

        private static void LoadCustomManagers(List<Type> types, AssetBundle bundle)
        {
            var assets = bundle.GetAllAssetNames();
            foreach (var type in types)
            {
                var typeName = type.Name;
                var prefabName = assets.First(name => name.IndexOf(typeName, StringComparison.InvariantCultureIgnoreCase) >= 0);
                if (prefabName != null)
                {
                    var prefab = (GameObject)bundle.LoadAsset(prefabName);
                    CustomManagers.Add(type, prefab);
                }
            }
        }

        private class YamlConfig
        {
            public bool headless { get; set; } = Config.Headless;
            public bool read_only { get; set; } = false;
            public string api_hostname { get; set; } = Config.ApiHost;
            public int api_port { get; set; } = Config.ApiPort;
            public string cloud_url { get; set; } = Config.CloudUrl;
            public string cloud_proxy { get; set; } = Config.CloudProxy;
            public string data_path { get; set; } = Config.PersistentDataPath;
        }

        private static YamlConfig LoadConfigFile(string file)
        {
            using (var fs = File.OpenText(file))
            {
                try
                {
                    return new Deserializer().Deserialize<YamlConfig>(fs);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            return null;
        }

        private static YamlConfig ConvertConfigFile()
        {
            return new YamlConfig()
            {
                api_hostname = ApiHost,
                api_port = ApiPort,
                data_path = PersistentDataPath,
                cloud_url = CloudUrl,
                cloud_proxy = CloudProxy,
                headless = Headless
            };
        }

        public static void ParseConfigFile()
        {
            var configFile = Path.Combine(Root, "config.yml");
            if (!File.Exists(configFile))
            {
                return;
            }

            var config = LoadConfigFile(configFile);
            if (config == null)
            {
                return;
            }

            ApiHost = config.api_hostname ?? "localhost";
            ApiPort = config.api_port;

            PersistentDataPath = config.data_path;

            CloudUrl = config.cloud_url.TrimEnd('/');
            string cloudUrl = Environment.GetEnvironmentVariable("SIMULATOR_CLOUDURL");
            if (!string.IsNullOrEmpty(cloudUrl))
            {
                CloudUrl = cloudUrl;
            }

            CloudProxy = config.cloud_proxy;
            string cloudProxy = Environment.GetEnvironmentVariable("http_proxy") ??
                                Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!string.IsNullOrEmpty(cloudProxy))
            {
                CloudProxy = cloudProxy;
            }

            Headless = config.headless;
        }

        private static void ParseCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--simid":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for sim id provided!");
                            Application.Quit(1);
                        }
                        SimID = args[++i];
                        break;
                    case "--cloudurl":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for cloud url provided!");
                            Application.Quit(1);
                        }
                        CloudUrl = args[++i];
                        break;
                    case "--apihost":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for api hostname provided!");
                            Application.Quit(1);
                        }
                        ApiHost = args[++i];
                        break;
                    case "--apiport":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for api port provided!");
                            Application.Quit(1);
                        }

                        if (!int.TryParse(args[++i], out ApiPort))
                        {
                            Debug.LogError("Port must be an integer!");
                            Application.Quit(1);
                        }
                        break;
                    case "--data":
                    case "-d":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for data path provided!");
                            Application.Quit(1);
                        }

                        PersistentDataPath = Path.GetFullPath(args[++i]);
                        break;
                    case "--retryForever":
                        RetryForever = true;
                        break;
                    default:
                        // skip unknown arguments to allow to pass default Unity Player args
                        Debug.LogWarning($"Unknown argument {args[i]}, skipping it");
                        break;
                }
            }
        }

        static void CreatePersistentPath()
        {
            if (!Directory.Exists(PersistentDataPath))
            {
                try
                {
                    Directory.CreateDirectory(PersistentDataPath);
                }
                catch
                {
                    Debug.LogError($"Cannot create directory at {PersistentDataPath}");
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit(1);
#endif
                }
            }
        }

        static void CreateLockFile()
        {
            try
            {
                LockFile = File.Open(Path.Combine(PersistentDataPath, "run.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                // mono on Linux requires explicit lock
                LockFile.Lock(0, 0);
            }
            catch (IOException ex) when ((short)ex.HResult == 32 || (short)ex.HResult == 33) // 32 = ERROR_SHARING_VIOLATION, 33 = ERROR_LOCK_VIOLATION
            {
                Debug.LogError($"!!! Another instance of simulator is already using this data folder: {PersistentDataPath}");
                Application.Quit(1); // return non-zero exit code
            }
        }
    }
}
