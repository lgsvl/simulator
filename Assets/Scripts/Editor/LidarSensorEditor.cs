/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using UnityEditor;

[CustomEditor(typeof(LidarSensor))]
class LidarSensorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var lidar = (LidarSensor)target;

        var choices = LidarTemplate.Templates.Select(x => x.Name).ToArray();

        lidar.Template = EditorGUILayout.Popup("Lidar Template: ", lidar.Template, choices);
        if (lidar.Template != 0)
        {
            lidar.ApplyTemplate();
            EditorUtility.SetDirty(lidar);
        }

        base.OnInspectorGUI();
    }
}
