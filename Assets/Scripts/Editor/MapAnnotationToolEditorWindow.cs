/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapAnnotationToolEditorWindow : EditorWindow
{
    //private List<MapWaypoint> tempWaypoints_selected = new List<MapWaypoint>();
    //private List<MapLaneSegmentBuilder> mapLaneBuilder_selected = new List<MapLaneSegmentBuilder>();
    private GameObject parentObj;
    private LayerMask layerMask;
    private GameObject tempWaypointGO;

    [MenuItem("Simulator/Map Tool")]
    public static void MapToolPanel()
    {
        MapAnnotationToolEditorWindow window = (MapAnnotationToolEditorWindow)EditorWindow.GetWindow(typeof(MapAnnotationToolEditorWindow), false, "MapTool");
        window.Show();
    }

    private void OnGUI()
    {
        // Modes
        var labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
        GUILayout.Space(5);
        EditorGUILayout.LabelField("Map Annotation Modes", labelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);

        if (GUILayout.Button(new GUIContent("Show Map All Mode: " + MapAnnotationTool.SHOW_MAP_ALL, "Toggle all annotation in scene visible"), GUILayout.Height(25)))
        {
            ToggleMapAll();
        }
        if (GUILayout.Button(new GUIContent("Show Map Selected Mode: " + MapAnnotationTool.SHOW_MAP_SELECTED, "Toggle selected annotation in scene visible"), GUILayout.Height(25)))
        {
            ToggleMapSelected();
        }
        if (GUILayout.Button(new GUIContent("Temp Waypoint Mode: " + MapAnnotationTool.TEMP_WAYPOINT_MODE, "Enter mode to create temporary waypoints"), GUILayout.Height(25)))
        {
            ToggleTempWaypointMode();
        }

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Test", GUI.skin.horizontalSlider);
        GUILayout.Space(10);

        // waypoint
        EditorGUILayout.LabelField("Create Waypoint", labelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);
        parentObj = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Parent Object", "This object will hold all new annotation objects created"), parentObj, typeof(GameObject), true);
        LayerMask tempMask = EditorGUILayout.MaskField(new GUIContent("Ground Snap Layer Mask", "The ground and road layer to snap objects created to"),
                                                       UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask),
                                                       UnityEditorInternal.InternalEditorUtility.layers);
        layerMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
        
        if (GUILayout.Button(new GUIContent("Create Temp Map Waypoint", "Create a temporary waypoint object in scene on layer")))
        {
            CreateTempWaypoint();
        }
        if (GUILayout.Button(new GUIContent("Clear All Temp Map Waypoints", "Delete all temporary waypoints")))
        {
            ClearAllTempWaypoints();
        }
    }

    private void Update()
    {
        if (MapAnnotationTool.TEMP_WAYPOINT_MODE && tempWaypointGO != null)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, 1000.0f, layerMask.value))
            {
                tempWaypointGO.transform.position = hit.point;
            }
            SceneView.RepaintAll();
        }
    }

    private void ToggleMapAll()
    {
        MapAnnotationTool.SHOW_MAP_ALL = !MapAnnotationTool.SHOW_MAP_ALL;
        SceneView.RepaintAll();
    }

    private void ToggleMapSelected()
    {
        MapAnnotationTool.SHOW_MAP_SELECTED = !MapAnnotationTool.SHOW_MAP_SELECTED;
        SceneView.RepaintAll();
    }

    private void ToggleTempWaypointMode()
    {
        MapAnnotationTool.TEMP_WAYPOINT_MODE = !MapAnnotationTool.TEMP_WAYPOINT_MODE;

        MapAnnotationTool.SHOW_MAP_ALL = MapAnnotationTool.TEMP_WAYPOINT_MODE ? true : false;
        MapAnnotationTool.SHOW_MAP_SELECTED = MapAnnotationTool.TEMP_WAYPOINT_MODE ? true : false;
        if (MapAnnotationTool.TEMP_WAYPOINT_MODE)
        {
            CreateTempWaypoint();
        }
        else
        {
            if (tempWaypointGO != null)
                DestroyImmediate(tempWaypointGO);
            ClearAllTempWaypoints();
        }
    }

    private void CreateTempWaypoint()
    {
        var cam = SceneView.lastActiveSceneView.camera;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, layerMask.value))
        {
            tempWaypointGO = new GameObject("TEMP_WAYPOINT");
            tempWaypointGO.transform.position = hit.point;
            var waypoint = tempWaypointGO.AddComponent<MapWaypoint>();
            waypoint.layerMask = layerMask;
            Undo.RegisterCreatedObjectUndo(tempWaypointGO, nameof(tempWaypointGO));
        }
        SceneView.RepaintAll();
    }

    private void ClearAllTempWaypoints()
    {
        var tempWPts = FindObjectsOfType<MapWaypoint>();
        foreach (var wp in tempWPts)
            Undo.DestroyObjectImmediate(wp.gameObject);
        SceneView.RepaintAll();
    }
}
