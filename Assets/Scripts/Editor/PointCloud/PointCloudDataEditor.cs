/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using Simulator.PointCloud;

namespace Simulator.Editor.PointCloud
{
    [CustomEditor(typeof(PointCloudData))]
    public class PointCloudDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var data = target as PointCloudData;

            EditorGUILayout.LabelField("Asset Size", EditorUtility.FormatBytes(data.Count * data.Stride));
            EditorGUILayout.LabelField("Point Count", data.Count.ToString("N0"));
            EditorGUILayout.LabelField("Has Color", data.HasColor.ToString());
            EditorGUILayout.LabelField("Center", data.OriginalCenter.ToString());
            EditorGUILayout.LabelField("Extents", data.OriginalExtents.ToString());
        }
    }
}
