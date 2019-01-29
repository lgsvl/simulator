/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapIntersectionBuilder)), CanEditMultipleObjects]
public class MapIntersectionBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapIntersectionBuilder mapIntersectionBuilder = (MapIntersectionBuilder)target;

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        //if (GUILayout.Button("Link Intersection Mesh"))
        //{
        //    Undo.RecordObject(mapIntersectionBuilder, "change builder");
        //    mapIntersectionBuilder.GetIntersection();
        //}
    }

    protected virtual void OnSceneGUI()
    {
        //placeholder
    }
}
