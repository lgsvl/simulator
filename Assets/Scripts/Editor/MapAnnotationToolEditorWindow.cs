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
    private GUIContent[] createModeContent;
    private List<MapWaypoint> tempWaypoints = new List<MapWaypoint>();
    private GameObject parentObj = null;
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
    private int stopLineFacing = 0;
    private Texture[] stopLineFacingImages;
    private GUIContent[] stopLineFacingContent;
    private bool isStopSign = false;
    private int boundryLineType = 3;
    private GUIContent[] boundryLineTypeContent;
    
    private Texture[] signalImages;
    private GUIContent[] signalTypeContent;
    private int signalType = 4;
    private int currentSignalForward = 0;
    private int currentSignalUp = 0;
    private Texture[] signalOrientationImages;
    private GUIContent[] signalOrientationForwardContent;
    private GUIContent[] signalOrientationUpContent;

    private enum SignType { STOP, YIELD };
    private SignType signType = SignType.STOP;
    
    [MenuItem("Simulator/MapTool #&m", false, 100)]
    public static void MapToolPanel()
    {
        MapAnnotationToolEditorWindow window = (MapAnnotationToolEditorWindow)EditorWindow.GetWindow(typeof(MapAnnotationToolEditorWindow), false, "MapTool");
        window.Show();
    }

    private void Awake()
    {
        waypointButtonImages = new Texture[4];
        waypointButtonImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIWaypoint.png");
        waypointButtonImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStraight.png");
        waypointButtonImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUICurved.png");
        waypointButtonImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIDelete.png");
        boundryImages = new Texture[8];
        boundryImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryUnknown.png");
        boundryImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryDotYellow.png");
        boundryImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryDotWhite.png");
        boundryImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundrySolidYellow.png");
        boundryImages[4] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundrySolidWhite.png");
        boundryImages[5] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryDoubleYellow.png");
        boundryImages[6] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryCurb.png");
        boundryImages[7] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryDoubleWhite.png");
        stopLineFacingImages = new Texture[2];
        stopLineFacingImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStoplineRight.png");
        stopLineFacingImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStoplineLeft.png");
        signalImages = new Texture[5];
        signalImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalHorizontal2.png");
        signalImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalVertical2.png");
        signalImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalHorizontal3.png");
        signalImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalVertical3.png");
        signalImages[4] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalSingle.png");
        signalOrientationImages = new Texture[4];
        signalOrientationImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalForward.png");
        signalOrientationImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalUp.png");
        signalOrientationImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalBack.png");
        signalOrientationImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalDown.png");

        createModeContent = new GUIContent[] {
            new GUIContent { text = "None", tooltip = "None"},
            new GUIContent { text = "Lane/Line", tooltip = "Lane and Line creation mode"},
            new GUIContent { text = "Signal", tooltip = "Signal creation mode"},
            new GUIContent { text = "Sign", tooltip = "Sign creation mode"},
            new GUIContent { text = "Pole", tooltip = "Pole creation mode"},
        };
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
        stopLineFacingContent = new GUIContent[] {
            new GUIContent { text = "Forward Right", image = stopLineFacingImages[0], tooltip = "Transform forward vector is right of waypoints direction"},
            new GUIContent { text = "Forward Left", image = stopLineFacingImages[1], tooltip = "Transform forward vector is left of waypoints direction"},
        };
        signalTypeContent = new GUIContent[] {
            new GUIContent { image = boundryImages[0], tooltip = "Unknown signal type" },
            new GUIContent { image = signalImages[0], tooltip = "Horizontal signal with 2 lights" },
            new GUIContent { image = signalImages[1], tooltip = "Vertical signal with 2 lights" },
            new GUIContent { image = signalImages[2], tooltip = "Horizontal signal with 3 lights" },
            new GUIContent { image = signalImages[3], tooltip = "Vertical signal with 3 lights" },
            new GUIContent { image = signalImages[4], tooltip = "Single signal" },
        };
        signalOrientationForwardContent = new GUIContent[] {
            new GUIContent { text = " Z", image = signalOrientationImages[0], tooltip = "Mesh is Z forward"},
            new GUIContent { text = "-Z", image = signalOrientationImages[0], tooltip = "Mesh is -Z forward"},
            new GUIContent { text = " X", image = signalOrientationImages[0], tooltip = "Mesh is X forward"},
            new GUIContent { text = "-X", image = signalOrientationImages[0], tooltip = "Mesh is -X forward"},
            new GUIContent { text = " Y", image = signalOrientationImages[0], tooltip = "Mesh is Y forward"},
            new GUIContent { text = "-Y", image = signalOrientationImages[0], tooltip = "Mesh is -Y forward"},
        };
        signalOrientationUpContent = new GUIContent[] {
            new GUIContent { text = " Y", image = signalOrientationImages[1], tooltip = "Mesh is Y up"},
            new GUIContent { text = "-Y", image = signalOrientationImages[1], tooltip = "Mesh is -Y up"},
            new GUIContent { text = " X", image = signalOrientationImages[1], tooltip = "Mesh is X up"},
            new GUIContent { text = "-X", image = signalOrientationImages[1], tooltip = "Mesh is -X up"},
            new GUIContent { text = " Z", image = signalOrientationImages[1], tooltip = "Mesh is Z up"},
            new GUIContent { text = "-Z", image = signalOrientationImages[1], tooltip = "Mesh is -Z up"},
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
        var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Map Annotation Tool", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);

        // modes
        EditorGUILayout.LabelField("View Modes", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.BeginHorizontal("box");
        var prevHelp = MapAnnotationTool.SHOW_HELP;
        MapAnnotationTool.SHOW_HELP = GUILayout.Toggle(MapAnnotationTool.SHOW_HELP, "View Help", new GUIStyle("Button"), GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false));
        if (prevHelp != MapAnnotationTool.SHOW_HELP) SceneView.RepaintAll();

        var prevAll = MapAnnotationTool.SHOW_MAP_ALL;
        MapAnnotationTool.SHOW_MAP_ALL = GUILayout.Toggle(MapAnnotationTool.SHOW_MAP_ALL, "View All", new GUIStyle("Button"), GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false));
        if (prevAll != MapAnnotationTool.SHOW_MAP_ALL) SceneView.RepaintAll();

        var prevSelected = MapAnnotationTool.SHOW_MAP_SELECTED;
        MapAnnotationTool.SHOW_MAP_SELECTED = GUILayout.Toggle(MapAnnotationTool.SHOW_MAP_SELECTED, "View Selected", new GUIStyle("Button"), GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false));
        if (prevSelected != MapAnnotationTool.SHOW_MAP_SELECTED) SceneView.RepaintAll();
        GUILayout.EndHorizontal();
        GUILayout.Space(20);

        EditorGUILayout.LabelField("Create Modes", titleLabelStyle, GUILayout.ExpandWidth(true));
        var prevCreateMode = MapAnnotationTool.createMode;
        MapAnnotationTool.createMode = (MapAnnotationTool.CreateMode)GUILayout.SelectionGrid((int)MapAnnotationTool.createMode, createModeContent, 5);
        if (prevCreateMode != MapAnnotationTool.createMode)
            ChangeCreateMode();
        
        GUILayout.Space(10);
        parentObj = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Parent Object", "This object will hold all new annotation objects created"), parentObj, typeof(GameObject), true);
        LayerMask tempMask = EditorGUILayout.MaskField(new GUIContent("Ground Snap Layer Mask", "The ground and road layer to snap objects waypoints"),
                                                       UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask),
                                                       UnityEditorInternal.InternalEditorUtility.layers);
        layerMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);

        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
                break;
            case MapAnnotationTool.CreateMode.LANE_LINE:
                EditorGUILayout.LabelField("Create Lane/Line", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);
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
                        laneSpeedLimit = EditorGUILayout.IntField(new GUIContent("Speed Limit", "Lane speed limit"), laneSpeedLimit);
                        break;
                    case 1:
                        GUILayout.Space(10);
                        stopLineFacing = GUILayout.SelectionGrid(stopLineFacing, stopLineFacingContent, 2);
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
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer")))
                    CreateTempWaypoint();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a straight line")))
                    CreateStraight();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[2], "Connect waypoints to make a curved line")))
                    CreateCurved();
                if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints")))
                    ClearAllTempWaypoints();
                GUILayout.EndHorizontal();
                break;
            case MapAnnotationTool.CreateMode.SIGNAL:
                EditorGUILayout.LabelField("Create Signal", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);
                EditorGUILayout.LabelField("Signal Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                signalType = GUILayout.SelectionGrid(signalType, signalTypeContent, 6);
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Signal Mesh Rotation", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                currentSignalForward = EditorGUILayout.IntPopup(new GUIContent("Forward Vector: "), currentSignalForward, signalOrientationForwardContent, new int[] { 0, 1, 2, 3, 4, 5 }, GUILayout.MinWidth(0));
                currentSignalUp = EditorGUILayout.IntPopup(new GUIContent("Up Vector: "), currentSignalUp, signalOrientationUpContent, new int[] { 0, 1, 2, 3, 4, 5 }, GUILayout.MinWidth(0));
                GUILayout.Space(5);
                if (GUILayout.Button(new GUIContent("Create Signal", "Create signal"), GUILayout.Height(25)))
                    CreateSignal();
                break;
            case MapAnnotationTool.CreateMode.SIGN:
                EditorGUILayout.LabelField("Create Sign", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);
                signType = (SignType)EditorGUILayout.EnumPopup("Sign type", signType);
                GUILayout.Space(5);
                if (GUILayout.Button(new GUIContent("Create Sign", "Create sign"), GUILayout.Height(25)))
                    CreateSign();
                break;
            case MapAnnotationTool.CreateMode.POLE:
                EditorGUILayout.LabelField("Create Pole", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);
                parentObj = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Parent Object", "This object will hold all new annotation objects created"), parentObj, typeof(GameObject), true);
                GUILayout.Space(15);
                if (GUILayout.Button(new GUIContent("Create Pole", "Create pole"), GUILayout.Height(25)))
                    CreatePole();
                break;
            default:
                break;
        }
    }

    private void Update()
    {
        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
                break;
            case MapAnnotationTool.CreateMode.SIGNAL:
                break;
            case MapAnnotationTool.CreateMode.LANE_LINE:
            case MapAnnotationTool.CreateMode.SIGN:
            case MapAnnotationTool.CreateMode.POLE:
                if (targetWaypointGO == null) return;
                var cam = SceneView.lastActiveSceneView.camera;
                if (cam == null) return;
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(ray, out hit, 1000.0f, layerMask.value))
                    targetWaypointGO.transform.position = hit.point;
                break;
        }
    }

    private void OnDestroy()
    {
        ClearModes();
    }

    private void ChangeCreateMode()
    {
        ClearTargetWaypoint();
        ClearAllTempWaypoints();
        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
                break;
            case MapAnnotationTool.CreateMode.LANE_LINE:
                MapAnnotationTool.SHOW_MAP_ALL = true;
                MapAnnotationTool.SHOW_MAP_SELECTED = true;
                CreateTargetWaypoint();
                break;
            case MapAnnotationTool.CreateMode.SIGNAL:
                MapAnnotationTool.SHOW_MAP_ALL = true;
                MapAnnotationTool.SHOW_MAP_SELECTED = true;
                break;
            case MapAnnotationTool.CreateMode.SIGN:
                MapAnnotationTool.SHOW_MAP_ALL = true;
                MapAnnotationTool.SHOW_MAP_SELECTED = true;
                CreateTargetWaypoint();
                break;
            case MapAnnotationTool.CreateMode.POLE:
                MapAnnotationTool.SHOW_MAP_ALL = true;
                MapAnnotationTool.SHOW_MAP_SELECTED = true;
                CreateTargetWaypoint();
                break;
        }
    }

    private void ClearModes()
    {
        MapAnnotationTool.SHOW_HELP = false;
        MapAnnotationTool.SHOW_MAP_ALL = false;
        MapAnnotationTool.SHOW_MAP_SELECTED = false;
        ClearTargetWaypoint();
        ClearAllTempWaypoints();
    }

    private void ClearTargetWaypoint()
    {
        targetWaypointGO = null;
        List<MapTargetWaypoint> missedTargetWP = new List<MapTargetWaypoint>();
        missedTargetWP.AddRange(FindObjectsOfType<MapTargetWaypoint>());
        for (int i = 0; i < missedTargetWP.Count; i++)
            Undo.DestroyObjectImmediate(missedTargetWP[i].gameObject);
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
            targetWaypointGO.hideFlags = HideFlags.HideInInspector;
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
        tempWaypoints.Clear();
        List<MapWaypoint> missedWP = new List<MapWaypoint>();
        missedWP.AddRange(FindObjectsOfType<MapWaypoint>());
        for (int i = 0; i < missedWP.Count; i++)
            Undo.DestroyObjectImmediate(missedWP[i].gameObject);
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
        if (createType == 1)
        {
            newGo.transform.rotation = Quaternion.LookRotation(dir);
            if (stopLineFacing == 0)
                newGo.transform.rotation = Quaternion.LookRotation(newGo.transform.TransformDirection(Vector3.right).normalized, newGo.transform.TransformDirection(Vector3.up).normalized);
            else
                newGo.transform.rotation = Quaternion.LookRotation(newGo.transform.TransformDirection(-Vector3.right).normalized, newGo.transform.TransformDirection(Vector3.up).normalized);
        }
        else
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
        var signalMesh = Selection.activeTransform;
        if (signalMesh == null)
        {
            Debug.Log("Must have a signal mesh selected");
            return;
        }
        
        var newGo = new GameObject("MapSignal");
        Undo.RegisterCreatedObjectUndo(newGo, newGo.name);
        var signal = newGo.AddComponent<MapSignal>();
        signal.signalLightMesh = signalMesh.GetComponent<Renderer>();

        if (signal.signalLightMesh == null)
        {
            Debug.Log("Signal mesh must have Renderer Component");
            DestroyImmediate(newGo);
            return;
        }

        Vector3 targetFwdVec = Vector3.forward;
        Vector3 targetUpVec = Vector3.up;
        switch (signalType)
        {
            case 0: // unknown
                signal.signalData = new List<MapData.SignalData>();
                break;
            case 1: // horizontal 2
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.right * 0.25f, signalColor = MapData.SignalColorType.Red },
                    new MapData.SignalData() { localPosition = Vector3.right * -0.25f, signalColor = MapData.SignalColorType.Green },
                };
                break;
            case 2: // vertical 2
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.up * 0.25f, signalColor = MapData.SignalColorType.Red },
                    new MapData.SignalData() { localPosition = Vector3.up * -0.25f, signalColor = MapData.SignalColorType.Green },
                };
                break;
            case 3: // horizontal 3
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.right * 0.4f, signalColor = MapData.SignalColorType.Red },
                    new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Yellow },
                    new MapData.SignalData() { localPosition = Vector3.right * -0.4f, signalColor = MapData.SignalColorType.Green },
                };
                break;
            case 4: // vertical 3
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.up * 0.4f, signalColor = MapData.SignalColorType.Red },
                    new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Yellow },
                    new MapData.SignalData() { localPosition = Vector3.up * -0.4f, signalColor = MapData.SignalColorType.Green },
                };
                break;
            case 5: // single
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Red },
                };
                break;
        }
        switch (currentSignalForward)
        {
            case 0: // z
                targetFwdVec = Vector3.forward;
                signal.boundScale = new Vector3(signal.signalLightMesh.bounds.size.x, signal.signalLightMesh.bounds.size.y, 0f);
                break;
            case 1: // -z
                targetFwdVec = -Vector3.forward;
                signal.boundScale = new Vector3(signal.signalLightMesh.bounds.size.x, signal.signalLightMesh.bounds.size.y, 0f);
                break;
            case 2: // x
                targetFwdVec = Vector3.right;
                signal.boundScale = new Vector3(signal.signalLightMesh.bounds.size.z, signal.signalLightMesh.bounds.size.y, 0f);
                break;
            case 3: // -x
                targetFwdVec = -Vector3.right;
                signal.boundScale = new Vector3(signal.signalLightMesh.bounds.size.z, signal.signalLightMesh.bounds.size.y, 0f);
                break;
            case 4: // y
                targetFwdVec = Vector3.up;
                signal.boundScale = new Vector3(signal.signalLightMesh.bounds.size.z, signal.signalLightMesh.bounds.size.y, 0f);
                break;
            case 5: // -y
                targetFwdVec = -Vector3.up;
                signal.boundScale = new Vector3(signal.signalLightMesh.bounds.size.z, signal.signalLightMesh.bounds.size.y, 0f);
                break;
        }
        switch (currentSignalUp)
        {
            case 0: // y
                targetUpVec = Vector3.up;
                break;
            case 1: // -y
                targetUpVec = -Vector3.up;
                break;
            case 2: // x
                targetUpVec = Vector3.right;
                break;
            case 3: // -x
                targetUpVec = -Vector3.right;
                break;
            case 4: // z
                targetUpVec = Vector3.forward;
                break;
            case 5: // -z
                targetUpVec = -Vector3.forward;
                break;
        }
        
        targetFwdVec = signalMesh.transform.TransformDirection(targetFwdVec).normalized;
        targetUpVec = signalMesh.transform.TransformDirection(targetUpVec).normalized;
        newGo.transform.rotation = Quaternion.LookRotation(targetFwdVec, targetUpVec);

        newGo.transform.position = signal.signalLightMesh.bounds.center;
        newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);
        Selection.activeObject = newGo;
    }

    private void CreateSign()
    {
        var newGo = new GameObject("MapSign");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var sign = newGo.AddComponent<MapSign>();
        sign.signType = signType == SignType.STOP ? MapData.SignType.STOP : MapData.SignType.YIELD;
        newGo.transform.position = targetWaypointGO.transform.position;
        newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);
        Selection.activeObject = newGo;
    }

    private void CreatePole()
    {
        var newGo = new GameObject("MapSign");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var sign = newGo.AddComponent<MapPole>();

        newGo.transform.position = targetWaypointGO.transform.position;
        newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);
        Selection.activeObject = newGo;
    }
}
