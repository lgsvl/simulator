using System.Collections.Generic;
using System.Linq;
using UnityEditor;
/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEngine;

public class MapToolUtilEditorWindow : EditorWindow
{
    //keeping track of the order
    private List<MapWaypoint> tempWaypoints_selected = new List<MapWaypoint>();
    private List<MapLaneSegmentBuilder> mapLaneBuilder_selected = new List<MapLaneSegmentBuilder>();
    private GameObject parentObj;

    [MenuItem("Window/Map Tool Panel")]
    public static void MapToolPanel()
    {
        MapToolUtilEditorWindow window = (MapToolUtilEditorWindow)EditorWindow.GetWindow(typeof(MapToolUtilEditorWindow));
        window.Show();
    }

    private void OnSelectionChange()
    {
        var selectionList = Selection.gameObjects.ToList();
        var selectedSceneGos = selectionList.Where(go => !AssetDatabase.Contains(go)).ToList();

        tempWaypoints_selected.RemoveAll(p => p == null);
        mapLaneBuilder_selected.RemoveAll(b => b == null);

        selectedSceneGos.ForEach(go =>
        {
            var tempWaypoint = go.GetComponent<MapWaypoint>();
            if (tempWaypoint != null)
            {
                if (!tempWaypoints_selected.Contains(tempWaypoint))
                    tempWaypoints_selected.Add(tempWaypoint);
            }

            var laneSegBuilder = go.GetComponent<MapLaneSegmentBuilder>();
            if (laneSegBuilder != null)
            {
                if (!mapLaneBuilder_selected.Contains(laneSegBuilder))
                    mapLaneBuilder_selected.Add(laneSegBuilder);
            }
        });

        tempWaypoints_selected.RemoveAll(p => !selectedSceneGos.Contains(p.gameObject));
        mapLaneBuilder_selected.RemoveAll(b => !selectedSceneGos.Contains(b.gameObject));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Show/Hide Map"))
        {
            ToggleMap();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        parentObj = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObj, typeof(GameObject), true);

        if (GUILayout.Button("Create Temp Map Waypoint"))
        {
            CreateTempWaypoint();
        }
        if (GUILayout.Button("Clear All Temp Waypoints"))
        {
            ClearAllTempWaypoints();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Make Lane Segment Builder"))
        {
            this.MakeLaneSegmentBuilder();
        }
        if (GUILayout.Button("Make Stopline Segment Builder"))
        {
            this.MakeStoplineSegmentBuilder();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Hide All MapSegment Handles"))
        {
            HideAllMapSegmentHandles();
        }
        if (GUILayout.Button("Double Selected Segment Builder Resolution"))
        {
            DoubleSelectionSubsegmentResolution();
        }

        if (GUILayout.Button("Half Selected Segment Builder Resolution"))
        {
            HalfSelectionSubsegmentResolution();
        }

        if (GUILayout.Button("Joint Two Lane Segments"))
        {
            JointTwoLaneSegments();
        }        

        if (GUILayout.Button("Link Neighbor Lanes from Left"))
        {
            this.LinkFromLeft();
        }
        if (GUILayout.Button("Link Neighbor Lanes from Right"))
        {
            this.LinkFromRight();
        }
        if (GUILayout.Button("Link Reverse Neighbor Lanes"))
        {
            this.LinkLeftReverse();
        }        
    }

    static void HideAllMapSegmentHandles()
    {
        var mapSegmentBuilders = FindObjectsOfType<MapSegmentBuilder>();
        foreach (var mapSegBldr in mapSegmentBuilders)
        {
            if (mapSegBldr.displayHandles)
            {
                Undo.RegisterFullObjectHierarchyUndo(mapSegBldr, mapSegBldr.gameObject.name);
                mapSegBldr.displayHandles = false;
            }
        }
    }

    static void DoubleSelectionSubsegmentResolution()
    {
        var Ts = Selection.transforms;

        var builders = Ts.Select(x => x.GetComponent<MapSegmentBuilder>()).ToList();
        if (builders.Count < 1)
        {
            Debug.Log($"You need to select at least one {nameof(MapSegmentBuilder)} instance");
            return;
        }

        var someFail = false;

        foreach (var builder in builders)
        {
            Undo.RegisterFullObjectHierarchyUndo(builder, "builder");
            if (!Map.MapTool.DoubleSubsegmentResolution(builder.segment))
            {
                someFail = true;
            }
        }

        if (someFail)
        {
            Debug.Log($"Some {builders.GetType().GetGenericArguments()[0]} can not double its resolution");
        }
    }

    static void HalfSelectionSubsegmentResolution()
    {
        var Ts = Selection.transforms;

        var builders = Ts.Select(x => x.GetComponent<MapSegmentBuilder>()).ToList();
        if (builders.Count < 1)
        {
            Debug.Log($"You need to select at least one {nameof(MapSegmentBuilder)} instance");
            return;
        }

        var someFail = false;

        foreach (var builder in builders)
        {
            Undo.RegisterFullObjectHierarchyUndo(builder, "builder");
            if (!Map.MapTool.HalfSubsegmentResolution(builder.segment))
            {
                someFail = true;
            }
        }

        if (someFail)
        {
            Debug.Log($"Some {builders.GetType().GetGenericArguments()[0]} can not half its resolution");
        }
    }

    static void JointTwoLaneSegments()
    {
        var Ts = Selection.transforms;

        var lnBuilders = Ts.Select(x => x.GetComponent<MapLaneSegmentBuilder>()).ToList();
        if (lnBuilders.Count != 2)
        {
            Debug.Log($"You need to select exactly two {nameof(MapLaneSegmentBuilder)} instances, aborted.");
            return;
        }

        //Undo Register
        lnBuilders.ForEach(b => Undo.RegisterFullObjectHierarchyUndo(b, nameof(b.gameObject.name)));

        var result = Map.MapTool.JointTwoMapLaneSegments(lnBuilders);
        if (!result)
        {
            Debug.Log($"operation failed.");
            return;
        } 
    }

    static void ToggleMap()
    {
        Map.MapTool.showMap = !Map.MapTool.showMap;
    }

    static void CreateTempWaypoint()
    {
        var cam = SceneView.lastActiveSceneView.camera;
        if (cam == null)
        {
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, 1 << LayerMask.NameToLayer("Ground And Road")))
        {
            var wpGo = new GameObject("Temp_Waypoint");
            wpGo.transform.position = hit.point;
            wpGo.AddComponent<MapWaypoint>();
            wpGo.transform.localScale = Vector3.one * Map.MapTool.PROXIMITY;
            Undo.RegisterCreatedObjectUndo(wpGo, nameof(wpGo));
        }
    }

    static void ClearAllTempWaypoints()
    {
        var tempWPts = FindObjectsOfType<MapWaypoint>();
        foreach (var wp in tempWPts)        
            Undo.DestroyObjectImmediate(wp.gameObject);        
    }

    private void MakeLaneSegmentBuilder()
    {
        tempWaypoints_selected.RemoveAll(p => p == null);
        if (tempWaypoints_selected.Count < 2)
        {
            Debug.Log("You need to select at least two temp waypoints for this operation");
            return;
        }
        var newGo = new GameObject("MapSegment_Lane");
        var laneSegBuilder = newGo.AddComponent<MapLaneSegmentBuilder>();
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));

        Vector3 avgPt = Vector3.zero;
        foreach (var p in tempWaypoints_selected)
        {
            avgPt += p.transform.position;
        }
        avgPt /= tempWaypoints_selected.Count;
        laneSegBuilder.transform.position = avgPt;

        foreach (var p in tempWaypoints_selected)
        {
            laneSegBuilder.segment.targetLocalPositions.Add(laneSegBuilder.transform.InverseTransformPoint(p.transform.position));
        }

        if (parentObj != null)
            laneSegBuilder.transform.SetParent(parentObj.transform);

        tempWaypoints_selected.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints_selected.Clear();
    }

    private void MakeStoplineSegmentBuilder()
    {
        tempWaypoints_selected.RemoveAll(p => p == null);
        if (tempWaypoints_selected.Count < 2)
        {
            Debug.Log("You need to select at least two temp waypoints for this operation");
            return;
        }
        var newGo = new GameObject("MapSegment_Stopline");
        var stoplineSegBuilder = newGo.AddComponent<MapStopLineSegmentBuilder>();
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));

        Vector3 avgPt = Vector3.zero;
        foreach (var p in tempWaypoints_selected)
        {
            avgPt += p.transform.position;
        }
        avgPt /= tempWaypoints_selected.Count;
        stoplineSegBuilder.transform.position = avgPt;

        foreach (var p in tempWaypoints_selected)
        {
            stoplineSegBuilder.segment.targetLocalPositions.Add(stoplineSegBuilder.transform.InverseTransformPoint(p.transform.position));
        }

        if (parentObj != null)
            stoplineSegBuilder.transform.SetParent(parentObj.transform);

        tempWaypoints_selected.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints_selected.Clear();
    }

    private void LinkFromLeft()
    {
        mapLaneBuilder_selected.RemoveAll(b => b == null);
        mapLaneBuilder_selected.ForEach(b => Undo.RegisterFullObjectHierarchyUndo(b, b.gameObject.name));
        Map.MapTool.LinkLanes(mapLaneBuilder_selected);
    }

    private void LinkFromRight()
    {
        mapLaneBuilder_selected.RemoveAll(b => b == null);
        mapLaneBuilder_selected.ForEach(b => Undo.RegisterFullObjectHierarchyUndo(b, b.gameObject.name));
        var reversed = new List<MapLaneSegmentBuilder>(mapLaneBuilder_selected);
        reversed.Reverse();
        Map.MapTool.LinkLanes(reversed);
    }
    
    private void LinkLeftReverse()
    {
        mapLaneBuilder_selected.RemoveAll(b => b == null);
        if (mapLaneBuilder_selected.Count != 2)
        {
            Debug.Log("You can only do the link reverse operation with exact two lane segment builders selected");
            return;
        }
        mapLaneBuilder_selected.ForEach(b => Undo.RegisterFullObjectHierarchyUndo(b, b.gameObject.name));
        Map.MapTool.LinkLanes(mapLaneBuilder_selected, reverseLink:true);
    }
}
