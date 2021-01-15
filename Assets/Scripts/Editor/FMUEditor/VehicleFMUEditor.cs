/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/

using UnityEngine;
using UnityEditor;
using Simulator.Editor;
using Simulator.FMU;

[CustomEditor(typeof(VehicleFMU), true, isFallback = true)]
public class VehicleFMUEditor : Editor
{
    Vector2 scrollPos;

    public override void OnInspectorGUI()
    {
        VehicleFMU fmu = (VehicleFMU)target;

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((VehicleFMU)target), typeof(VehicleFMU), false);
        GUI.enabled = true;
        SerializedObject serializedObject = new SerializedObject(fmu);
        serializedObject.Update();
        GUILayout.Space(5);

        if (!Application.isPlaying)
        {
            if (ShowBool(serializedObject.FindProperty("UnitySolver")))
            {
                ShowAxleInfo(serializedObject.FindProperty("Axles"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CenterOfMass"));
            }
        }
        GUILayout.Space(5);

        if (!Application.isPlaying)
        {
            if (GUILayout.Button(fmu.FMUData.modelVariables != null ? "Clear FMU Data" : "Import FMU", new GUIStyle(GUI.skin.button), GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false)))
            {
                fmu.FMUData = fmu.FMUData.modelVariables != null ? null : FMUImporter.ImportFMU(fmu.transform.name);
            }
        }
        GUILayout.Space(5);

        if (fmu.FMUData == null)
            return;

        if (fmu.FMUData.modelVariables != null)
        {
            ShowScalar(serializedObject.FindProperty("FMUData"));
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(fmu);
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }
    }

    private bool ShowBool(SerializedProperty enabled)
    {
        return enabled.boolValue = GUILayout.Toggle(enabled.boolValue, enabled.boolValue ? "Unity Physics" : "Non Unity Physics", new GUIStyle(GUI.skin.button), GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false));
    }

    private void ShowAxleInfo(SerializedProperty list)
    {
        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
            for (int i = 0; i < list.arraySize; i++)
            {
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
            }
        }
        EditorGUI.indentLevel -= 1;
    }

    private void ShowScalar(SerializedProperty data)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("FMU Name");
        EditorGUILayout.LabelField(data.FindPropertyRelative("Name").stringValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("FMU Version");
        EditorGUILayout.LabelField(data.FindPropertyRelative("Version").stringValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("FMU GUID");
        EditorGUILayout.LabelField(data.FindPropertyRelative("GUID").stringValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("FMU Model Name");
        EditorGUILayout.LabelField(data.FindPropertyRelative("modelName").stringValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("FMU Type");
        EditorGUILayout.LabelField(((FMIType)data.FindPropertyRelative("type").enumValueIndex).ToString());
        EditorGUILayout.EndHorizontal();

        var modelVars = data.FindPropertyRelative("modelVariables");
        EditorGUILayout.PropertyField(modelVars);

        var style = new GUIStyle { alignment = TextAnchor.MiddleRight };

        if (modelVars.isExpanded)
        {
            using (var h = new EditorGUILayout.VerticalScope())
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos, false, true, GUILayout.Height(200)))
                {
                    scrollPos = scrollView.scrollPosition;
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField($"Vehicle FMU index");
                    EditorGUILayout.LabelField($"FMU value reference");
                    EditorGUILayout.LabelField($"Description");
                    EditorGUILayout.LabelField($"Causality");
                    EditorGUILayout.LabelField($"Variability");
                    EditorGUILayout.LabelField($"Initial");
                    EditorGUILayout.LabelField($"Type");
                    EditorGUILayout.LabelField($"Start");
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel -= 1;

                    for (int i = 0; i < modelVars.arraySize; i++)
                    {
                        var listElement = modelVars.GetArrayElementAtIndex(i);
                        EditorGUI.indentLevel += 1;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{i}");
                        EditorGUILayout.LabelField($"{listElement.FindPropertyRelative("name").stringValue}");
                        EditorGUILayout.LabelField($"{listElement.FindPropertyRelative("description").stringValue}");
                        EditorGUILayout.LabelField($"{listElement.FindPropertyRelative("causality").stringValue}");
                        EditorGUILayout.LabelField($"{listElement.FindPropertyRelative("variability").stringValue}");
                        EditorGUILayout.LabelField($"{listElement.FindPropertyRelative("initial").stringValue}");
                        EditorGUILayout.LabelField($"{((VariableType)listElement.FindPropertyRelative("type").enumValueIndex).ToString()}");
                        EditorGUILayout.LabelField($"{listElement.FindPropertyRelative("start").stringValue}");
                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
        }
    }
}