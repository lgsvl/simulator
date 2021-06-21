/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor
{
    using Components;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(RainCollider))]
    public class RainColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var current = target as RainCollider;
            if (current == null)
                return;

            EditorGUILayout.Space(10);

            var res = current.textureSize;

            if (res != default)
            {
                var pixels = res.x * res.y * res.z;
                var size = pixels;
                var limit = current.memoryLimit * 1024 * 1024;
                var sizeStr = size > 1024 * 1024 ? $"{size / 1024f / 1024f:F2} MB" : $"{size / 1024f:F2} KB";

                EditorGUILayout.HelpBox($"Resolution: {res.x}x{res.y}x{res.z}\nMemory: {sizeStr}", MessageType.None);

                if (size > limit)
                    EditorGUILayout.HelpBox($"Memory limit ({current.memoryLimit} MB) exceeded. Collider won't work.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox($"Resolution: UNKNOWN\nMemory: UNKNOWN", MessageType.None);
            }

            if (GUILayout.Button("Calculate size"))
            {
                current.RecalculateSize();
            }
        }
    }
}