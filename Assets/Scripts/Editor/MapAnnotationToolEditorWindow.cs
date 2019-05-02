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
    private GUIStyle titleLabelStyle;
    private GUIStyle subtitleLabelStyle;
    private List<MapWaypoint> tempWaypoints = new List<MapWaypoint>();
    
    private GameObject parentObj;
    private LayerMask layerMask;
    private GameObject targetWaypointGO;
    private Texture[] waypointButtonImages;
    private int waypointTotal = 1;
    private int createType = 0;
    private GUIContent[] createTypeContent = {
        new GUIContent { text = "Lane", tooltip = "Create a new Lane MapLane object" },
        new GUIContent { text = "StopLine", tooltip = "Create a new Stop MapLine object" },
        new GUIContent { text = "BoundryLine", tooltip = "Create a new Boundry MapLine object" }
    };
    private int laneTurnType = 0;
    private GUIContent[] laneTurnTypeContent = {
        new GUIContent { text = "NO_TURN", tooltip = "Not a turn lane" },
        new GUIContent { text = "LEFT_TURN", tooltip = "Left turn lane" },
        new GUIContent { text = "RIGHT_TURN", tooltip = "Right turn lane" },
        new GUIContent { text = "U_TURN", tooltip = "U turn lane" }
    };
    private int laneLeftBoundryType = 2;
    private int laneRightBoundryType = 2;
    private Texture[] boundryImages;
    private GUIContent[] boundryTypeContent;
    private int laneSpeedLimit = 25;
    private bool isStopSign = false;
    private int boundryLineType = 3;
    private GUIContent[] boundryLineTypeContent;

    private GameObject signalMesh;
    private Texture[] signalImages;
    private GUIContent[] signalTypeContent;
    private int signalType = 4;
    //private List<MapLaneSegmentBuilder> mapLaneBuilder_selected = new List<MapLaneSegmentBuilder>();

    [MenuItem("Simulator/Map Tool")]
    public static void MapToolPanel()
    {
        MapAnnotationToolEditorWindow window = (MapAnnotationToolEditorWindow)EditorWindow.GetWindow(typeof(MapAnnotationToolEditorWindow), false, "MapTool");
        window.Show();
    }

    private void Awake()
    {
        waypointButtonImages = new Texture[4];
        waypointButtonImages[0] = (Texture)EditorGUIUtility.Load("MapUIWaypoint.png");
        waypointButtonImages[1] = (Texture)EditorGUIUtility.Load("MapUIStraight.png");
        waypointButtonImages[2] = (Texture)EditorGUIUtility.Load("MapUICurved.png");
        waypointButtonImages[3] = (Texture)EditorGUIUtility.Load("MapUIDelete.png");
        boundryImages = new Texture[8];
        boundryImages[0] = (Texture)EditorGUIUtility.Load("MapUIBoundryUnknown.png");
        boundryImages[1] = (Texture)EditorGUIUtility.Load("MapUIBoundryDotYellow.png");
        boundryImages[2] = (Texture)EditorGUIUtility.Load("MapUIBoundryDotWhite.png");
        boundryImages[3] = (Texture)EditorGUIUtility.Load("MapUIBoundrySolidYellow.png");
        boundryImages[4] = (Texture)EditorGUIUtility.Load("MapUIBoundrySolidWhite.png");
        boundryImages[5] = (Texture)EditorGUIUtility.Load("MapUIBoundryDoubleYellow.png");
        boundryImages[6] = (Texture)EditorGUIUtility.Load("MapUIBoundryCurb.png");
        boundryImages[7] = (Texture)EditorGUIUtility.Load("MapUIBoundryDoubleWhite.png");
        signalImages = new Texture[5];
        signalImages[0] = (Texture)EditorGUIUtility.Load("MapUISignalHorizontal2.png");
        signalImages[1] = (Texture)EditorGUIUtility.Load("MapUISignalVertical2.png");
        signalImages[2] = (Texture)EditorGUIUtility.Load("MapUISignalHorizontal3.png");
        signalImages[3] = (Texture)EditorGUIUtility.Load("MapUISignalVertical3.png");
        signalImages[4] = (Texture)EditorGUIUtility.Load("MapUISignalSingle.png");

        boundryTypeContent = new GUIContent[] {
            new GUIContent { image = boundryImages[0], tooltip = "Unknown boundry" },
            new GUIContent { image = boundryImages[1], tooltip = "Dotted yellow boundry" },
            new GUIContent { image = boundryImages[2], tooltip = "Dotted white boundry" },
            new GUIContent { image = boundryImages[3], tooltip = "Solid yellow boundry" },
            new GUIContent { image = boundryImages[4], tooltip = "Solid white boundry" },
            new GUIContent { image = boundryImages[5], tooltip = "Double yellow boundry" },
            new GUIContent { image = boundryImages[6], tooltip = "Curb boundry" },
        };
        boundryLineTypeContent = new GUIContent[] {
            new GUIContent { image = boundryImages[0], tooltip = "Unknown boundry line" },
            new GUIContent { image = boundryImages[4], tooltip = "Solid white boundry line" },
            new GUIContent { image = boundryImages[3], tooltip = "Solid yellow boundry line" },
            new GUIContent { image = boundryImages[2], tooltip = "Dotted white boundry line" },
            new GUIContent { image = boundryImages[1], tooltip = "Dotted yellow boundry line" },
            new GUIContent { image = boundryImages[7], tooltip = "Double white boundry line" },
            new GUIContent { image = boundryImages[5], tooltip = "Double yellow boundry line" },
            new GUIContent { image = boundryImages[6], tooltip = "Curb boundry line" },
        };
        signalTypeContent = new GUIContent[] {
            new GUIContent { image = boundryImages[0], tooltip = "Unknown signal type" },
            new GUIContent { image = signalImages[0], tooltip = "Horizontal signal with 2 lights" },
            new GUIContent { image = signalImages[1], tooltip = "Vertical signal with 2 lights" },
            new GUIContent { image = signalImages[2], tooltip = "Horizontal signal with 3 lights" },
            new GUIContent { image = signalImages[3], tooltip = "Vertical signal with 3 lights" },
            new GUIContent { image = signalImages[4], tooltip = "Single signal" },
        };
    }

    private void OnEnable()
    {
        layerMask = 1 << LayerMask.NameToLayer("Default");
        if (targetWaypointGO != null)
            DestroyImmediate(targetWaypointGO);
    }

    private void OnGUI()
    {
        // styles
        titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Map Annotation Modes", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);

        // modes
        if (GUILayout.Button(new GUIContent("Show Help Text Mode: " + MapAnnotationTool.SHOW_HELP, "Toggle help text visible"), GUILayout.Height(25)))
        {
            ToggleHelpText();
        }
        if (GUILayout.Button(new GUIContent("Show Map All Mode: " + MapAnnotationTool.SHOW_MAP_ALL, "Toggle all annotation in scene visible"), GUILayout.Height(25)))
        {
            ToggleMapAll();
        }
        if (GUILayout.Button(new GUIContent("Show Map Selected Mode: " + MapAnnotationTool.SHOW_MAP_SELECTED, "Toggle selected annotation in scene visible"), GUILayout.Height(25)))
        {
            ToggleMapSelected();
        }
        if (GUILayout.Button(new GUIContent("Create Lane/Line Mode: " + MapAnnotationTool.CREATE_LANE_LINE_MODE, "Enter mode to create waypoints for annotation creation"), GUILayout.Height(25)))
        {
            ToggleLaneLineMode();
        }
        if (GUILayout.Button(new GUIContent("Create Signal Mode: " + MapAnnotationTool.CREATE_SIGNAL_MODE, "Enter mode to create waypoints for annotation creation"), GUILayout.Height(25)))
        {
            ToggleSignalMode();
        }

        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
        
        // lane line
        if (MapAnnotationTool.CREATE_LANE_LINE_MODE)
        {
            EditorGUILayout.LabelField("Create Lane/Line", titleLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(20);
            parentObj = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Parent Object", "This object will hold all new annotation objects created"), parentObj, typeof(GameObject), true);
            LayerMask tempMask = EditorGUILayout.MaskField(new GUIContent("Ground Snap Layer Mask", "The ground and road layer to snap objects waypoints"),
                                                           UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask),
                                                           UnityEditorInternal.InternalEditorUtility.layers);
            layerMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Map Object Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
            createType = GUILayout.Toolbar(createType, createTypeContent);

            switch (createType)
            {
                case 0:
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Lane Turn Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    laneTurnType = GUILayout.Toolbar(laneTurnType, laneTurnTypeContent);
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Left Boundry Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    laneLeftBoundryType = GUILayout.SelectionGrid(laneLeftBoundryType, boundryTypeContent, 7);
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Right Boundry Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    laneRightBoundryType = GUILayout.SelectionGrid(laneRightBoundryType, boundryTypeContent, 7);
                    GUILayout.Space(10);
                    laneSpeedLimit = EditorGUILayout.IntField(new GUIContent("Speed Limit", "Lane speed limit in MPH"), laneSpeedLimit);
                    break;
                case 1:
                    GUILayout.Space(10);
                    isStopSign = GUILayout.Toggle(isStopSign, "Is this a stop sign?");
                    break;
                case 2:
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Boundry Line Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    boundryLineType = GUILayout.SelectionGrid(boundryLineType, boundryLineTypeContent, 8);
                    break;
                default:
                    break;
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Waypoint Connect", subtitleLabelStyle, GUILayout.ExpandWidth(true));
            waypointTotal = EditorGUILayout.IntField(new GUIContent("Waypoint count", "Number of waypoints when connected *MINIMUM 2 for straight 3 for curved*"), waypointTotal);
            if (waypointTotal < 3) waypointTotal = 3;
            GUILayout.BeginHorizontal("create");
            if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer")))
                CreateTempWaypoint();
            if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a straight line")))
                CreateStraight();
            if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[2], "Connect waypoints to make a curved line")))
                CreateCurved();
            if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints")))
                ClearAllTempWaypoints();
            GUILayout.EndHorizontal();
        }

        // signal
        if (MapAnnotationTool.CREATE_SIGNAL_MODE)
        {
            EditorGUILayout.LabelField("Create Signal", titleLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(20);
            parentObj = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Parent Object", "This object will hold all new annotation objects created"), parentObj, typeof(GameObject), true);
            signalMesh = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Signal Mesh Object", "The mesh for the signal annotation"), signalMesh, typeof(GameObject), true);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Signal Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
            signalType = GUILayout.SelectionGrid(signalType, signalTypeContent, 6);

            GUILayout.Space(5);
            if (GUILayout.Button(new GUIContent("Create Signal", "Create signal"), GUILayout.Height(25)))
                CreateSignal();
            
        }

        // extras

    }

    private void Update()
    {
        if (MapAnnotationTool.CREATE_LANE_LINE_MODE && targetWaypointGO != null)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, 1000.0f, layerMask.value))
                targetWaypointGO.transform.position = hit.point;
            
            SceneView.RepaintAll();
        }
    }

    private void OnDestroy()
    {
        ClearModes();
    }

    private void ToggleHelpText()
    {
        MapAnnotationTool.SHOW_HELP = !MapAnnotationTool.SHOW_HELP;
        SceneView.RepaintAll();
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

    private void ToggleLaneLineMode()
    {
        MapAnnotationTool.CREATE_LANE_LINE_MODE = !MapAnnotationTool.CREATE_LANE_LINE_MODE;
        MapAnnotationTool.CREATE_SIGNAL_MODE = false;
        MapAnnotationTool.SHOW_MAP_ALL = MapAnnotationTool.CREATE_LANE_LINE_MODE ? true : false;
        MapAnnotationTool.SHOW_MAP_SELECTED = MapAnnotationTool.CREATE_LANE_LINE_MODE ? true : false;
        if (MapAnnotationTool.CREATE_LANE_LINE_MODE)
        {
            CreateTargetWaypoint();
        }
        else
        {
            if (targetWaypointGO != null)
                DestroyImmediate(targetWaypointGO);
            ClearAllTempWaypoints();
        }
    }

    private void ToggleSignalMode()
    {
        MapAnnotationTool.CREATE_SIGNAL_MODE = !MapAnnotationTool.CREATE_SIGNAL_MODE;
        MapAnnotationTool.CREATE_LANE_LINE_MODE = false;
        MapAnnotationTool.SHOW_MAP_ALL = MapAnnotationTool.CREATE_SIGNAL_MODE ? true : false;
        MapAnnotationTool.SHOW_MAP_SELECTED = MapAnnotationTool.CREATE_SIGNAL_MODE ? true : false;
        if (MapAnnotationTool.CREATE_SIGNAL_MODE)
        {
            signalMesh = null;
        }
        else
        {
            signalMesh = null;
        }
    }

    private void ClearModes()
    {
        MapAnnotationTool.CREATE_LANE_LINE_MODE = false;
        MapAnnotationTool.CREATE_SIGNAL_MODE = false;
        MapAnnotationTool.SHOW_MAP_ALL = false;
        MapAnnotationTool.SHOW_MAP_SELECTED = false;
        if (targetWaypointGO != null)
            DestroyImmediate(targetWaypointGO);
        ClearAllTempWaypoints();
    }

    private void CreateTargetWaypoint()
    {
        var cam = SceneView.lastActiveSceneView.camera;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, layerMask.value))
        {
            targetWaypointGO = new GameObject("TARGET_WAYPOINT");
            targetWaypointGO.transform.position = hit.point;
            targetWaypointGO.AddComponent<MapTargetWaypoint>().layerMask = layerMask;
            Undo.RegisterCreatedObjectUndo(targetWaypointGO, nameof(targetWaypointGO));
        }
        SceneView.RepaintAll();
    }

    private void CreateTempWaypoint()
    {
        var cam = SceneView.lastActiveSceneView.camera;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, layerMask.value))
        {
            var tempWaypointGO = new GameObject("TEMP_WAYPOINT");
            tempWaypointGO.transform.position = hit.point;
            tempWaypointGO.AddComponent<MapWaypoint>().layerMask = layerMask;
            tempWaypoints.Add(tempWaypointGO.GetComponent<MapWaypoint>());
            Undo.RegisterCreatedObjectUndo(tempWaypointGO, nameof(tempWaypointGO));
        }
        SceneView.RepaintAll();
    }

    private void ClearAllTempWaypoints()
    {
        for (int i = 0; i < tempWaypoints.Count; i++)
            Undo.DestroyObjectImmediate(tempWaypoints[i].gameObject);
        tempWaypoints.Clear();
        SceneView.RepaintAll();
    }

    private void CreateStraight()
    {
        tempWaypoints.RemoveAll(p => p == null);
        if (tempWaypoints.Count != 2)
        {
            Debug.Log("You need two temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject();
        switch (createType)
        {
            case 0:
                newGo.name = "MapLane";
                newGo.AddComponent<MapLane>();
                break;
            case 1:
                newGo.name = "MapLineStop";
                newGo.AddComponent<MapLine>();
                break;
            case 2:
                newGo.name = "MapLineBoundry";
                newGo.AddComponent<MapLine>();
                break;
        }
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        
        Vector3 avePos = Vector3.Lerp(tempWaypoints[0].transform.position, tempWaypoints[1].transform.position, 0.5f);
        newGo.transform.position = avePos;
        var dir = (tempWaypoints[1].transform.position - tempWaypoints[0].transform.position).normalized;
        newGo.transform.rotation = Quaternion.LookRotation(dir);

        float t = 0f;
        Vector3 position = Vector3.zero;
        Vector3 p0 = tempWaypoints[0].transform.position;
        Vector3 p1 = tempWaypoints[1].transform.position;
        List<Vector3> tempLocalPos = new List<Vector3>();
        for (int i = 0; i < waypointTotal; i++)
        {
            t = i / (waypointTotal - 1.0f);
            position = (1.0f - t) * p0 + t * p1;
            tempLocalPos.Add(position);
        }
        
        foreach (var p in tempLocalPos)
        {
            switch (createType)
            {
                case 0: // lane
                    newGo.GetComponent<MapLane>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLane>().laneTurnType = (MapData.LaneTurnType)laneTurnType + 1;
                    newGo.GetComponent<MapLane>().leftBoundType = (MapData.LaneBoundaryType)laneLeftBoundryType;
                    newGo.GetComponent<MapLane>().rightBoundType = (MapData.LaneBoundaryType)laneRightBoundryType;
                    newGo.GetComponent<MapLane>().speedLimit = laneSpeedLimit;
                    break;
                case 1: // stopline
                    newGo.GetComponent<MapLine>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLine>().lineType = MapData.LineType.STOP;
                    newGo.GetComponent<MapLine>().isStopSign = isStopSign;
                    break;
                case 2: // boundry line
                    newGo.GetComponent<MapLine>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLine>().lineType = (MapData.LineType)boundryLineType + 1;
                    break;
            }
        }
        
        newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);

        tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints.Clear();

        Selection.activeObject = newGo;
    }

    private void CreateCurved()
    {
        tempWaypoints.RemoveAll(p => p == null);
        if (tempWaypoints.Count != 3)
        {
            Debug.Log("You need three temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject();
        switch (createType)
        {
            case 0:
                newGo.name = "MapLane";
                newGo.AddComponent<MapLane>();
                break;
            case 1:
                newGo.name = "MapLineStop";
                newGo.AddComponent<MapLine>();
                break;
            case 2:
                newGo.name = "MapLineBoundry";
                newGo.AddComponent<MapLine>();
                break;
        }
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));

        Vector3 avePos = Vector3.Lerp(tempWaypoints[0].transform.position, tempWaypoints[2].transform.position, 0.5f);
        newGo.transform.position = avePos;
        
        float t = 0f;
        Vector3 position = Vector3.zero;
        Vector3 p0 = tempWaypoints[0].transform.position;
        Vector3 p1 = tempWaypoints[1].transform.position;
        Vector3 p2 = tempWaypoints[2].transform.position;
        List<Vector3> tempLocalPos = new List<Vector3>();
        for (int i = 0; i < waypointTotal; i++)
        {
            t = i / (waypointTotal - 1.0f);
            position = (1.0f - t) * (1.0f - t) * p0 + 2.0f * (1.0f - t) * t * p1 + t * t * p2;
            tempLocalPos.Add(position);
        }

        var dir = (tempLocalPos[1] - tempLocalPos[0]).normalized;
        newGo.transform.rotation = Quaternion.LookRotation(dir);

        foreach (var p in tempLocalPos)
        {
            switch (createType)
            {
                case 0: // lane
                    newGo.GetComponent<MapLane>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLane>().laneTurnType = (MapData.LaneTurnType)laneTurnType + 1;
                    newGo.GetComponent<MapLane>().leftBoundType = (MapData.LaneBoundaryType)laneLeftBoundryType;
                    newGo.GetComponent<MapLane>().rightBoundType = (MapData.LaneBoundaryType)laneRightBoundryType;
                    newGo.GetComponent<MapLane>().speedLimit = laneSpeedLimit;
                    break;
                case 1: // stopline
                    newGo.GetComponent<MapLine>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLine>().lineType = MapData.LineType.STOP;
                    newGo.GetComponent<MapLine>().isStopSign = isStopSign;
                    break;
                case 2: // boundry line
                    newGo.GetComponent<MapLine>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLine>().lineType = (MapData.LineType)boundryLineType + 1;
                    break;
            }
        }

        newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);

        tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints.Clear();

        Selection.activeObject = newGo;
    }

    private void CreateSignal()
    {
        //UNKNOWN = 1,
        //MIX_2_HORIZONTAL = 2,
        //MIX_2_VERTICAL = 3,
        //MIX_3_HORIZONTAL = 4,
        //MIX_3_VERTICAL = 5,
        //SINGLE = 6,
    }
}
