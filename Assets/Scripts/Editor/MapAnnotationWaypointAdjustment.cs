/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Simulator.Map;

public class MapAnnotationWaypointAdjustment: EditorWindow
{
    [MenuItem("Simulator/Adjust Map Annotation Waypoints", false, 130)]
    public static void Open()
    {
        var window = GetWindow(typeof(MapAnnotationWaypointAdjustment), false, "HD Map Annotation Waypoint Adjustment");
        window.Show();
    }

    private void OnGUI()
    {
        var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };

        GUILayout.Space(10);
        EditorGUILayout.LabelField("HD Map Annotation Waypoint Adjustment", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("This tool snaps all waypoints in the HD map onto the Default layer", UnityEditor.MessageType.Info);

        if (GUILayout.Button("Adjust Waypoints"))
        {
            AdjustWaypoints();
        }
    }

    private void AdjustWaypoints()
    {
        List<Object> allObjects = new List<Object>();
        MapLane[] mapLanes = FindObjectsOfType<MapLane>();
        MapLine[] mapLines = FindObjectsOfType<MapLine>();
        MapJunction[] junctions = FindObjectsOfType<MapJunction>();
        MapSpeedBump[] bumps = FindObjectsOfType<MapSpeedBump>();
        MapParkingSpace[] spaces = FindObjectsOfType<MapParkingSpace>();
        MapPedestrian[] walkways = FindObjectsOfType<MapPedestrian>();
        MapClearArea[] areas = FindObjectsOfType<MapClearArea>();
        MapCrossWalk[] crosses = FindObjectsOfType<MapCrossWalk>();
        allObjects.AddRange(mapLanes);
        allObjects.AddRange(mapLines);
        allObjects.AddRange(junctions);
        allObjects.AddRange(bumps);
        allObjects.AddRange(spaces);
        allObjects.AddRange(walkways);
        allObjects.AddRange(areas);
        allObjects.AddRange(crosses);
        Undo.RecordObjects(allObjects.ToArray(),"waypoints");

        foreach (MapLane lane in mapLanes)
        {
            for (int i=0; i<lane.mapLocalPositions.Count; i++)
            {
                lane.mapLocalPositions[i] = TransformWaypoint(lane.transform, lane.mapLocalPositions[i]);
            }
        }
        Debug.Log("Map Lanes moved");
        SceneView.RepaintAll();
        
        foreach (MapLine line in mapLines)
        {
            for (int i=0; i<line.mapLocalPositions.Count; i++)
            {
                line.mapLocalPositions[i] = TransformWaypoint(line.transform, line.mapLocalPositions[i]);
            }
        }
        Debug.Log("Map Lines moved");
        SceneView.RepaintAll();
        
        foreach (MapJunction j in junctions)
        {
            for (int i=0; i<j.mapLocalPositions.Count; i++)
            {
                j.mapLocalPositions[i] = TransformWaypoint(j.transform, j.mapLocalPositions[i]);
            }
        }
        Debug.Log("Map Junctions moved");
        SceneView.RepaintAll();
        
        foreach (MapSpeedBump b in bumps)
        {
            for (int i = 0; i < b.mapLocalPositions.Count; i++)
            {
                b.mapLocalPositions[i] = TransformWaypoint(b.transform, b.mapLocalPositions[i]);
            }
        }
        Debug.Log("Map Speed Bumps moved");
        SceneView.RepaintAll();
        
        foreach (MapParkingSpace s in spaces)
        {
            for (int i=0; i < s.mapLocalPositions.Count; i++)
            {
                s.mapLocalPositions[i] = TransformWaypoint(s.transform, s.mapLocalPositions[i]);
            }
        }
        Debug.Log("Map Parking Space moved");
        SceneView.RepaintAll();
        
        foreach (MapPedestrian w in walkways)
        {
            for (int i = 0; i < w.mapLocalPositions.Count; i++)
            {
                w.mapLocalPositions[i] = TransformWaypoint(w.transform, w.mapLocalPositions[i]);
            }
        }
        Debug.Log("Map Pedestrian Nav moved");
        SceneView.RepaintAll();
        
        foreach (MapClearArea a in areas)
        {
            for (int i = 0; i < a.mapLocalPositions.Count; i++)
            {
                a.mapLocalPositions[i] = TransformWaypoint(a.transform, a.mapLocalPositions[i]);
            }
        }
        Debug.Log("Map Clear Areas moved");
        SceneView.RepaintAll();
        
        foreach (MapCrossWalk c in crosses)
        {
            for (int i = 0; i < c.mapLocalPositions.Count; i++)
            {
                c.mapLocalPositions[i] = TransformWaypoint(c.transform, c.mapLocalPositions[i]);
            }
        }
        Debug.Log("Map Crosswalks moved");
        SceneView.RepaintAll();
    }

    private Vector3 TransformWaypoint(Transform t, Vector3 localPosition)
    {
        LayerMask layerMask = 1 << LayerMask.NameToLayer("Default");
        Vector3 worldVector = t.TransformPoint(localPosition);
        RaycastHit hit;
        if (Physics.Raycast(worldVector + new Vector3(0, 100, 0), new Vector3(0, -1, 0), out hit, 200f, layerMask))
        {
            return t.InverseTransformPoint(hit.point);
        }

        return localPosition;
    }
}