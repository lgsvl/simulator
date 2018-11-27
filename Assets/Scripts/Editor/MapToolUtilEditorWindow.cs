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
    private LayerMask lyrMask;
    private bool mergeConnectionPoint;
    private int waypointCount = 5;
    private float startTangent = 6.5f;
    private float endTangent = 6.5f;
    private bool offsetEndPoints = true; //whether to offset end points for newly generated in-between lane to avoid selection issue
    private static List<Vector3> inBtwLaneParamSetList;
    private static int inBtwLaneParamsPresetCount = 4;

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

    void Awake()
    {
        lyrMask = 1 << LayerMask.NameToLayer("Ground And Road"); //default layer for snapping temp construction way point
    }

    void OnGUI()
    {
        if (GUILayout.Button("Show/Hide Map"))
        {
            ToggleMap();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("Create Waypoints", EditorStyles.boldLabel);

        parentObj = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObj, typeof(GameObject), true);

        LayerMask tempMask = EditorGUILayout.MaskField("Snapping Layer Mask", UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(lyrMask), UnityEditorInternal.InternalEditorUtility.layers);
        lyrMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

        if (GUILayout.Button("Create Temp Map Waypoint"))
        {
            this.CreateTempWaypoint();
        }
        if (GUILayout.Button("Clear All Temp Waypoints"))
        {
            ClearAllTempWaypoints();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("Make Map Elements", EditorStyles.boldLabel);

        if (GUILayout.Button("Make Lane Segment Builder"))
        {
            this.MakeLaneSegmentBuilder();
        }
        if (GUILayout.Button("Make Stopline Segment Builder"))
        {
            this.MakeStoplineSegmentBuilder();
        }
        if (GUILayout.Button("Make Boundary Line Segment Builder"))
        {
            this.MakeBoundaryLineSegmentBuilder();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("Advanced Utils", EditorStyles.boldLabel);

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

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Joint Two Lane Segments"))
        {
            this.JointTwoLaneSegments();
        }
        mergeConnectionPoint = EditorGUILayout.Toggle("Merge Connection Points", mergeConnectionPoint);
        EditorGUILayout.EndHorizontal();

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

        if (GUILayout.Button("Nullify All Neighbor Lane Fields"))
        {
            this.NullifyAllNeighborLaneFields();
        }

        if (GUILayout.Button("Link Signallight and Stopline"))
        {
            this.LinkSignallightStopline();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("In-Between Lane Generation", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        waypointCount = EditorGUILayout.IntField("Waypoint Count", waypointCount);
        startTangent = EditorGUILayout.FloatField("Start Tangent", startTangent);
        endTangent = EditorGUILayout.FloatField("End Tangent", endTangent);
        EditorGUILayout.EndHorizontal();

        inBtwLaneParamsPresetCount = EditorGUILayout.IntField("Preset Count", inBtwLaneParamsPresetCount);

        if (inBtwLaneParamSetList == null || inBtwLaneParamSetList.Count != inBtwLaneParamsPresetCount)
        {
            inBtwLaneParamSetList = new List<Vector3>(new Vector3[inBtwLaneParamsPresetCount]);
        }
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < inBtwLaneParamsPresetCount; i++)
        {
            if (GUILayout.Button($"Save Preset {i + 1}"))
            {
                var set = inBtwLaneParamSetList[i];
                set.x = startTangent;
                set.y = endTangent;
                set.z = (float)waypointCount;
                inBtwLaneParamSetList[i] = set;
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < inBtwLaneParamsPresetCount; i++)
        {
            if (GUILayout.Button($"Load Preset {i + 1}"))
            {
                var set = inBtwLaneParamSetList[i];
                startTangent = set.x;
                endTangent = set.y;
                waypointCount = Mathf.RoundToInt(set.z);
                inBtwLaneParamSetList[i] = set;
            }
        }
        EditorGUILayout.EndHorizontal();
        offsetEndPoints = EditorGUILayout.Toggle("Offset Start/End Points", offsetEndPoints);
        if (GUILayout.Button("Auto Generate In-Between Lane"))
        {
            this.AutoGenerateConnectionLane();
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

    private void JointTwoLaneSegments()
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

        var result = Map.MapTool.JointTwoMapLaneSegments(lnBuilders, mergeConnectionPoint);
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

    private void CreateTempWaypoint()
    {
        var cam = SceneView.lastActiveSceneView.camera;
        if (cam == null)
        {
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, lyrMask.value))
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
            avgPt += p.transform.position;
        
        avgPt /= tempWaypoints_selected.Count;
        laneSegBuilder.transform.position = avgPt;

        foreach (var p in tempWaypoints_selected)        
            laneSegBuilder.segment.targetLocalPositions.Add(laneSegBuilder.transform.InverseTransformPoint(p.transform.position));
        

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
            avgPt += p.transform.position;
        
        avgPt /= tempWaypoints_selected.Count;
        stoplineSegBuilder.transform.position = avgPt;

        foreach (var p in tempWaypoints_selected)        
            stoplineSegBuilder.segment.targetLocalPositions.Add(stoplineSegBuilder.transform.InverseTransformPoint(p.transform.position));
        

        if (parentObj != null)
            stoplineSegBuilder.transform.SetParent(parentObj.transform);

        tempWaypoints_selected.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints_selected.Clear();
    }

    private void MakeBoundaryLineSegmentBuilder()
    {
        tempWaypoints_selected.RemoveAll(p => p == null);
        if (tempWaypoints_selected.Count < 2)
        {
            Debug.Log("You need to select at least two temp waypoints for this operation");
            return;
        }
        var newGo = new GameObject("MapSegment_WhiteLine");
        var boundaryLineSegBuilder = newGo.AddComponent<MapBoundaryLineSegmentBuilder>();
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));

        Vector3 avgPt = Vector3.zero;
        foreach (var p in tempWaypoints_selected)        
            avgPt += p.transform.position;
        
        avgPt /= tempWaypoints_selected.Count;
        boundaryLineSegBuilder.transform.position = avgPt;

        foreach (var p in tempWaypoints_selected)        
            boundaryLineSegBuilder.segment.targetLocalPositions.Add(boundaryLineSegBuilder.transform.InverseTransformPoint(p.transform.position));

        boundaryLineSegBuilder.lineType = Map.BoundLineType.SOLID_WHITE;

        if (parentObj != null)
            boundaryLineSegBuilder.transform.SetParent(parentObj.transform);

        tempWaypoints_selected.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints_selected.Clear();
    }

    private void AutoGenerateConnectionLane()
    {
        mapLaneBuilder_selected.RemoveAll(b => b == null);
        if (mapLaneBuilder_selected.Count != 2)
        {
            Debug.Log("You can only auto generate new lane between exactly two lanes");
            return;
        }

        var A = mapLaneBuilder_selected[0];
        var B = mapLaneBuilder_selected[1];
        var A_start = A.transform.TransformPoint(A.segment.targetLocalPositions[0]);
        var A_start_aimVec = (A.transform.TransformPoint(A.segment.targetLocalPositions[1]) - A.transform.TransformPoint(A.segment.targetLocalPositions[0])).normalized;
        var B_start = B.transform.TransformPoint(B.segment.targetLocalPositions[0]);
        var B_start_aimVec = (B.transform.TransformPoint(B.segment.targetLocalPositions[1]) - B.transform.TransformPoint(B.segment.targetLocalPositions[0])).normalized;
        var A_end = A.transform.TransformPoint(A.segment.targetLocalPositions[A.segment.targetLocalPositions.Count - 1]);
        var A_end_aimVec = (A_end - A.transform.TransformPoint(A.segment.targetLocalPositions[A.segment.targetLocalPositions.Count - 2])).normalized;
        var B_end = B.transform.TransformPoint(B.segment.targetLocalPositions[B.segment.targetLocalPositions.Count - 1]);
        var B_end_aimVec = (B_end - B.transform.TransformPoint(B.segment.targetLocalPositions[B.segment.targetLocalPositions.Count - 2])).normalized;

        List<Vector3> retPoints;
        if (Vector3.Distance(A_end, B_start) < Vector3.Distance(B_end, A_start))
        {
            Map.MapTool.AutoGenerateNewLane(A_end, A_end_aimVec, startTangent, B_start, B_start_aimVec, endTangent, waypointCount, out retPoints);
        }
        else
        {
            Map.MapTool.AutoGenerateNewLane(B_end, B_end_aimVec, startTangent, A_start, A_start_aimVec, endTangent, waypointCount, out retPoints);
        }

        var newGo = new GameObject("MapSegment_Lane");
        var laneSegBuilder = newGo.AddComponent<MapLaneSegmentBuilder>();
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));

        Vector3 avgPt = Vector3.zero;
        foreach (var pos in retPoints)
        {
            avgPt += pos;
        }
        if (retPoints.Count != 0)        
            avgPt /= retPoints.Count;
        
        laneSegBuilder.transform.position = avgPt;

        foreach (var pos in retPoints)        
            laneSegBuilder.segment.targetLocalPositions.Add(laneSegBuilder.transform.InverseTransformPoint(pos));

        if (offsetEndPoints)
        {
            laneSegBuilder.segment.targetLocalPositions[0] += (laneSegBuilder.segment.targetLocalPositions[1] - laneSegBuilder.segment.targetLocalPositions[0]).normalized * Map.MapTool.PROXIMITY * 0.15f;
            laneSegBuilder.segment.targetLocalPositions[laneSegBuilder.segment.targetLocalPositions.Count - 1] += (laneSegBuilder.segment.targetLocalPositions[laneSegBuilder.segment.targetLocalPositions.Count - 2] - laneSegBuilder.segment.targetLocalPositions[laneSegBuilder.segment.targetLocalPositions.Count - 1]).normalized * Map.MapTool.PROXIMITY * 0.15f;
        }        

        if (parentObj != null)
            laneSegBuilder.transform.SetParent(parentObj.transform);        
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

    private void NullifyAllNeighborLaneFields()
    {
        mapLaneBuilder_selected.RemoveAll(b => b == null);
        if (mapLaneBuilder_selected.Count < 1)        
            return;
        
        mapLaneBuilder_selected.ForEach(b => {
            Undo.RegisterFullObjectHierarchyUndo(b, b.gameObject.name);
            b.leftNeighborForward = null;
            b.rightNeighborForward = null;
            b.leftNeighborReverse = null;
            b.rightNeighborReverse = null;
        });

    }

    private void LinkSignallightStopline()
    {
        var Ts = Selection.transforms;

        var signalLightBuilders = Ts.Select(b => b.GetComponent<MapSignalLightBuilder>()).ToList();
        var stoplineBuilders = Ts.Select(b => b.GetComponent<MapStopLineSegmentBuilder>()).ToList();
        signalLightBuilders.RemoveAll(b => b == null);
        stoplineBuilders.RemoveAll(b => b == null);

        if (!(signalLightBuilders.Count == 1 && stoplineBuilders.Count == 1))
        {
            Debug.Log($"You need to select one {nameof(MapSignalLightBuilder)} and one {nameof(MapStopLineSegmentBuilder)} to perform the operation");
            return;
        }

        signalLightBuilders[0].hintStopline = stoplineBuilders[0];
    }
}
