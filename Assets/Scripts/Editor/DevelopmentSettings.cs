/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PetaPoco;
using Simulator.Database;
using Simulator.Web;

namespace Simulator.Editor
{
    public class DevelopmentSettings : EditorWindow
    {
        List<VehicleDetailData> Vehicles = new List<VehicleDetailData>();

        [SerializeField]
        bool CreateVehicle;

        [SerializeField]
        string VehicleCloudId;

        [SerializeField]
        string Connection = "localhost:9090";

        [SerializeField]
        bool EnableAPI;

        [SerializeField]
        bool UseSeed;

        [SerializeField]
        int Seed;

        [SerializeField]
        bool EnableNPCs;

        [SerializeField]
        bool EnablePEDs;
        CloudAPI API;
        string errorMessage;

        [MenuItem("Simulator/Development Settings...", false, 50)]
        public static void Open()
        {
            var window = GetWindow<DevelopmentSettings>();
            var data = EditorPrefs.GetString("Simulator/DevelopmentSettings", JsonUtility.ToJson(window, false));
            JsonUtility.FromJsonOverwrite(data, window);
            window.titleContent = new GUIContent("Development Settings");
            window.Show();
        }

        void OnEnable()
        {
            Refresh();
        }

        async void Refresh()
        {
            try 
            {
                errorMessage = "";
                Simulator.Web.Config.ParseConfigFile();

                Simulator.Database.DatabaseManager.Init();
                var csservice = new Simulator.Database.Services.ClientSettingsService();
                ClientSettings settings = csservice.GetOrMake();
                Config.SimID = settings.simid;
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

                API = new CloudAPI(new Uri(Config.CloudUrl), settings.simid);
                var ret = await API.GetLibrary<VehicleDetailData>();
                Vehicles = ret.ToList();
                if (!Vehicles.Any(v => v.Id == VehicleCloudId) && Vehicles.Count > 0)
                {
                    VehicleCloudId = Vehicles[0].Name;
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                errorMessage = ex.Message;
                if(ex.InnerException != null)
                    errorMessage += "\n"+ex.InnerException.Message;
            }
        }
        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.HelpBox("Cloud URL: "+Config.CloudUrl, MessageType.Info);

            if(!String.IsNullOrEmpty(errorMessage))
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);


            if (GUILayout.Button("Refresh"))
                Refresh();

            if (Vehicles.Count > 0)
            {
                GUILayout.BeginHorizontal();
                CreateVehicle = GUILayout.Toggle(CreateVehicle, "Create vehicle: ");

                EditorGUI.BeginDisabledGroup(!CreateVehicle);

                int selected = Vehicles.FindIndex(v => v.Id == VehicleCloudId);
                selected = EditorGUILayout.Popup(selected, Vehicles.Select(v => v.Name).ToArray(), GUILayout.ExpandWidth(true));

                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();

                if (selected >= 0 && selected < Vehicles.Count)
                {
                    var vehicle = Vehicles[selected];
                    VehicleCloudId = vehicle.Id;

                    if (vehicle.bridge != null && !string.IsNullOrEmpty(vehicle.bridge.type))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{vehicle.bridge.type} Bridge:");
                        Connection = GUILayout.TextField(Connection, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();
                    }
                }
            }

            GUILayout.BeginHorizontal();
            UseSeed = GUILayout.Toggle(UseSeed, "Use predefined seed: ");
            EditorGUI.BeginDisabledGroup(!UseSeed);
            Seed = EditorGUILayout.IntField(Seed, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            EnableAPI = GUILayout.Toggle(EnableAPI, "Enable API");
            EnableNPCs = GUILayout.Toggle(EnableNPCs, "Enable NPCs");
            EnablePEDs = GUILayout.Toggle(EnablePEDs, "Enable Pedestrians");

            if (EditorGUI.EndChangeCheck())
            {
                var data = JsonUtility.ToJson(this, false);
                EditorPrefs.SetString("Simulator/DevelopmentSettings", data);
            }
        }
    }
}
