/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simulator.Bridge;
using Simulator.Database;
using Simulator.Web;
using UnityEditor;
using UnityEngine;

namespace Simulator.Editor
{
    public class DevelopmentSettingsWindow : EditorWindow
    {
        struct VersionEntry
        {
            public string display;
            public string version;
        }
        private List<VersionEntry> SimulatorVersions = null;

        private int versionIndex = -1;
        private List<VehicleDetailData> CloudVehicles = new List<VehicleDetailData>();
        private List<string> LocalVehicles = new List<string>();

        private DevelopmentSettingsAsset Settings;
        private SimulationData DeveloperSimulation = null;

        private VehicleData EgoVehicle => DeveloperSimulation.Vehicles?[0];

        private CloudAPI API;
        private string ErrorMessage;
        private static DevelopmentSettingsWindow Instance;
        private int CurrentVehicleIndex = 0;
        private string SensorScratchPad = "";

        private Vector2 ScrollPos;
        private bool VehicleSetup = false;
        private bool NPCSelectEnable = false;
        private float SaveAssetTime = float.MaxValue;

        private bool linked = true;
        private bool updateVehicleDetails = true;
        private string[] Bridges = new string[] { };

        [MenuItem("Simulator/Development Settings...", false, 50)]
        public static void Open()
        {
            var window = GetWindow<DevelopmentSettingsWindow>();
            window.titleContent = new GUIContent("Development Settings");
            window.Show();
        }

        void OnEnable()
        {
            Instance = this;
            EditorApplication.playModeStateChanged += HandlePlayMode;
            EditorApplication.update += OnEditorUpdate;

            Settings = (DevelopmentSettingsAsset)AssetDatabase.LoadAssetAtPath("Assets/Resources/Editor/DeveloperSettings.asset", typeof(DevelopmentSettingsAsset));
            if (Settings == null)
            {
                Settings = (DevelopmentSettingsAsset)CreateInstance(typeof(DevelopmentSettingsAsset));
                Debug.Log("Initialized new developer settings");
                Settings.VersionOverride = Application.version;
                AssetDatabase.CreateAsset(Settings, "Assets/Resources/Editor/DeveloperSettings.asset");
            }

            if (Settings.developerSimulationJson != null)
            {
                DeveloperSimulation = JsonConvert.DeserializeObject<SimulationData>(Settings.developerSimulationJson);
            }

            if (DeveloperSimulation == null)
            {
                DeveloperSimulation = new SimulationData()
                {
                    Name = "DeveloperSettings",
                    TimeOfDay = DateTime.Now,
                };
            }

            Refresh();
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayMode;
        }

        void OnDestroy()
        {
            Instance = null;
        }

        protected virtual void OnEditorUpdate()
        {
            if (Time.realtimeSinceStartup > SaveAssetTime + 3.0f)
            {
                UpdateAsset();
            }
        }

        private static void HandlePlayMode(PlayModeStateChange state)
        {
            if (Instance == null)
                return;

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Instance.Refresh();
            }
            else if (state == PlayModeStateChange.ExitingEditMode)
            {
                Instance.API.Disconnect();
            }
        }

        bool updating = false;

        class VehicleChoiceEntry
        {
            public string cloudIdOrPrefabPath;
            public string configId = null;
            public VehicleDetailData vehicleDetailData = null;
            public string display;
            public bool IsLocal => cloudIdOrPrefabPath.EndsWith(".prefab");
        }

        List<VehicleChoiceEntry> VehicleChoices =>
                             LocalVehicles.Select(prefabPath =>
                             new VehicleChoiceEntry
                             {
                                 cloudIdOrPrefabPath = prefabPath,
                                 display = "local: " + Path.GetFileName(prefabPath)
                             }).Concat(
                             CloudVehicles.SelectMany(
                                 vehicle => vehicle.SensorsConfigurations, (detailData, config) => new VehicleChoiceEntry
                                 {
                                     cloudIdOrPrefabPath = detailData.Id,
                                     configId = config.Id,
                                     vehicleDetailData = detailData,
                                     display = "cloud: " + detailData.Name + "/" + config.Name
                                 }
                                     ))
                             .ToList();

        async void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                updating = true;
                ErrorMessage = "";
                Simulator.Web.Config.ParseConfigFile();

                await UpdateVersions();

                try
                {
                    // FIXME also list and find bundles and cloud bridges?
                    var bridgesAssembly = Assembly.Load("Simulator.Bridges");
                    Bridges = bridgesAssembly.GetTypes()
                    .Where(ty => typeof(IBridgeFactory).IsAssignableFrom(ty) && !ty.IsAbstract)
                    .Select(ty => BridgePlugins.GetNameFromFactory(ty)).ToArray();
                }
                catch { }

                if (DeveloperSimulation.Cluster == null)
                {
                    DeveloperSimulation.Cluster = new ClusterData()
                    {
                        Name = "DeveloperSettingsDummy",
                        Instances = new[]
                        {
                            new InstanceData
                            {
                                HostName="dummy.developer.settings",
                                Ip = new []{ "127.0.0.1" },
                                MacAddress="00:00:00:00:00:00"
                            }
                        }
                    };
                }

                if (string.IsNullOrEmpty(Config.CloudProxy))
                {
                    API = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID);
                }
                else
                {
                    API = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID, new Uri(Config.CloudProxy));
                }

                DatabaseManager.Init();
                var csservice = new Simulator.Database.Services.ClientSettingsService();
                ClientSettings cls = csservice.GetOrMake();
                Config.SimID = cls.simid;
                if (string.IsNullOrEmpty(Config.CloudUrl))
                {
                    ErrorMessage = "Cloud URL not set";
                    return;
                }
                if (string.IsNullOrEmpty(Config.SimID))
                {
                    linked = false;
                    ErrorMessage = "Simulator not linked";
                    return;
                }

                var ret = await API.GetLibrary<VehicleDetailData>();
                CloudVehicles = ret.ToList();

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/External/Vehicles" });
                LocalVehicles = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToList();

                string idOrPath = null;
                if (DeveloperSimulation.Vehicles != null) // get previously selected thing
                {
                    // we abuse VehicleData.Id to store the prefab path
                    idOrPath = EgoVehicle.Id;
                }

                if (idOrPath != null)
                {
                    // find index of previously selected thing in new dataset
                    var foundIndex = VehicleChoices.FindIndex(v => v.cloudIdOrPrefabPath == idOrPath && (v.IsLocal || v.configId == Settings.VehicleConfigId));
                    SetVehicleFromSelectionIndex(foundIndex);
                    updateVehicleDetails = true;
                    await UpdateCloudVehicleDetails();
                }

                DeveloperSimulation.NPCs = Config.NPCVehicles.Values.ToArray(); // TODO get from cloud and refresh config.cs LoadExternalAssets()
            }
            catch (CloudAPI.NoSuccessException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ErrorMessage = "This instance requires linking to a cluster on " + Config.CloudUrl;
                    linked = false;
                }
                else
                {
                    Debug.LogException(ex);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ErrorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    ErrorMessage += "\n" + ex.InnerException.Message;
                }
            }
            finally
            {
                updating = false;
                Repaint();
            }
        }

        static readonly System.Text.RegularExpressions.Regex versionRegExp = new System.Text.RegularExpressions.Regex(@"^(\d+)\.(\d+)(-rc\d+)?$");
        public static Task<List<string>> GetGitVersionTags()
        {
            var tcs = new TaskCompletionSource<List<string>>();
            var lines = new List<string>();
            string errorString = string.Empty;
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        FileName = "git",
                        Arguments = "tag --sort=creatordate --merged",
                    },
                    EnableRaisingEvents = true
                };
                process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(
                    delegate (object sender, System.Diagnostics.DataReceivedEventArgs e)
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        errorString += e.Data + "\n";
                    }
                );
                process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler
                (
                    delegate (object sender, System.Diagnostics.DataReceivedEventArgs e)
                    {
                        if (string.IsNullOrEmpty(e.Data)) return;
                        var match = versionRegExp.Match(e.Data);
                        if (!match.Success) return;
                        lines.Add(e.Data);
                    }
                );
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.Exited += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(errorString))
                    {
                        tcs.SetException(new Exception(errorString));
                    }
                    else
                    {
                        tcs.SetResult(lines);
                    }
                    process.CancelOutputRead();
                    process.CancelErrorRead();
                    process.Dispose();
                };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            return tcs.Task;
        }

        async Task UpdateVersions()
        {
            var tags = new List<string>();
            try
            {
                tags = await GetGitVersionTags();
            }
            catch (Exception e)
            {
                ErrorMessage += $"Error invoking git to get tags: ({e.Message})";
            }

            SimulatorVersions = new List<VersionEntry>();
            SimulatorVersions.Add(new VersionEntry { display = "Enter version manually" });
            if (!tags.Contains(Application.version))
            {
                SimulatorVersions.Add(new VersionEntry { display = Application.version + " (Project Settings Version)", version = Application.version });
            }

            var currentReleaseIndex = 0;
            tags.Reverse();
            foreach (var tag in tags)
            {
                if (!tag.Contains("-rc"))
                {
                    SimulatorVersions.Add(new VersionEntry { display = tag + " (last release)", version = tag });
                    currentReleaseIndex = SimulatorVersions.Count - 1;
                    // stop with first release found and do not list older tags
                    // user should check out tag instead as only that provides all the changed dependency to match
                    break;
                }
                SimulatorVersions.Add(new VersionEntry { display = tag, version = tag });
            }

            // if no version was selected, select last release as development target
            if (string.IsNullOrWhiteSpace(Settings.VersionOverride))
            {
                Settings.VersionOverride = SimulatorVersions[currentReleaseIndex].version;
                versionIndex = currentReleaseIndex;
            }
            else
            {
                versionIndex = SimulatorVersions.FindIndex((e) => e.version == Settings.VersionOverride);
                if (versionIndex == -1) versionIndex = 0;
            }
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Cloud URL: " + Config.CloudUrl, MessageType.Info);
            EditorGUILayout.HelpBox(new GUIContent("SimID: " + Config.SimID, "Identifies this instance"));

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                EditorGUILayout.HelpBox(ErrorMessage, MessageType.Warning);
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Disabled during play mode", MessageType.Info);
                return;
            }

            if (updating)
            {
                EditorGUILayout.HelpBox("Updating...", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            if (!linked && GUILayout.Button(new GUIContent("Link", "Add this instance to a cluster or create a cluster.")))
            {
                var simInfo = CloudAPI.GetInfo();
                LinkTask(simInfo);
            }
            if (GUILayout.Button(new GUIContent("Refresh", "Refresh displayed local and cloud asset data in this window.")))
            {
                Refresh();
            }
            if (GUILayout.Button(new GUIContent("Manage clusters", "Visit cluster page on " + Config.CloudUrl)))
            {
                Application.OpenURL(Config.CloudUrl + "/clusters");
            }

            EditorGUILayout.EndHorizontal();

            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);

            if (SimulatorVersions != null && SimulatorVersions.Count > 0)
            {

                if (versionIndex < 0 || versionIndex >= SimulatorVersions.Count)
                {
                    versionIndex = SimulatorVersions.FindIndex(e => e.version == Settings.VersionOverride);
                    if (versionIndex < 0)
                    {
                        versionIndex = 0;
                    }
                }

                versionIndex = EditorGUILayout.Popup(new GUIContent("Version", "Influences which compatible asset which will be downloaded from wise"), versionIndex, SimulatorVersions.Select(v => v.display).ToArray());
                if (versionIndex < 0 || versionIndex >= SimulatorVersions.Count)
                {
                    versionIndex = SimulatorVersions.Count - 1;
                }

                if (versionIndex == 0)
                {
                    Settings.VersionOverride = EditorGUILayout.TextField(new GUIContent("Version", "Influences which compatible asset which will be downloaded from wise"), Settings.VersionOverride);
                }
                else
                {
                    Settings.VersionOverride = SimulatorVersions[versionIndex].version;
                }
            }

            foreach (PropertyInfo prop in typeof(SimulationData).GetProperties())
            {
                if (prop.Name == "Id" || prop.Name == "UpdatedAt" || prop.Name == "CreatedAt" || prop.Name == "OwnerId" || prop.Name == "TestReportId" || prop.Name == "Version")
                    continue;

                object value = prop.GetValue(DeveloperSimulation);
                if (prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(DeveloperSimulation, EditorGUILayout.ToggleLeft(prop.Name, ((bool?)value).Value));
                }
                else if (prop.PropertyType == typeof(int))
                {
                    prop.SetValue(DeveloperSimulation, EditorGUILayout.IntField(prop.Name, ((int?)value).Value));
                }
                else if (prop.PropertyType == typeof(float))
                {
                    prop.SetValue(DeveloperSimulation, EditorGUILayout.FloatField(prop.Name, ((float?)value).Value));
                }
                else if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(DeveloperSimulation, EditorGUILayout.TextField(prop.Name, (string)value));
                }
                else if (prop.PropertyType == typeof(DateTime))
                {
                    var userValue = EditorGUILayout.TextField(prop.Name, ((DateTime)value).ToString());
                    if (DateTime.TryParse(userValue, out DateTime dt))
                    {
                        prop.SetValue(DeveloperSimulation, dt);
                    }
                }
                else if (prop.PropertyType == typeof(int?))
                {
                    int? val = (int?)value;
                    EditorGUILayout.BeginHorizontal();
                    bool doSet = EditorGUILayout.ToggleLeft("enable " + prop.Name, val.HasValue);
                    EditorGUI.BeginDisabledGroup(!doSet);
                    int? newValue = EditorGUILayout.IntField(prop.Name, val.HasValue ? val.Value : 0, GUILayout.ExpandWidth(true));
                    prop.SetValue(DeveloperSimulation, doSet ? newValue : null);
                    EditorGUI.EndDisabledGroup();
                    GUILayout.EndHorizontal();
                }
                else if (prop.PropertyType == typeof(VehicleData[]))
                {
                    VehicleSetup = EditorGUILayout.Foldout(VehicleSetup, "EGO Vehicle Setup");
                    if (VehicleSetup)
                    {
                        if (VehicleChoices.Count > 0)
                        {
                            EditorGUI.indentLevel++;
                            var newIndex = EditorGUILayout.Popup(CurrentVehicleIndex, VehicleChoices.Select(v => v.display).ToArray());
                            var vehicle = SetVehicleFromSelectionIndex(newIndex);

                            if (vehicle.Id.EndsWith(".prefab"))
                            {
                                if (Bridges.Length == 0)
                                {
                                    EditorGUILayout.LabelField("no local bridges available");
                                    vehicle.Bridge = null;
                                }
                                else
                                {
                                    var wantBridge = EditorGUILayout.ToggleLeft("Enable Bridge", vehicle.Bridge != null);
                                    if (!wantBridge)
                                    {
                                        vehicle.Bridge = null;
                                    }
                                    else if (vehicle.Bridge == null)
                                    {
                                        vehicle.Bridge = new BridgeData { };
                                    }

                                    if (vehicle.Bridge != null)
                                    {
                                        var bridgeIndex = Array.IndexOf(Bridges, vehicle.Bridge.Type);
                                        if (bridgeIndex < 0)
                                        {
                                            bridgeIndex = 0;
                                        }

                                        bridgeIndex = EditorGUILayout.Popup("Bridge Type", bridgeIndex, Bridges);
                                        vehicle.Bridge.Type = Bridges[bridgeIndex];
                                        vehicle.Bridge.ConnectionString = EditorGUILayout.TextField("Bridge Connection", vehicle.Bridge.ConnectionString);
                                    }
                                }
                                EditorGUILayout.LabelField("json sensor config");
                                SensorScratchPad = EditorGUILayout.TextArea(SensorScratchPad, GUILayout.ExpandHeight(true));

                                try
                                {
                                    vehicle.Sensors = JsonConvert.DeserializeObject<SensorData[]>(SensorScratchPad);
                                    if (vehicle.Sensors == null)
                                    {
                                        vehicle.Sensors = new SensorData[0];
                                    }
                                }
                                catch (Exception e)
                                {
                                    EditorGUILayout.HelpBox(e.Message, MessageType.Error);
                                }
                            }
                            else
                            {
                                if (vehicle.Bridge != null)
                                {
                                    vehicle.Bridge.ConnectionString = EditorGUILayout.TextField("Bridge Connection", vehicle.Bridge.ConnectionString);
                                    EditorGUILayout.TextField("Bridge Type", vehicle.Bridge.Type);
                                    EditorGUILayout.TextField("Bridge Name", vehicle.Bridge.Name);
                                    EditorGUILayout.TextField("Bridge Id", vehicle.Bridge.Id);
                                }
                                EditorGUILayout.LabelField("json sensor config");
                                EditorGUILayout.TextArea(JsonConvert.SerializeObject(vehicle.Sensors, JsonSettings.camelCasePretty), GUILayout.ExpandHeight(true));
                            }
                            EditorGUI.indentLevel--;
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("no Vehicles in Cloud Library or Assets/External/Vehicles", MessageType.Info);
                        }
                    }
                }
            }

            NPCSelectEnable = EditorGUILayout.BeginToggleGroup("NPC Select", NPCSelectEnable);
            if (NPCSelectEnable)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
                if (GUILayout.Button("Select All", GUILayout.ExpandWidth(false)))
                {
                    foreach (var npc in DeveloperSimulation.NPCs)
                    {
                        npc.Enabled = true;
                    }
                }
                if (GUILayout.Button("Select None", GUILayout.ExpandWidth(false)))
                {
                    foreach (var npc in DeveloperSimulation.NPCs)
                    {
                        npc.Enabled = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
                foreach (var npc in DeveloperSimulation.NPCs)
                {
                    npc.Enabled = EditorGUILayout.Toggle($"{npc.Name}", npc.Enabled);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                SaveAssetTime = Time.realtimeSinceStartup;
            }
        }

        private VehicleData SetVehicleFromSelectionIndex(int newSelectionIndex)
        {
            VehicleData previousVehicle = DeveloperSimulation.Vehicles?[0];

            if (newSelectionIndex < 0 || newSelectionIndex >= VehicleChoices.Count)
            {
                Debug.Log($"previously selected vehicle missing. {newSelectionIndex}/{VehicleChoices.Count}");
                newSelectionIndex = 0;
            }
            var selection = VehicleChoices[newSelectionIndex];
            CurrentVehicleIndex = newSelectionIndex;

            VehicleData vehicle;

            if (selection.IsLocal)
            {
                Settings.VehicleConfigId = null;
                if (previousVehicle != null && selection.cloudIdOrPrefabPath == previousVehicle.Id)
                {
                    return previousVehicle;
                }

                vehicle = new VehicleData
                {
                    Id = selection.cloudIdOrPrefabPath,
                    Bridge = previousVehicle?.Bridge
                };
            }
            else // Cloud Vehicle
            {
                vehicle = selection.vehicleDetailData.ToVehicleData();
                vehicle.Sensors = vehicle.SensorsConfigurations.First(config => config.Id == selection.configId).Sensors;
                if (previousVehicle != null && vehicle.Id == previousVehicle.Id && selection.configId == Settings.VehicleConfigId)
                {
                    return previousVehicle;
                }
                updateVehicleDetails = true;
                Settings.VehicleConfigId = selection.configId;
            }

            DeveloperSimulation.Vehicles = new VehicleData[] { vehicle };
            UpdateCloudVehicleDetails();
            return vehicle;
        }

        async Task UpdateCloudVehicleDetails()
        {
            if (!updateVehicleDetails)
                return;

            updateVehicleDetails = false;

            if (API == null)
                return;

            if (DeveloperSimulation.Vehicles == null)
                return;

            var pluginLibrary = await API.GetLibrary<PluginDetailData>();
            var bridgeLibrary = pluginLibrary.Where(p => p.Category == "bridge");

            if (!EgoVehicle.Id.EndsWith(".prefab"))
            {
                try
                {
                    var selection = VehicleChoices[CurrentVehicleIndex];
                    // vehicle list does not give us sensor data, so we have to get it later.
                    // I do not want to query each vehicle individually and I can't block the UI, so
                    // we do it after a change was made to the settings in this fire and forget async function
                    var data = await API.Get<VehicleDetailData>(selection.cloudIdOrPrefabPath);
                    var selectedConfig = data.SensorsConfigurations.First(c => c.Id == selection.configId);
                    data.Sensors = selectedConfig.Sensors;
                    data.Bridge = selectedConfig.Bridge;

                    if (data.Bridge != null && string.IsNullOrEmpty(data.Bridge.AssetGuid))
                    {
                        var candidate = bridgeLibrary.FirstOrDefault(p => p.Id == selectedConfig.BridgePluginId);
                        if (candidate != null)
                        {
                            data.Bridge.AssetGuid = candidate.AssetGuid;
                        }
                    }

                    // copy previous bridge data as it is not saved with the vehicle
                    if (EgoVehicle.Bridge != null && selectedConfig.Bridge != null)
                    {
                        data.Bridge.ConnectionString = EgoVehicle.Bridge.ConnectionString;
                    }
                    DeveloperSimulation.Vehicles = new VehicleData[] { data.ToVehicleData() };

                    // splice in fetched data (containing extended data like bridge) for matching id
                    CloudVehicles = CloudVehicles.Select(v => v.Id == EgoVehicle.Id ? data : v).ToList();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogWarning($"Failed to get details of vehicle {EgoVehicle.Id}: {e.Message}");
                }
            }

            if (EgoVehicle.Sensors != null)
            {
                try
                {
                    var sensorsLibrary = pluginLibrary.Where(p => p.Category == "sensor");

                    foreach (var sensor in EgoVehicle.Sensors)
                    {
                        if (sensor.Plugin == null)
                        {
                            sensor.Plugin = new SensorPlugin();
                        }

                        if (string.IsNullOrEmpty(sensor.Plugin.AssetGuid))
                        {
                            Utilities.SensorConfig offlineCandidate = null;
                            foreach (var config in Config.Sensors)
                            {
                                Debug.Log($" {config.Name}: assetguid: {config.AssetGuid} params {string.Join(", ", config.Parameters.Select(p => p.Name))}");
                            }

                            if (!string.IsNullOrEmpty(sensor.Plugin.AssetGuid))
                            {
                                offlineCandidate = Config.Sensors.FirstOrDefault(p => p.AssetGuid == sensor.Plugin.AssetGuid);
                            }
                            if (offlineCandidate == null && !string.IsNullOrEmpty(sensor.Name))
                            {
                                offlineCandidate = Config.Sensors.FirstOrDefault(p => p.Name == sensor.Name);
                            }
                            if (offlineCandidate != null)
                            {
                                sensor.Plugin.AssetGuid = offlineCandidate.AssetGuid;
                                Debug.Log($"Updated details for sensor {sensor.Name}:  AssetGuid {offlineCandidate.AssetGuid} name: {offlineCandidate.Name}");
                                continue;
                            }

                            PluginDetailData candidate = null;
                            if (!string.IsNullOrEmpty(sensor.Plugin.Id))
                            {
                                candidate = sensorsLibrary.FirstOrDefault(p => p.Id == sensor.Plugin.Id);
                            }
                            if (!string.IsNullOrEmpty(sensor.Type))
                            {
                                candidate = sensorsLibrary.FirstOrDefault(p => p.Type == sensor.Type);
                            }
                            if (candidate == null && !string.IsNullOrEmpty(sensor.Name))
                            {
                                candidate = sensorsLibrary.FirstOrDefault(p => p.Name == sensor.Name);
                            }

                            if (candidate != null)
                            {
                                sensor.Type = candidate.Type;
                                sensor.Plugin.Type = candidate.Type;
                                sensor.Plugin.AssetGuid = candidate.AssetGuid;
                                Debug.Log($"Updated details for sensor {sensor.Name}: Type: {candidate.Type} (assetguid {candidate.AssetGuid})");
                            }
                            else
                            {
                                Debug.LogError($"Could not find suitable sensor in My Library for {sensor.Name}");
                            }
                        }
                    }
                    SensorScratchPad = JsonConvert.SerializeObject(EgoVehicle.Sensors, JsonSettings.camelCasePretty);
                }
                catch (Exception e)
                {
                    Debug.Log($"Failed update complete sensor configuration: {e.Message}");
                }
            }
        }

        async void UpdateAsset()
        {
            try
            {
                SaveAssetTime = float.MaxValue;
                updating = true;
                await UpdateCloudVehicleDetails();
                Settings.developerSimulationJson = JsonConvert.SerializeObject(DeveloperSimulation, JsonSettings.camelCase);
                EditorUtility.SetDirty(Settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Saved DeveloperSettings.");
            }
            finally
            {
                updating = false;
            }
        }

        private void OnLostFocus()
        {
            if (Time.realtimeSinceStartup >= SaveAssetTime)
            {
                UpdateAsset();
            }
        }

        async void LinkTask(SimulatorInfo simInfo)
        {
            try
            {
                var stream = await API.Connect(simInfo);
                string line;
                using var reader = new StreamReader(stream);
                while (true)
                {
                    var lineTask = reader.ReadLineAsync();
                    if (await Task.WhenAny(lineTask, Task.Delay(30000)) != lineTask)
                    {
                        Debug.Log("Took to long to link to cluster, aborting");
                        return;
                    }
                    line = lineTask.Result;

                    if (line == null)
                    {
                        break;
                    }

                    if (line.StartsWith("data:") && !string.IsNullOrEmpty(line.Substring(6)))
                    {
                        JObject deserialized = JObject.Parse(line.Substring(5));
                        if (deserialized != null && deserialized.HasValues)
                        {
                            var status = deserialized.GetValue("status");
                            if (status != null)
                            {
                                switch (status.ToString())
                                {
                                    case "Unrecognized":
                                        Application.OpenURL(Config.CloudUrl + "/clusters/link?token=" + simInfo.linkToken);
                                        break;
                                    case "OK":
                                        Debug.Log("appear to be linked!");
                                        Refresh();
                                        linked = true;
                                        return;
                                    default:
                                        return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("error linking editor instance to wise");
                Debug.LogException(e);
            }
            finally
            {
                Debug.Log("closing api");
                API.Disconnect();
            }
        }
    }
}
