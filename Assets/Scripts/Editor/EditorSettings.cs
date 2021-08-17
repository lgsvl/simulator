/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Editor
{
    [CreateAssetMenu(fileName = "EditorSettings", menuName = "Simulator/Editor Settings")]
    public class EditorSettings : ScriptableObject
    {
        public static EditorSettings Load()
        {
            return Resources.Load<EditorSettings>("Editor/EditorSettings");
        }

        public GameObject MapTrafficSignalPrefab;

        public GameObject MapStopSignPrefab;
    }
}
