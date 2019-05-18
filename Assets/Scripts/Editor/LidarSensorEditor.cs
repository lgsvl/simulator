/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using UnityEditor;
using Simulator.Sensors;

namespace Simulator.Editor
{
    [CustomEditor(typeof(LidarSensor))]
    class LidarSensorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var lidar = (LidarSensor)target;

            var choices = LidarSensor.Template.Templates.Select(x => x.Name).ToArray();

            lidar.TemplateIndex = EditorGUILayout.Popup("Lidar Template: ", lidar.TemplateIndex, choices);
            if (lidar.TemplateIndex != 0)
            {
                lidar.ApplyTemplate();
                EditorUtility.SetDirty(lidar);
            }

            base.OnInspectorGUI();
        }
    }
}
