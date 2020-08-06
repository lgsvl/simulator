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
using Newtonsoft.Json.Linq;

namespace Simulator.Editor
{
    public class DevelopmentSettingsWindow : EditorWindow
    {
        List<VehicleDetailData> CloudVehicles = new List<VehicleDetailData>();
        List<string> LocalVehicles = new List<string>();

        DevelopmentSettingsAsset settings;
        SimulationData developerSimulation = null;

        CloudAPI API;
        string errorMessage;
        private static DevelopmentSettingsWindow _instance;
        int currentVehicleIndex = 0;
        string sensorScratchPad = "";

        Vector2 ScrollPos;

        [MenuItem("Simulator/Development Settings...", false, 50)]
        public static void Open()
        {
            var window = GetWindow<DevelopmentSettingsWindow>();
            window.titleContent = new GUIContent("Development Settings");
            window.Show();
        }

        void OnEnable()
        {
            _instance = this;
            EditorApplication.playModeStateChanged -= HandlePlayMode;
            EditorApplication.playModeStateChanged += HandlePlayMode;
            settings = (DevelopmentSettingsAsset)AssetDatabase.LoadAssetAtPath("Assets/Resources/Editor/DeveloperSettings.asset", typeof(DevelopmentSettingsAsset));
            if (settings == null)
            {
                settings = (DevelopmentSettingsAsset)CreateInstance(typeof(DevelopmentSettingsAsset));
                Debug.Log("Initialized new developer settings");
                AssetDatabase.CreateAsset(settings, "Assets/Resources/Editor/DeveloperSettings.asset");
            }

            if (settings.developerSimulationJson != null)
            {
                developerSimulation = Newtonsoft.Json.JsonConvert.DeserializeObject<SimulationData>(settings.developerSimulationJson);
            }

            if (developerSimulation == null)
            {
                developerSimulation = new SimulationData()
                {
                    Name = "DeveloperSettings",
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

        void OnDestroy()
        {
            if (API != null)
            {
                API.Disconnect();
            }
            _instance = null;
        }

        private static void HandlePlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && _instance != null && _instance.API != null)
            {
                _instance.API.Disconnect();
            }
            if (state == PlayModeStateChange.ExitingPlayMode && _instance != null)
            {
                _instance.Refresh();
            }
        }

        async void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                errorMessage = "";
                Simulator.Web.Config.ParseConfigFile();

                Simulator.Database.DatabaseManager.Init();
                var csservice = new Simulator.Database.Services.ClientSettingsService();
                ClientSettings cls = csservice.GetOrMake();
                Config.SimID = cls.simid;
                if (String.IsNullOrEmpty(Config.CloudUrl))
                {
                    errorMessage = "Cloud URL not set";
                    return;
                }
                if (String.IsNullOrEmpty(Config.SimID))
                {
                    errorMessage = "Simulator not linked";
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

                string idOrPath = null;
                if (developerSimulation.Vehicles != null) // get previously selected thing
                {
                    // we abuse VehicleData.Id to store the prefab path
                    idOrPath = developerSimulation.Vehicles[0].Id;
                }

                if (idOrPath != null)
                {
                    // find index of previously selected thing in new dataset
                    var vehicleChoices = LocalVehicles.Select(g => g).Concat(
                    CloudVehicles.Select(v => v.Id)).ToList();
                    currentVehicleIndex = vehicleChoices.FindIndex(v => v == idOrPath);
                    // vehicle was previously selected but no longer available, need to keep something selected
                    if (currentVehicleIndex < 0)
                    {
                        currentVehicleIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += "\n" + ex.InnerException.Message;
                }
                API.Disconnect();
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/External/Vehicles" });
            LocalVehicles = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToList();
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Cloud URL: " + Config.CloudUrl, MessageType.Info);
            EditorGUILayout.HelpBox("SimID: " + Config.SimID, MessageType.None);

            if (!String.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Disabled during play mode", MessageType.Info);
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
                if (prop.Name == "Id" || prop.Name == "UpdatedAt" || prop.Name == "CreatedAt" || prop.Name == "OwnerId")
                    continue;

                object value = prop.GetValue(developerSimulation);
                if (prop.PropertyType == typeof(bool))
                    prop.SetValue(developerSimulation, EditorGUILayout.ToggleLeft(prop.Name, ((bool?)value).Value));
                else if (prop.PropertyType == typeof(int))
                    prop.SetValue(developerSimulation, EditorGUILayout.IntField(prop.Name, ((int?)value).Value));
                else if (prop.PropertyType == typeof(float))
                    prop.SetValue(developerSimulation, EditorGUILayout.FloatField(prop.Name, ((float?)value).Value));
                else if (prop.PropertyType == typeof(string))
                    prop.SetValue(developerSimulation, EditorGUILayout.TextField(prop.Name, (string)value));
                else if (prop.PropertyType == typeof(DateTime))
                {
                    var userValue = EditorGUILayout.TextField(prop.Name, ((DateTime)value).ToString());
                    if (DateTime.TryParse(userValue, out DateTime dt))
                    {
                        prop.SetValue(developerSimulation, dt);
                    }
                }
                else if (prop.PropertyType == typeof(int?))
                {
                    int? val = (int?)value;
                    EditorGUILayout.BeginHorizontal();
                    bool doSet = EditorGUILayout.ToggleLeft("enable " + prop.Name, val.HasValue);
                    EditorGUI.BeginDisabledGroup(!doSet);
                    int? newValue = EditorGUILayout.IntField(prop.Name, val.HasValue ? val.Value : 0, GUILayout.ExpandWidth(true));
                    prop.SetValue(developerSimulation, doSet ? newValue : null);
                    EditorGUI.EndDisabledGroup();
                    GUILayout.EndHorizontal();
                }
                else if (prop.PropertyType == typeof(VehicleData[]))
                {
                    // a list of tuples (display:"name to show", data:string or VehicleDetailData )
                    var vehicleChoices = LocalVehicles.Select(g => (data: (object)g, display: "local: " + Path.GetFileName(g))).Concat(
                        CloudVehicles.Select(v => (data: (object)v, display: "cloud: " + v.Name))
                    ).ToList();

                    if (vehicleChoices.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        currentVehicleIndex = EditorGUILayout.Popup(currentVehicleIndex, vehicleChoices.Select(v => v.display).ToArray());

                        if (currentVehicleIndex > vehicleChoices.Count - 1)
                            return;

                        object selection = vehicleChoices[currentVehicleIndex].data;

                        if (selection.GetType() == typeof(string))
                        {
                            if (developerSimulation.Vehicles == null)
                            {
                                developerSimulation.Vehicles = new VehicleData[]
                                {
                                    new VehicleData()
                                };
                            }

                            var vehicle = developerSimulation.Vehicles[0];
                            vehicle.Id = (string)selection;
                            if (vehicle.bridge == null)
                            {
                                vehicle.bridge = new BridgeData();
                            }

                            vehicle.bridge.type = EditorGUILayout.TextField("Bridge Type", vehicle.bridge.type);
                            vehicle.bridge.connectionString = EditorGUILayout.TextField("Bridge Connection", vehicle.bridge.connectionString);

                            EditorGUILayout.LabelField("json sensor config");
                            sensorScratchPad = EditorGUILayout.TextArea(sensorScratchPad, GUILayout.Height(200));

                            try
                            {
                                vehicle.Sensors = Newtonsoft.Json.JsonConvert.DeserializeObject<SensorData[]>(sensorScratchPad);
                            }
                            catch (Exception e)
                            {
                                EditorGUILayout.HelpBox(e.Message, MessageType.Error);
                            }
                        }
                        else if (selection.GetType() == typeof(VehicleDetailData))
                        {
                            // want cloud vehicle, so clear local vehicle
                            developerSimulation.Vehicles = new VehicleData[] { ((VehicleDetailData)selection).ToVehicleData() };
                        }
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("no Vehicles in Cloud Library or Assets/External/Vehicles", MessageType.Info);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                updateAsset();
            }
        }

        async void updateAsset()
        {
            if (developerSimulation.Vehicles != null && !developerSimulation.Vehicles[0].Id.EndsWith(".prefab"))
            {
                // vehicle list does not give us sensor data, so we have to get it later.
                // I do not want to query each vehicle individually and I can't block the UI, so
                // we do it after a change was made to the settings in this fire and forget async function
                var data = await API.Get<VehicleDetailData>(developerSimulation.Vehicles[0].Id);
                developerSimulation.Vehicles = new VehicleData[] { data.ToVehicleData() };
            }
            settings.developerSimulationJson = Newtonsoft.Json.JsonConvert.SerializeObject(developerSimulation);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
