/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PropertyDrawers
{
    using System.IO;
    
    using Simulator.Utilities;

    using UnityEditor;
    using UnityEngine;
    using Utilities.Attributes;

    [CustomPropertyDrawer(typeof(PathSelectorAttribute))]
    public class PathSelectorAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!(attribute is PathSelectorAttribute attr))
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (property.propertyType != SerializedPropertyType.String)
                return;

            var buttonPos = position;
            buttonPos.width = 22;
            buttonPos.x = position.xMax - 22;
            var fieldPos = position;
            fieldPos.width -= 27;

            var currentPath = Utility.GetFullPath(property.stringValue);

            var pathValid = !string.IsNullOrEmpty(currentPath)
                            && (attr.SelectDirectory ? Directory.Exists(currentPath) : File.Exists(currentPath));

            var color = GUI.color;
            if (!pathValid)
                GUI.color = Color.red;

            EditorGUI.PropertyField(fieldPos, property, label);

            GUI.color = color;

            if (GUI.Button(buttonPos, "..."))
            {
                var startingPath = pathValid ? Path.GetDirectoryName(currentPath) : Application.dataPath;

                var newPath = attr.SelectDirectory
                    ? EditorUtility.OpenFolderPanel("Select directory", startingPath, string.Empty)
                    : EditorUtility.OpenFilePanel("Select file", startingPath, attr.AllowedExtensions);

                if (!string.IsNullOrEmpty(newPath))
                {
                    property.stringValue = attr.TruncateToRelative
                        ? Utility.GetRelativePathIfApplies(newPath)
                        : Utility.GetForwardSlashPath(newPath);
                }
            }
        }
    }
}