/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;
using Simulator.Web;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Simulator.Editor
{
    [CreateAssetMenu(fileName = "DevelopmentSettings", menuName = "Simulator/Development Settings")]
    public class DevelopmentSettingsAsset: ScriptableObject
    {
        [System.Serializable]
        public class LocalVehicle
        {
            public string PrefabPath;
            public string SensorConfig;
            public string BridgeName;
            public string BridgeConnection;
        }
        public LocalVehicle localVehicle = null;
        public string developerSimulationJson;
    }
}