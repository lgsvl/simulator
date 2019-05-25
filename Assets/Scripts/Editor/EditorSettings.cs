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

        [Tooltip("Lidar sensor prefab to use for point cloud generation")]
        public LidarSensor LidarSensor;
    }
}
