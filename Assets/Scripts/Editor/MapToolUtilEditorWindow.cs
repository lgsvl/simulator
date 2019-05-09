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
    Vector2 scrollPosition;

    enum AxisSpace { Local, World }
    enum Axis { XPos, XNeg, YPos, YNeg, ZPos, ZNeg }
    AxisSpace signallightAlignSpace = AxisSpace.Local;
    Axis signallightFwdAxis = Axis.ZPos;
    Axis signallightUpAxis = Axis.YPos;

    AxisSpace stopsignAlignSpace = AxisSpace.Local;
    Axis stopsignUpAxis = Axis.YPos;

    AxisSpace trafficPoleAlignSpace = AxisSpace.Local;
    Axis trafficPoleUpAxis = Axis.ZPos;

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

    private HDMapSignalLightBuilder signallightTemplate;

    private GUIStyle optionTitleLabelStyle;

    [MenuItem("Window/Map Tool Panel")]
    public static void MapToolPanel()
    {
        MapToolUtilEditorWindow window = (MapToolUtilEditorWindow)EditorWindow.GetWindow(typeof(MapToolUtilEditorWindow));
        window.Show();
    }

    void OnEnable()
    {
        lyrMask = 1 << LayerMask.NameToLayer("Ground And Road"); //default layer for snapping temp construction way point
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
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal: false, alwaysShowVertical: true);

        //setup default label style
        if (optionTitleLabelStyle == null)        
            optionTitleLabelStyle = new GUIStyle(GUI.skin.label);        
        optionTitleLabelStyle.alignment = TextAnchor.MiddleRight;

        if (GUILayout.Button("Show/Hide Map"))
        {
            ToggleMap();
        }

        if (GUILayout.Button("Show/Hide Map Selected"))
        {
            ToggleMapSelected();
            Debug.Log("NOT SUPPORTED" + "ShowMapSelected: " + Map.MapTool.showMapSelected);
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
        if (GUILayout.Button("Clear All Temp Map Waypoints"))
        {
            ClearAllTempWaypoints();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("Create Map Elements", EditorStyles.boldLabel);

        if (GUILayout.Button($"Make Lane ({nameof(MapLaneSegmentBuilder)})"))
        {
            this.MakeLaneSegmentBuilder();
        }
        if (GUILayout.Button($"Make StopLine ({nameof(MapStopLineSegmentBuilder)})"))
        {
            this.MakeStoplineSegmentBuilder();
        }
        if (GUILayout.Button($"Make BoundaryLine ({nameof(MapBoundaryLineSegmentBuilder)})"))
        {
            this.MakeBoundaryLineSegmentBuilder();
        }
        if (GUILayout.Button($"Make Junction ({nameof(MapJunctionBuilder)})"))
        {
            this.MakeJunctionBuilder();
        }
        if (GUILayout.Button($"Make SpeedBump ({nameof(MapSpeedBumpBuilder)})"))
        {
            this.MakeSpeedBumpBuilder();
        }
        if (GUILayout.Button($"Make ParkingSpace ({nameof(MapParkingSpaceBuilder)})"))
        {
            this.MakeParkingSpaceBuilder();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Signal Light:");

        if (GUILayout.Button($"Load SignalLight Template"))
        {
            this.LoadSignallightBuilderTemplate();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button($"Make SignalLight ({nameof(HDMapSignalLightBuilder)})"))
        {
            this.MakeSignallightBuilder();
        }

        EditorGUILayout.LabelField("Space", optionTitleLabelStyle, GUILayout.MinWidth(0));
        signallightAlignSpace = (AxisSpace)EditorGUILayout.EnumPopup(signallightAlignSpace);

        EditorGUILayout.LabelField("Aim", optionTitleLabelStyle, GUILayout.MinWidth(0));
        signallightFwdAxis = (Axis)EditorGUILayout.EnumPopup(signallightFwdAxis);

        EditorGUILayout.LabelField("Up", optionTitleLabelStyle, GUILayout.MinWidth(0));
        signallightUpAxis = (Axis)EditorGUILayout.EnumPopup(signallightUpAxis);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Stop Sign:");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button($"Make StopSign ({nameof(HDMapStopSignBuilder)})"))
        {
            this.MakeStopsignBuilder();
        }

        EditorGUILayout.LabelField("Space", optionTitleLabelStyle, GUILayout.MinWidth(0));
        stopsignAlignSpace = (AxisSpace)EditorGUILayout.EnumPopup(stopsignAlignSpace);

        EditorGUILayout.LabelField("Up", optionTitleLabelStyle, GUILayout.MinWidth(0));
        stopsignUpAxis = (Axis)EditorGUILayout.EnumPopup(stopsignUpAxis);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Traffic Pole:");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button($"Make Traffic Pole ({nameof(VectorMapPoleBuilder)})"))
        {
            this.MakeTrafficPoleBuilder();
        }

        EditorGUILayout.LabelField("Space", optionTitleLabelStyle, GUILayout.MinWidth(0));
        trafficPoleAlignSpace = (AxisSpace)EditorGUILayout.EnumPopup(trafficPoleAlignSpace);

        EditorGUILayout.LabelField("Up", optionTitleLabelStyle, GUILayout.MinWidth(0));
        trafficPoleUpAxis = (Axis)EditorGUILayout.EnumPopup(trafficPoleUpAxis);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("Advanced Utils", EditorStyles.boldLabel);

        if (GUILayout.Button("Hide All MapSegment Handles"))
        {
            HideAllMapSegmentHandles();
        }
        if (GUILayout.Button("Double Selected Segment Builders Resolution"))
        {
            DoubleSelectionSubsegmentResolution();
        }

        if (GUILayout.Button("Half Selected Segment Builders Resolution"))
        {
            HalfSelectionSubsegmentResolution();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Joint Two Lane Segments"))
        {
            this.JointTwoLaneSegments();
        }
        EditorGUILayout.LabelField("Merge Connection Points", optionTitleLabelStyle, GUILayout.MinWidth(0));
        mergeConnectionPoint = EditorGUILayout.Toggle(mergeConnectionPoint);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Link SignalLight and StopLine"))
        {
            this.LinkSignallightStopline();
        }

        if (GUILayout.Button("Link StopSign and StopLine"))
        {
            this.LinkStopsignStopline();
        }

        if (GUILayout.Button("Link TrafficPole and SignalLight"))
        {
            this.LinkTrafficPoleAndSignalLight();
        }

        if (GUILayout.Button("Link Selected TrafficPoles To Contained SignalLights"))
        {
            this.LinkedSelectedTrafficPolesToContainedLights();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Label("In-Between Lane Generation", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        waypointCount = EditorGUILayout.IntField("Waypoint Count", waypointCount);
        startTangent = EditorGUILayout.FloatField("Start Tangent", startTangent);
        endTangent = EditorGUILayout.FloatField("End Tangent", endTangent);
        EditorGUILayout.EndHorizontal();

        inBtwLaneParamsPresetCount = EditorGUILayout.IntField("Preset Count", inBtwLaneParamsPresetCount);

        if (inBtwLaneParamsPresetCount < 1)
            inBtwLaneParamsPresetCount = 1;
        if (inBtwLaneParamsPresetCount > 7)
            inBtwLaneParamsPresetCount = 7;

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


        //experimental/temp functionalities
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Experimental/Temp Functions", EditorStyles.boldLabel);

        if (GUILayout.Button($"{nameof(HDMapSignalLightBuilder)} --> {nameof(MapSignalLightBuilder)}"))
        {
            this.HDMapSignalLightToMapSignalLight();
        }

        EditorGUILayout.EndScrollView();
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

    static void ToggleMapSelected()
    {
        Map.MapTool.showMapSelected = !Map.MapTool.showMapSelected;
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
            var waypoint = wpGo.AddComponent<MapWaypoint>();
            waypoint.lyrMask = lyrMask;
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

        Selection.activeObject = newGo;
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

        Selection.activeObject = newGo;
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

        Selection.activeObject = newGo;
    }

    private void MakeJunctionBuilder()
    {
        tempWaypoints_selected.RemoveAll(p => p == null);
        if (tempWaypoints_selected.Count < 2)
        {
            Debug.Log("You need to select at least two temp waypoints for this operation");
            return;
        }
        var newGo = new GameObject("Junction");
        var junctionBuilder = newGo.AddComponent<MapJunctionBuilder>();
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));

        Vector3 avgPt = Vector3.zero;
        foreach (var p in tempWaypoints_selected)
            avgPt += p.transform.position;

        avgPt /= tempWaypoints_selected.Count;
        junctionBuilder.transform.position = avgPt;

        foreach (var p in tempWaypoints_selected)
            junctionBuilder.segment.targetLocalPositions.Add(junctionBuilder.transform.InverseTransformPoint(p.transform.position));

        junctionBuilder.lineType = Map.BoundLineType.SOLID_WHITE;

        if (parentObj != null)
            junctionBuilder.transform.SetParent(parentObj.transform);

        tempWaypoints_selected.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints_selected.Clear();

        Selection.activeObject = newGo;
    }

    private void MakeParkingSpaceBuilder()
    {
        tempWaypoints_selected.RemoveAll(p => p == null);
        if (tempWaypoints_selected.Count < 2)
        {
            Debug.Log("You need to select at least two temp waypoints for this operation");
            return;
        }
        var newGo = new GameObject("ParkingSpace");
        var parkingSpaceBuilder = newGo.AddComponent<MapParkingSpaceBuilder>();
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));

        Vector3 avgPt = Vector3.zero;
        foreach (var p in tempWaypoints_selected)
            avgPt += p.transform.position;

        avgPt /= tempWaypoints_selected.Count;
        parkingSpaceBuilder.transform.position = avgPt;

        foreach (var p in tempWaypoints_selected)
            parkingSpaceBuilder.segment.targetLocalPositions.Add(parkingSpaceBuilder.transform.InverseTransformPoint(p.transform.position));

        parkingSpaceBuilder.lineType = Map.BoundLineType.SOLID_WHITE;

        if (parentObj != null)
            parkingSpaceBuilder.transform.SetParent(parentObj.transform);

        tempWaypoints_selected.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints_selected.Clear();

        Selection.activeObject = newGo;
    }

    private void MakeSpeedBumpBuilder()
    {
        tempWaypoints_selected.RemoveAll(p => p == null);
        if (tempWaypoints_selected.Count < 2)
        {
            Debug.Log("You need to select at least two temp waypoints for this operation");
            return;
        }
        var newGo = new GameObject("SpeedBump");
        var speedBumpBuilder = newGo.AddComponent<MapSpeedBumpBuilder>();
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));

        Vector3 avgPt = Vector3.zero;
        foreach (var p in tempWaypoints_selected)
            avgPt += p.transform.position;

        avgPt /= tempWaypoints_selected.Count;
        speedBumpBuilder.transform.position = avgPt;

        foreach (var p in tempWaypoints_selected)
            speedBumpBuilder.segment.targetLocalPositions.Add(speedBumpBuilder.transform.InverseTransformPoint(p.transform.position));

        speedBumpBuilder.lineType = Map.BoundLineType.SOLID_WHITE;

        if (parentObj != null)
            speedBumpBuilder.transform.SetParent(parentObj.transform);

        tempWaypoints_selected.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints_selected.Clear();

        Selection.activeObject = newGo;
    }

    private void LoadSignallightBuilderTemplate()
    {
        var Ts = Selection.transforms;
        if (Ts.Length != 1)
        {
            Debug.Log("You need exactly one object selected for this operation");
            return;
        }  


        var builder = Ts[0].GetComponent<HDMapSignalLightBuilder>();
        if (builder == null)
        {
            Debug.Log($"You need exactly one {nameof(HDMapSignalLightBuilder)} selected for this operation");
            return;
        }

        signallightTemplate = builder;
    }

    private void MakeSignallightBuilder()
    {
        var Ts = Selection.transforms;

        Vector3 targetFwdVec = Vector3.forward;
        Vector3 targetUpVec = Vector3.up;

        switch (signallightFwdAxis)
        {
            case Axis.XPos:
                targetFwdVec = Vector3.right;
                break;
            case Axis.XNeg:
                targetFwdVec = -Vector3.right;
                break;
            case Axis.YPos:
                targetFwdVec = Vector3.up;
                break;
            case Axis.YNeg:
                targetFwdVec = -Vector3.up;
                break;
            case Axis.ZPos:
                targetFwdVec = Vector3.forward;
                break;
            case Axis.ZNeg:
                targetFwdVec = -Vector3.forward;
                break;
        }

        switch (signallightUpAxis)
        {
            case Axis.XPos:
                targetUpVec = Vector3.right;
                break;
            case Axis.XNeg:
                targetUpVec = -Vector3.right;
                break;
            case Axis.YPos:
                targetUpVec = Vector3.up;
                break;
            case Axis.YNeg:
                targetUpVec = -Vector3.up;
                break;
            case Axis.ZPos:
                targetUpVec = Vector3.forward;
                break;
            case Axis.ZNeg:
                targetUpVec = -Vector3.forward;
                break;
        }    

        if (Ts.Length == 0 || (Ts.Length == 1 && Ts[0].GetComponent<MapWaypoint>() != null))
        {
            var newGo = new GameObject("HDMapSignalLight");
            Undo.RegisterCreatedObjectUndo(newGo, newGo.name);
            newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);
            if (Ts.Length == 0)
            {
                newGo.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 8f;
            }
            else
            {
                var waypoint = Ts[0].GetComponent<MapWaypoint>();
                newGo.transform.position = waypoint.transform.position;
                Undo.DestroyObjectImmediate(waypoint.gameObject);
            }

            var builder = newGo.AddComponent<HDMapSignalLightBuilder>();

            if (signallightTemplate != null)
            {
                builder.signalDatas = signallightTemplate.signalDatas.Select(d => new MapSignalLightBuilder.Data() { localPosition = builder.transform.InverseTransformVector(signallightTemplate.transform.TransformVector(d.localPosition)), type = d.type }).ToList();
                builder.boundOffsets = signallightTemplate.boundOffsets;
                Vector3 multiplier = new Vector3(
                    signallightTemplate.transform.lossyScale.x / builder.transform.lossyScale.x,
                    signallightTemplate.transform.lossyScale.y / builder.transform.lossyScale.y,
                    signallightTemplate.transform.lossyScale.z / builder.transform.lossyScale.z
                    );

                builder.boundScale = Vector3.Scale(multiplier, signallightTemplate.boundScale);
            }
            else
            {
                builder.signalDatas = new List<MapSignalLightBuilder.Data>() {
                    new MapSignalLightBuilder.Data() { localPosition = Vector3.up * 0.5f, type = MapSignalLightBuilder.Data.Type.Red },
                new MapSignalLightBuilder.Data() { localPosition = Vector3.zero, type = MapSignalLightBuilder.Data.Type.Yellow },
                new MapSignalLightBuilder.Data() { localPosition = Vector3.up * -0.5f, type = MapSignalLightBuilder.Data.Type.Green }
                };

                builder.boundScale = new Vector3(1f, 1f, 0);
            }

            Selection.activeObject = newGo;

            return;
        }


        foreach (var t in Ts)
        {
            var newGo = new GameObject("HDMapSignalLight");
            Undo.RegisterCreatedObjectUndo(newGo, newGo.name);
            var builder = newGo.AddComponent<HDMapSignalLightBuilder>();

            if (signallightTemplate != null)
            {
                builder.signalDatas = signallightTemplate.signalDatas.Select(d => new MapSignalLightBuilder.Data() { localPosition = builder.transform.InverseTransformVector(signallightTemplate.transform.TransformVector(d.localPosition)), type = d.type }).ToList();
                builder.boundOffsets = signallightTemplate.boundOffsets;
                Vector3 multiplier = new Vector3(
                    signallightTemplate.transform.lossyScale.x / builder.transform.lossyScale.x,
                    signallightTemplate.transform.lossyScale.y / builder.transform.lossyScale.y,
                    signallightTemplate.transform.lossyScale.z / builder.transform.lossyScale.z
                    );

                builder.boundScale = Vector3.Scale(multiplier, signallightTemplate.boundScale);
            }

            Vector3 fwdVec = targetFwdVec;
            Vector3 UpVec = targetUpVec;

            if (signallightAlignSpace == AxisSpace.Local)
            {
                fwdVec = t.TransformDirection(targetFwdVec).normalized;
                UpVec = t.TransformDirection(targetUpVec).normalized;
            }

            newGo.transform.rotation = Quaternion.FromToRotation(newGo.transform.up, UpVec) * newGo.transform.rotation;
            newGo.transform.rotation = Quaternion.FromToRotation(newGo.transform.forward, fwdVec) * newGo.transform.rotation;
            newGo.transform.position = t.transform.position;

            newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);

            Selection.activeObject = newGo;
        }
    }

    private void MakeStopsignBuilder()
    {
        var Ts = Selection.transforms;

        Vector3 targetUpVec = Vector3.up;

        switch (stopsignUpAxis)
        {
            case Axis.XPos:
                targetUpVec = Vector3.right;
                break;
            case Axis.XNeg:
                targetUpVec = -Vector3.right;
                break;
            case Axis.YPos:
                targetUpVec = Vector3.up;
                break;
            case Axis.YNeg:
                targetUpVec = -Vector3.up;
                break;
            case Axis.ZPos:
                targetUpVec = Vector3.forward;
                break;
            case Axis.ZNeg:
                targetUpVec = -Vector3.forward;
                break;
        }

        if (Ts.Length == 0 || (Ts.Length == 1 && Ts[0].GetComponent<MapWaypoint>() != null))
        {
            var newGo = new GameObject("HDMapStopSign");
            Undo.RegisterCreatedObjectUndo(newGo, newGo.name);
            newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);
            if (Ts.Length == 0)
            {
                newGo.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 8f;
            }
            else
            {
                var waypoint = Ts[0].GetComponent<MapWaypoint>();
                newGo.transform.position = waypoint.transform.position;
                Undo.DestroyObjectImmediate(waypoint.gameObject);
            }

            var builder = newGo.AddComponent<HDMapStopSignBuilder>();
            builder.transform.rotation = Quaternion.FromToRotation(builder.transform.forward, Vector3.up) * builder.transform.rotation;

            Selection.activeObject = newGo;

            return;
        }

        foreach (var t in Ts)
        {
            var newGo = new GameObject("HDMapStopSign");
            Undo.RegisterCreatedObjectUndo(newGo, newGo.name);
            var builder = newGo.AddComponent<HDMapStopSignBuilder>();

            Vector3 UpVec = targetUpVec;

            if (stopsignAlignSpace == AxisSpace.Local)
            {
                UpVec = t.TransformDirection(targetUpVec);
            }

            newGo.transform.rotation = Quaternion.FromToRotation(newGo.transform.forward, UpVec) * newGo.transform.rotation;
            newGo.transform.position = t.transform.position;

            newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);

            Selection.activeObject = newGo;
        }
    }

    private void MakeTrafficPoleBuilder()
    {
        var Ts = Selection.transforms;

        Vector3 targetUpVec = Vector3.up;

        switch (trafficPoleUpAxis)
        {
            case Axis.XPos:
                targetUpVec = Vector3.right;
                break;
            case Axis.XNeg:
                targetUpVec = -Vector3.right;
                break;
            case Axis.YPos:
                targetUpVec = Vector3.up;
                break;
            case Axis.YNeg:
                targetUpVec = -Vector3.up;
                break;
            case Axis.ZPos:
                targetUpVec = Vector3.forward;
                break;
            case Axis.ZNeg:
                targetUpVec = -Vector3.forward;
                break;
        }

        if (Ts.Length == 0 || (Ts.Length == 1 && Ts[0].GetComponent<MapWaypoint>() != null))
        {
            var newGo = new GameObject("VectorMapPole");
            Undo.RegisterCreatedObjectUndo(newGo, newGo.name);
            newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);
            if (Ts.Length == 0)
            {
                newGo.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 8f;
            }
            else
            {
                var waypoint = Ts[0].GetComponent<MapWaypoint>();
                newGo.transform.position = waypoint.transform.position;
                Undo.DestroyObjectImmediate(waypoint.gameObject);
            }

            var builder = newGo.AddComponent<VectorMapPoleBuilder>();
            builder.transform.rotation = Quaternion.FromToRotation(builder.transform.forward, Vector3.up) * builder.transform.rotation;

            Selection.activeObject = newGo;

            return;
        }

        foreach (var t in Ts)
        {
            var newGo = new GameObject("VectorMapPole");
            Undo.RegisterCreatedObjectUndo(newGo, newGo.name);
            var builder = newGo.AddComponent<VectorMapPoleBuilder>();

            Vector3 UpVec = targetUpVec;

            if (stopsignAlignSpace == AxisSpace.Local)
            {
                UpVec = t.TransformDirection(targetUpVec);
            }

            newGo.transform.rotation = Quaternion.FromToRotation(newGo.transform.forward, UpVec) * newGo.transform.rotation;
            newGo.transform.position = t.transform.position;

            newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);

            Selection.activeObject = newGo;
        }
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

        Selection.activeObject = newGo;
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

        Undo.RegisterFullObjectHierarchyUndo(signalLightBuilders[0], signalLightBuilders[0].gameObject.name);
        signalLightBuilders[0].hintStopline = stoplineBuilders[0];
    }

    private void LinkStopsignStopline()
    {
        var Ts = Selection.transforms;

        var stopsignbuilder = Ts.Select(b => b.GetComponent<HDMapStopSignBuilder>()).ToList();
        var stoplineBuilders = Ts.Select(b => b.GetComponent<MapStopLineSegmentBuilder>()).ToList();
        stopsignbuilder.RemoveAll(b => b == null);
        stoplineBuilders.RemoveAll(b => b == null);

        if (!(stopsignbuilder.Count == 1 && stoplineBuilders.Count == 1))
        {
            Debug.Log($"You need to select one {nameof(HDMapStopSignBuilder)} and one {nameof(MapStopLineSegmentBuilder)} to perform the operation");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(stopsignbuilder[0], stopsignbuilder[0].gameObject.name);
        stopsignbuilder[0].stopline = stoplineBuilders[0];
    }

    private void LinkTrafficPoleAndSignalLight()
    {
        var Ts = Selection.transforms;

        var signallights = Ts.Select(b => b.GetComponent<MapSignalLightBuilder>()).ToList();
        var trafficPoles = Ts.Select(b => b.GetComponent<VectorMapPoleBuilder>()).ToList();
        signallights.RemoveAll(b => b == null);
        trafficPoles.RemoveAll(b => b == null);

        if (!(signallights.Count == 1 && trafficPoles.Count == 1))
        {
            Debug.Log($"You need to select one {nameof(MapSignalLightBuilder)} and one {nameof(VectorMapPoleBuilder)} to perform the operation");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(trafficPoles[0], trafficPoles[0].gameObject.name);
        if (!trafficPoles[0].signalLights.Contains(signallights[0]))        
            trafficPoles[0].signalLights.Add(signallights[0]);        
        trafficPoles[0].signalLights.RemoveAll(sl => sl == null);
    }

    private void LinkedSelectedTrafficPolesToContainedLights()
    {
        var Ts = Selection.transforms;

        var trafficPoles = Ts.Select(b => b.GetComponent<VectorMapPoleBuilder>()).ToList();

        if (trafficPoles.Count == 0)
        {
            Debug.Log($"You need to select at least one {nameof(VectorMapPoleBuilder)} to perform the operation");
            return;
        }

        foreach (var pole in trafficPoles)
        {
            Undo.RegisterFullObjectHierarchyUndo(pole, pole.gameObject.name);
            pole.LinkContainedSignalLights();
        }        
    }

    private void HDMapSignalLightToMapSignalLight()
    {
        var Ts = Selection.transforms;

        foreach (var t in Ts)
        {
            var subClassInstance = t.GetComponent<HDMapSignalLightBuilder>();
            if (subClassInstance == null)
            {
                continue;
            }
            var parentClassInstance = t.gameObject.AddComponent<MapSignalLightBuilder>();
            parentClassInstance.signalDatas = subClassInstance.signalDatas;
            parentClassInstance.hintStopline = subClassInstance.hintStopline;
            Undo.DestroyObjectImmediate(subClassInstance);
        }
    }
}
