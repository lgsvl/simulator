/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using Simulator.Database;
using Simulator.Web;
using Newtonsoft.Json;

namespace Simulator.Editor
{
    public class DevelopmentSettingsWindow : EditorWindow
    {
        private List<VehicleDetailData> CloudVehicles = new List<VehicleDetailData>();
        private List<string> LocalVehicles = new List<string>();

        private DevelopmentSettingsAsset Settings;
        private SimulationData DeveloperSimulation = null;

        private CloudAPI API;
        private string ErrorMessage;
        private static DevelopmentSettingsWindow Instance;
        private int CurrentVehicleIndex = 0;
        private string SensorScratchPad = "";

        private Vector2 ScrollPos;
        private bool VehicleSetup = false;
        private bool NPCSelectEnable = false;
        private float SaveAssetTime = float.MaxValue;

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
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.update += OnEditorUpdate;

            Settings = (DevelopmentSettingsAsset)AssetDatabase.LoadAssetAtPath("Assets/Resources/Editor/DeveloperSettings.asset", typeof(DevelopmentSettingsAsset));
            if (Settings == null)
            {
                Settings = (DevelopmentSettingsAsset)CreateInstance(typeof(DevelopmentSettingsAsset));
                Debug.Log("Initialized new developer settings");
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
                    Cluster = new ClusterData()
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
                    }
                };
            }

            Refresh();
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }

        public void OnBeforeAssemblyReload()
        {
            API.Disconnect();
        }

        void OnDestroy()
        {
            if (API != null)
            {
                API.Disconnect();
            }
            Instance = null;
        }

        protected virtual void OnEditorUpdate()
        {
            if (Time.realtimeSinceStartup > SaveAssetTime)
            {
                UpdateAsset();
            }
        }

        private static void HandlePlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && Instance != null && Instance.API != null)
            {
                Instance.API.Disconnect();
            }
            if (state == PlayModeStateChange.ExitingPlayMode && Instance != null)
            {
                Instance.Refresh();
            }
        }

        bool updating = false;

        List<(string idOrPath, object data, string display)> VehicleChoices =>
                             LocalVehicles.Select(g => (idOrPath: g, data: (object)g, display: "local: " + Path.GetFileName(g))).Concat(
                             CloudVehicles.Select(v => (idOrPath: v.Id, data: (object)v, display: "cloud: " + v.Name)))
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

                DatabaseManager.Init();
                var csservice = new Simulator.Database.Services.ClientSettingsService();
                ClientSettings cls = csservice.GetOrMake();
                Config.SimID = cls.simid;
                if (String.IsNullOrEmpty(Config.CloudUrl))
                {
                    ErrorMessage = "Cloud URL not set";
                    return;
                }
                if (String.IsNullOrEmpty(Config.SimID))
                {
                    ErrorMessage = "Simulator not linked";
                    return;
                }

                if (API != null)
                {
                    API.Disconnect();
                }

                API = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID);
                var simInfo = CloudAPI.GetInfo();

                var reader = await API.Connect(simInfo);
                await API.EnsureConnectSuccess();
                var ret = await API.GetLibrary<VehicleDetailData>();
                CloudVehicles = ret.ToList();

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/External/Vehicles" });
                LocalVehicles = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToList();

                string idOrPath = null;
                if (DeveloperSimulation.Vehicles != null) // get previously selected thing
                {
                    // we abuse VehicleData.Id to store the prefab path
                    idOrPath = DeveloperSimulation.Vehicles[0].Id;
                }

                if (idOrPath != null)
                {
                    // find index of previously selected thing in new dataset
                    var vehicleChoices = VehicleChoices;
                    var selected = vehicleChoices.FindIndex(v => v.idOrPath == idOrPath);
                    SetVehicleFromSelectionIndex(vehicleChoices, selected);

                    await UpdateCloudVehicleDetails();
                }

                DeveloperSimulation.NPCs = Config.NPCVehicles.Values.ToArray(); // TODO get from cloud and refresh config.cs LoadExternalAssets()
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ErrorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    ErrorMessage += "\n" + ex.InnerException.Message;
                }
                API.Disconnect();
            }
            finally
            {
                updating = false;
                Repaint();
            }

        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Cloud URL: " + Config.CloudUrl, MessageType.Info);
            EditorGUILayout.HelpBox("SimID: " + Config.SimID, MessageType.None);

            if (!String.IsNullOrEmpty(ErrorMessage))
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

            if (GUILayout.Button("Disconnect"))
            {
                API.Disconnect();
            }

            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }

            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
            foreach (PropertyInfo prop in typeof(SimulationData).GetProperties())
            {
                if (prop.Name == "Id" || prop.Name == "UpdatedAt" || prop.Name == "CreatedAt" || prop.Name == "OwnerId" || prop.Name == "TestReportId")
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
                    VehicleSetup = EditorGUILayout.Foldout(VehicleSetup, "EGO Setup");
                    if (VehicleSetup)
                    {
                        // a list of tuples (display:"name to show", data:string or VehicleDetailData )
                        var vehicleChoices = VehicleChoices;

                        if (vehicleChoices.Count > 0)
                        {
                            EditorGUI.indentLevel++;
                            var newIndex = EditorGUILayout.Popup(CurrentVehicleIndex, vehicleChoices.Select(v => v.display).ToArray());
                            var vehicle = SetVehicleFromSelectionIndex(vehicleChoices, newIndex);

                            if (vehicle.Id.EndsWith(".prefab"))
                            {
                                vehicle.Bridge.Type = EditorGUILayout.TextField("Bridge Type", vehicle.Bridge.Type);
                                vehicle.Bridge.ConnectionString = EditorGUILayout.TextField("Bridge Connection", vehicle.Bridge.ConnectionString);

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
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.TextField("Bridge Type", vehicle.Bridge.Type);
                                    EditorGUILayout.TextField("Bridge Name", vehicle.Bridge.Name);
                                    EditorGUILayout.TextField("Bridge AssetGuid", vehicle.Bridge.AssetGuid);
                                    EditorGUILayout.LabelField("json sensor config");
                                    EditorGUILayout.TextArea(Newtonsoft.Json.JsonConvert.SerializeObject(vehicle.Sensors, JsonSettings.camelCase), GUILayout.Height(200));
                                    EditorGUI.EndDisabledGroup();
                                }
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
                SaveAssetTime = Time.realtimeSinceStartup + .25f;
            }
        }

        private VehicleData SetVehicleFromSelectionIndex(List<(string idOrPath, object data, string display)> vehicleChoices, int newSelectionIndex)
        {
            VehicleData previousVehicle = DeveloperSimulation.Vehicles?[0];

            if (newSelectionIndex < 0 || newSelectionIndex > vehicleChoices.Count)
            {
                Debug.Log("previously selected vehicle missing.");
                newSelectionIndex = 0;
            }
            CurrentVehicleIndex = newSelectionIndex;
            object selection = vehicleChoices[CurrentVehicleIndex].data;
            VehicleData vehicle;

            if (selection.GetType() == typeof(string))
            {
                if (previousVehicle != null && (string)selection == previousVehicle.Id)
                {
                    return previousVehicle;
                }

                vehicle = new VehicleData
                {
                    Id = (string)selection,
                    Bridge = previousVehicle?.Bridge ?? new BridgeData()
                };
            }
            else // Cloud Vehicle
            {
                vehicle = ((VehicleDetailData)selection).ToVehicleData();
                if (previousVehicle != null && vehicle.Id == previousVehicle.Id)
                {
                    return previousVehicle;
                }
            }

            DeveloperSimulation.Vehicles = new VehicleData[] { vehicle };
            return vehicle;
        }

        async Task UpdateCloudVehicleDetails()
        {
            if (API == null)
                return;

            if (DeveloperSimulation.Vehicles == null) return;

            if (!DeveloperSimulation.Vehicles[0].Id.EndsWith(".prefab"))
            {
                try
                {
                    // vehicle list does not give us sensor data, so we have to get it later.
                    // I do not want to query each vehicle individually and I can't block the UI, so
                    // we do it after a change was made to the settings in this fire and forget async function
                    var data = await API.Get<VehicleDetailData>(DeveloperSimulation.Vehicles[0].Id);
                    // copy previous bridge data as it is not saved with the vehicle
                    if (DeveloperSimulation.Vehicles[0].Bridge != null)
                    {
                        data.Bridge.ConnectionString = DeveloperSimulation.Vehicles[0].Bridge.ConnectionString;
                    }
                    DeveloperSimulation.Vehicles = new VehicleData[] { data.ToVehicleData() };

                    // splice in fetched data (containing extended data like bridge) for matching id
                    CloudVehicles = CloudVehicles.Select(v => v.Id == DeveloperSimulation.Vehicles[0].Id ? data : v).ToList();
                }
                catch (Exception e)
                {
                    Debug.Log($"Failed to get details of vehicle {DeveloperSimulation.Vehicles[0].Id}: {e.Message}");
                }
            }

            if (DeveloperSimulation.Vehicles[0].Sensors != null)
            {
                try
                {
                    PluginDetailData[] sensorsLibrary = null;

                    foreach (var sensor in DeveloperSimulation.Vehicles[0].Sensors)
                    {
                        if (sensor.Plugin == null)
                        {
                            sensor.Plugin = new SensorPlugin();
                        }

                        if (string.IsNullOrEmpty(sensor.Plugin.AssetGuid))
                        {
                            if (sensorsLibrary == null)
                            {
                                sensorsLibrary = (await API.GetLibrary<PluginDetailData>())
                                    .Where(p => p.Category == "sensor").ToArray();
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
                                Debug.LogError("Could not find suitable sensor in My Library");
                            }
                        }
                    }
                    SensorScratchPad = JsonConvert.SerializeObject(DeveloperSimulation.Vehicles[0].Sensors, JsonSettings.camelCasePretty);
                }
                catch (Exception e)
                {
                    Debug.Log($"Failed update complete sensor configuration: {e.Message}");
                }
            }
        }

        async void UpdateAsset()
        {
            SaveAssetTime = float.MaxValue;
            updating = true;
            await UpdateCloudVehicleDetails();
            Settings.developerSimulationJson = JsonConvert.SerializeObject(DeveloperSimulation, JsonSettings.camelCase);
            EditorUtility.SetDirty(Settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            updating = false;
            Debug.Log("Saved DeveloperSettings.");
        }

        private void OnLostFocus()
        {
            UpdateAsset();
        }
    }
}
