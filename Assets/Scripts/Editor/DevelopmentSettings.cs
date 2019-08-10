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

namespace Simulator.Editor
{
    public class DevelopmentSettings : EditorWindow
    {
        List<VehicleModel> Vehicles;

        [SerializeField]
        bool CreateVehicle;

        [SerializeField]
        string VehicleName;

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
            try
            {
                using (var db = DatabaseManager.GetConfig(DatabaseManager.GetConnectionString()).Create())
                {
                    Vehicles = db.Fetch<VehicleModel>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Vehicles = new List<VehicleModel>();
            }

            if (string.IsNullOrEmpty(VehicleName) && Vehicles.Count != 0)
            {
                VehicleName = Vehicles[0].Name;
            }
        }

        void OnGUI()
        {
            if (Vehicles == null || Vehicles.Count == 0)
            {
                EditorGUILayout.HelpBox("No vehicles found in database, please create at least one with WebUI", MessageType.Warning);
            }
            else
            {
                GUILayout.BeginHorizontal();
                CreateVehicle = GUILayout.Toggle(CreateVehicle, "Create vehicle: ");

                EditorGUI.BeginDisabledGroup(!CreateVehicle);

                int selected = Vehicles.FindIndex(v => v.Name == VehicleName);
                selected = EditorGUILayout.Popup(selected, Vehicles.Select(v => v.Name).ToArray(), GUILayout.ExpandWidth(true));

                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();

                if (selected >= 0 && selected < Vehicles.Count)
                {
                    var vehicle = Vehicles[selected];
                    VehicleName = vehicle.Name;

                    if (!string.IsNullOrEmpty(vehicle.BridgeType))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{vehicle.BridgeType} Bridge:");
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

            if (GUILayout.Button("Apply"))
            {
                var data = JsonUtility.ToJson(this, false);
                EditorPrefs.SetString("Simulator/DevelopmentSettings", data);
            }
        }
    }
}
