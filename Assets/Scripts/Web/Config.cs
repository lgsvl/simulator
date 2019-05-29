/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using Simulator.Utilities;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Simulator.Web
{
    public static class Config
    {
        public static string Root;
        public static string PersistentDataPath;

        public static List<SensorConfig> Sensors;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        static void Initialize()
        {
            Root = Path.Combine(Application.dataPath, "..");
            PersistentDataPath = Application.persistentDataPath;
            Sensors = SensorTypes.ListSensorFields(RuntimeSettings.Instance.SensorPrefabs);
        }
    }
}
