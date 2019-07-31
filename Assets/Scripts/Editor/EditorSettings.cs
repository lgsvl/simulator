/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Sensors;

namespace Simulator.Editor
{
    [CreateAssetMenu(fileName = "EditorSettings", menuName = "Simulator/Editor Settings")]
    public class EditorSettings : ScriptableObject
    {
        public static EditorSettings Load()
        {
            return Resources.Load<EditorSettings>("Editor/EditorSettings");
        }

        [Tooltip("LiDAR sensor prefab is used to generate point cloud")]
        public LidarSensor LidarSensor;

        public GameObject MapTrafficSignalPrefab;

        public GameObject MapStopSignPrefab;
    }
}
