/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using Simulator.Map;
using System.Linq;
using UnityEditorInternal;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using Simulator.Editor;

public class MapAnnotations : EditorWindow
{
    private GUIContent[] createModeContent;
    private List<MapWaypoint> tempWaypoints = new List<MapWaypoint>();
    private GameObject parentObj = null;
    private LayerMask layerMask;
    private GameObject targetWaypointGO;
    private Texture[] waypointButtonImages;
    private int waypointTotal = 1;
    private GUIContent[] holderTypeContent = {
        new GUIContent { text = "Intersection", tooltip = "Create a new Intersection holder" },
        new GUIContent { text = "Lane Section", tooltip = "Create a new Lane Section holder" },
    };
    private MapHolder mapHolder;
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
    private float laneSpeedLimit = 11.176f;
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
    private int currentSignForward = 0;
    private int currentSignUp = 0;

    private int pedType = 0;
    private GUIContent[] pedTypeContent = {
        new GUIContent { text = "Sidewalk", tooltip = "Set sidewalk pedestrian path" },
        new GUIContent { text = "Crosswalk", tooltip = "Set crosswalk pedestrian path" },
        new GUIContent { text = "Jaywalk", tooltip = "Set jaywalk pedestrian path" }
    };

    private SerializedObject serializedObject;
    private SerializedProperty mapLaneProperty;
    private Vector2 scrollPos;
    private int ExtraLinesCnt;

    [MenuItem("Simulator/Annotate HD Map #&m", false, 100)]
    public static void Open()
    {
        var window = GetWindow(typeof(MapAnnotations), false, "HD Map Annotation");
        window.Show();
    }

    private void Awake()
    {
        waypointButtonImages = new Texture[4];
        stopLineFacingImages = new Texture[2];
        signalImages = new Texture[5];
        signalOrientationImages = new Texture[4];
        boundryImages = new Texture[9];
        if (!EditorGUIUtility.isProSkin)
        {
            waypointButtonImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIWaypoint.png");
            waypointButtonImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStraight.png");
            waypointButtonImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUICurved.png");
            waypointButtonImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIDelete.png");

            stopLineFacingImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStoplineRight.png");
            stopLineFacingImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStoplineLeft.png");

            signalImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalHorizontal2.png");
            signalImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalVertical2.png");
            signalImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalHorizontal3.png");
            signalImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalVertical3.png");
            signalImages[4] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalSingle.png");

            signalOrientationImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalForward.png");
            signalOrientationImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalUp.png");
            signalOrientationImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalBack.png");
            signalOrientationImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalDown.png");

            boundryImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryUnknown.png");
        }
        else
        {
            waypointButtonImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIWaypointPro.png");
            waypointButtonImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStraightPro.png");
            waypointButtonImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUICurvedPro.png");
            waypointButtonImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIDeletePro.png");

            stopLineFacingImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStoplineRightPro.png");
            stopLineFacingImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIStoplineLeftPro.png");

            signalImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalHorizontal2Pro.png");
            signalImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalVertical2Pro.png");
            signalImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalHorizontal3Pro.png");
            signalImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalVertical3Pro.png");
            signalImages[4] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalSinglePro.png");

            signalOrientationImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalForwardPro.png");
            signalOrientationImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalUpPro.png");
            signalOrientationImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalBackPro.png");
            signalOrientationImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUISignalDownPro.png");

            boundryImages[0] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryUnknownPro.png");
        }
        boundryImages[1] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryDotYellow.png");
        boundryImages[2] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryDotWhite.png");
        boundryImages[3] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundrySolidYellow.png");
        boundryImages[4] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundrySolidWhite.png");
        boundryImages[5] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryDoubleYellow.png");
        boundryImages[6] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryCurb.png");
        boundryImages[7] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryDoubleWhite.png");
        boundryImages[8] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MapUI/MapUIBoundryVirtual.png");
    
        createModeContent = new GUIContent[] {
            new GUIContent { text = "None", tooltip = "None"},
            new GUIContent { text = "Lane/Line", tooltip = "Lane and Line creation mode"},
            new GUIContent { text = "Signal", tooltip = "Signal creation mode"},
            new GUIContent { text = "Sign", tooltip = "Sign creation mode"},
            new GUIContent { text = "Pole", tooltip = "Pole creation mode"},
            new GUIContent { text = "Pedestrian", tooltip = "Pedestrian creation mode"},
            new GUIContent { text = "Junction", tooltip = "Junction creation mode"},
            new GUIContent { text = "CrossWalk", tooltip = "Cross walk creation mode"},
            new GUIContent { text = "ClearArea", tooltip = "Clear area creation mode"},
            new GUIContent { text = "ParkingSpace", tooltip = "Parking space creation mode"},
            new GUIContent { text = "SpeedBump", tooltip = "Speed bump creation mode"},

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
            new GUIContent { image = boundryImages[8], tooltip = "Virtual boundry line" },
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

        MapAnnotationTool.createMode = MapAnnotationTool.CreateMode.NONE;
        tempWaypoints.Clear();
    }

    private void OnEnable()
    {
        MapAnnotationTool.TOOL_ACTIVE = true;
        layerMask = 1 << LayerMask.NameToLayer("Default");
        if (targetWaypointGO != null)
            DestroyImmediate(targetWaypointGO);
        mapHolder = FindObjectOfType<MapHolder>();
    }

    private void OnSelectionChange()
    {
        Repaint();
        SceneView.RepaintAll();
    }

    private void OnFocus()
    {
        if (targetWaypointGO != null) return;

        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
            case MapAnnotationTool.CreateMode.SIGN:
            case MapAnnotationTool.CreateMode.SIGNAL:
                break;
            case MapAnnotationTool.CreateMode.LANE_LINE:
            case MapAnnotationTool.CreateMode.POLE:
            case MapAnnotationTool.CreateMode.PEDESTRIAN:
            default:
                CreateTargetWaypoint();
                break;
        }
    }

    private void OnLostFocus()
    {
        if (mouseOverWindow?.ToString().Trim().Replace("(", "").Replace(")", "") == "UnityEditor.SceneView") return;
        ClearTargetWaypoint();
    }

    private void OnDisable()
    {
        ClearModes();
    }

    private void Update()
    {
        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
            case MapAnnotationTool.CreateMode.SIGNAL:
            case MapAnnotationTool.CreateMode.SIGN:
                break;
            case MapAnnotationTool.CreateMode.LANE_LINE:
            case MapAnnotationTool.CreateMode.JUNCTION:
            case MapAnnotationTool.CreateMode.CROSSWALK:
            case MapAnnotationTool.CreateMode.CLEARAREA:
            case MapAnnotationTool.CreateMode.PARKINGSPACE:
            case MapAnnotationTool.CreateMode.SPEEDBUMP:
            case MapAnnotationTool.CreateMode.POLE:
            case MapAnnotationTool.CreateMode.PEDESTRIAN:
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

    private void OnGUI()
    {
        // styles
        var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
        var buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.onNormal.textColor = Color.white;
        var nonProColor = new Color(0.75f, 0.75f, 0.75f);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("HD Map Annotation", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (mapHolder == null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Create Map Annotation Holder", titleLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(20);
            if (!EditorGUIUtility.isProSkin)
                GUI.backgroundColor = nonProColor;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Create Map Holder", "Create a holder object in scene to hold annotation objects")))
                CreateMapHolder();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        GUILayout.Space(10);

        // modes
        EditorGUILayout.LabelField("View Modes", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.BeginHorizontal("box");
        if (!EditorGUIUtility.isProSkin)
            GUI.backgroundColor = nonProColor;
        var prevHelp = MapAnnotationTool.SHOW_HELP;
        MapAnnotationTool.SHOW_HELP = GUILayout.Toggle(MapAnnotationTool.SHOW_HELP, "View Help", buttonStyle, GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false));
        if (prevHelp != MapAnnotationTool.SHOW_HELP) SceneView.RepaintAll();

        var prevAll = MapAnnotationTool.SHOW_MAP_ALL;
        MapAnnotationTool.SHOW_MAP_ALL = GUILayout.Toggle(MapAnnotationTool.SHOW_MAP_ALL, "View All", buttonStyle, GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false));
        if (prevAll != MapAnnotationTool.SHOW_MAP_ALL) SceneView.RepaintAll();

        var prevSelected = MapAnnotationTool.SHOW_MAP_SELECTED;
        MapAnnotationTool.SHOW_MAP_SELECTED = GUILayout.Toggle(MapAnnotationTool.SHOW_MAP_SELECTED, "Lock View Selected", buttonStyle, GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false));
        if (prevSelected != MapAnnotationTool.SHOW_MAP_SELECTED)
        {
            if (MapAnnotationTool.SHOW_MAP_SELECTED)
            {
                var selectedMapData = new List<MapData>();
                foreach (var obj in Selection.gameObjects)
                {
                    selectedMapData.AddRange(obj.GetComponentsInChildren<MapData>());
                }
                foreach (var data in selectedMapData)
                {
                    data.selected = true;
                }
            }
            else
            {
                foreach (var data in FindObjectsOfType<MapData>())
                {
                    data.selected = false;
                }
            }
            SceneView.RepaintAll();
        }
        if (!EditorGUIUtility.isProSkin)
            GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.Space(20);

        EditorGUILayout.LabelField("Create Modes", titleLabelStyle, GUILayout.ExpandWidth(true));
        if (!EditorGUIUtility.isProSkin)
            GUI.backgroundColor = nonProColor;
        var prevCreateMode = MapAnnotationTool.createMode;
        MapAnnotationTool.createMode = (MapAnnotationTool.CreateMode)GUILayout.SelectionGrid((int)MapAnnotationTool.createMode, createModeContent, 3, buttonStyle);
        if (prevCreateMode != MapAnnotationTool.createMode)
            ChangeCreateMode();
        if (!EditorGUIUtility.isProSkin)
            GUI.backgroundColor = Color.white;
        GUILayout.Space(10);

        parentObj = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Parent Object", "This object will hold all new annotation objects created"), parentObj, typeof(GameObject), true);
        if (!EditorGUIUtility.isProSkin)
            GUI.backgroundColor = nonProColor;
        LayerMask tempMask = EditorGUILayout.MaskField(new GUIContent("Ground Snap Layer Mask", "The ground and road layer to snap objects waypoints"),
                                                       InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask),
                                                       InternalEditorUtility.layers, buttonStyle);
        layerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
        GUILayout.Space(10);

        EditorGUILayout.LabelField("Utilities", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Snap annotated positions to layer");
        if (GUILayout.Button(new GUIContent("Snap all", "Snap all annotated local positions to ground layer"), GUILayout.MaxWidth(100f)))
            SnapAllToLayer();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Remove extra boundary lines");
        if (GUILayout.Button(new GUIContent("Remove lines", "Remove extra boundary lines for parallel lanes"), GUILayout.MaxWidth(100f)))
            RemoveExtraLines();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Create fake boundary lines");
        if (GUILayout.Button(new GUIContent("Create lines", "Create fake boundary lines automatically for lanes without boundary lines"), GUILayout.MaxWidth(100f)))
            CreateLines();
        GUILayout.EndHorizontal();

        if (!EditorGUIUtility.isProSkin)
            GUI.backgroundColor = Color.white;
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
                EditorGUILayout.LabelField("Create None", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);
                break;
            case MapAnnotationTool.CreateMode.LANE_LINE:
                EditorGUILayout.LabelField("Create Intersection / Lane Section Holder", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(holderTypeContent[0]))
                    CreateIntersectionHolder();
                if (GUILayout.Button(holderTypeContent[1]))
                    CreateLaneSectionHolder();
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Create Lane/Line", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                EditorGUILayout.LabelField("Map Object Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                createType = GUILayout.Toolbar(createType, createTypeContent);
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                switch (createType)
                {
                    case 0: // lane
                        GUILayout.Space(10);
                        if (!EditorGUIUtility.isProSkin)
                            GUI.backgroundColor = nonProColor;
                        EditorGUILayout.LabelField("Lane Turn Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                        laneTurnType = GUILayout.Toolbar(laneTurnType, laneTurnTypeContent);
                        GUILayout.Space(10);

                        EditorGUILayout.LabelField("Left Boundry Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                        laneLeftBoundryType = GUILayout.SelectionGrid(laneLeftBoundryType, boundryTypeContent, 7);
                        GUILayout.Space(10);

                        EditorGUILayout.LabelField("Right Boundry Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                        laneRightBoundryType = GUILayout.SelectionGrid(laneRightBoundryType, boundryTypeContent, 7);
                        if (!EditorGUIUtility.isProSkin)
                            GUI.backgroundColor = Color.white;
                        GUILayout.Space(10);
                        laneSpeedLimit = EditorGUILayout.FloatField(new GUIContent("Speed Limit", "Lane speed limit"), laneSpeedLimit);
                        break;
                    case 1: // stop line
                        GUILayout.Space(10);
                        if (!EditorGUIUtility.isProSkin)
                            GUI.backgroundColor = nonProColor;
                        stopLineFacing = GUILayout.SelectionGrid(stopLineFacing, stopLineFacingContent, 2, buttonStyle);
                        GUILayout.Space(5);

                        isStopSign = GUILayout.Toggle(isStopSign, "Is this a stop sign? " + isStopSign, buttonStyle, GUILayout.Height(25));
                        if (!EditorGUIUtility.isProSkin)
                            GUI.backgroundColor = Color.white;
                        break;
                    case 2: // boundry line
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Boundry Line Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                        if (!EditorGUIUtility.isProSkin)
                            GUI.backgroundColor = nonProColor;
                        boundryLineType = GUILayout.SelectionGrid(boundryLineType, boundryLineTypeContent, 5);
                        if (!EditorGUIUtility.isProSkin)
                            GUI.backgroundColor = Color.white;
                        break;
                    default:
                        break;
                }
                GUILayout.Space(10);

                EditorGUILayout.LabelField("Waypoint Connect", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                waypointTotal = EditorGUILayout.IntField(new GUIContent("Waypoint count", "Number of waypoints when connected *MINIMUM 2 for straight 3 for curved*"), waypointTotal);
                if (waypointTotal < 3) waypointTotal = 3;
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer - SHIFT+W")))
                    CreateTempWaypoint();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a straight line - SHIFT+R")))
                    CreateStraight();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[2], "Connect waypoints to make a curved line - ALT+R")))
                    CreateCurved();
                if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints - SHIFT+Z")))
                    ClearAllTempWaypoints();
                GUILayout.EndHorizontal();

                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                break;
            case MapAnnotationTool.CreateMode.SIGNAL:
                EditorGUILayout.LabelField("Create Signal", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;
                EditorGUILayout.LabelField("Signal Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                signalType = GUILayout.SelectionGrid(signalType, signalTypeContent, 6);
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Signal Mesh Rotation", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                currentSignalForward = EditorGUILayout.IntPopup(new GUIContent("Forward Vector: "), currentSignalForward, signalOrientationForwardContent, new int[] { 0, 1, 2, 3, 4, 5 }, buttonStyle, GUILayout.MinWidth(0));
                currentSignalUp = EditorGUILayout.IntPopup(new GUIContent("Up Vector: "), currentSignalUp, signalOrientationUpContent, new int[] { 0, 1, 2, 3, 4, 5 }, buttonStyle, GUILayout.MinWidth(0));
                GUILayout.Space(5);
                if (GUILayout.Button(new GUIContent("Create Signal", "Create signal - SHIFT+R"), GUILayout.Height(25)))
                    CreateSignal();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                break;
            case MapAnnotationTool.CreateMode.SIGN:
                EditorGUILayout.LabelField("Create Sign", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;
                EditorGUILayout.LabelField("Sign Mesh Rotation", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                currentSignForward = EditorGUILayout.IntPopup(new GUIContent("Forward Vector: "), currentSignForward, signalOrientationForwardContent, new int[] { 0, 1, 2, 3, 4, 5 }, buttonStyle, GUILayout.MinWidth(0));
                currentSignUp = EditorGUILayout.IntPopup(new GUIContent("Up Vector: "), currentSignUp, signalOrientationUpContent, new int[] { 0, 1, 2, 3, 4, 5 }, buttonStyle, GUILayout.MinWidth(0));
                GUILayout.Space(5);
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;
                signType = (SignType)EditorGUILayout.EnumPopup("Sign type", signType, buttonStyle);
                GUILayout.Space(5);

                if (GUILayout.Button(new GUIContent("Create Sign", "Create sign - SHIFT+R"), GUILayout.Height(25)))
                    CreateSign();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                break;
            case MapAnnotationTool.CreateMode.POLE:
                EditorGUILayout.LabelField("Create Pole", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;
                if (GUILayout.Button(new GUIContent("Create Pole", "Create pole - SHIFT+R"), GUILayout.Height(25)))
                    CreatePole();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                break;
            case MapAnnotationTool.CreateMode.PEDESTRIAN:
                EditorGUILayout.LabelField("Create Pedestrian", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                EditorGUILayout.LabelField("Pedestrian Path Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;
                pedType = GUILayout.Toolbar(pedType, pedTypeContent);
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                GUILayout.Space(5);

                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer - SHIFT+W")))
                    CreateTempWaypoint();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints - SHIFT+R")))
                    CreatePedestrian();
                if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints - SHIFT+Z")))
                    ClearAllTempWaypoints();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                break;

            case MapAnnotationTool.CreateMode.JUNCTION:
                EditorGUILayout.LabelField("Create Junction", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                EditorGUILayout.LabelField("Waypoint Connect", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                waypointTotal = EditorGUILayout.IntField(new GUIContent("Waypoint count", "Number of waypoints when connected *MINIMUM 2 for straight 3 for curved*"), waypointTotal);
                if (waypointTotal < 3) waypointTotal = 3;
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer - SHIFT+W")))
                    CreateTempWaypoint();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a junction - SHIFT+R")))
                    CreateJunction();
                if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints - SHIFT+Z")))
                    ClearAllTempWaypoints();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                break;
            case MapAnnotationTool.CreateMode.CROSSWALK:
                EditorGUILayout.LabelField("Create Crosswalk", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                if (waypointTotal < 4) waypointTotal = 4;
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer - SHIFT+W")))
                    CreateTempWaypoint();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a crosswalk - SHIFT+R")))
                    CreateCrossWalk();
                if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints - SHIFT+Z")))
                    ClearAllTempWaypoints();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                break;
            case MapAnnotationTool.CreateMode.CLEARAREA:
                EditorGUILayout.LabelField("Create Cleararea", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                EditorGUILayout.LabelField("Waypoint Connect", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                waypointTotal = EditorGUILayout.IntField(new GUIContent("Waypoint count", "Number of waypoints when connected *4 for cleararea*"), waypointTotal);
                if (waypointTotal < 4) waypointTotal = 4;
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer - SHIFT+W")))
                    CreateTempWaypoint();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a clear area - SHIFT+R")))
                    CreateClearArea();
                if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints - SHIFT+Z")))
                    ClearAllTempWaypoints();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                break;
            case MapAnnotationTool.CreateMode.PARKINGSPACE:
                EditorGUILayout.LabelField("Create Parkingspace", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                if (waypointTotal < 4) waypointTotal = 4;
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer - SHIFT+W")))
                    CreateTempWaypoint();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a parking space - SHIFT+R")))
                    CreateParkingSpace();
                if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints - SHIFT+Z")))
                    ClearAllTempWaypoints();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                EditorGUILayout.TextArea(@"
                Sequence of vertices of rectangle for parking space:
                [2]-----[3]
                 |             |
                 |             |
                 |             |
                [1]-----[0]
                ----------
                lane
                ----------
                ", GUILayout.ExpandWidth(true));
                break;
            case MapAnnotationTool.CreateMode.SPEEDBUMP:
                EditorGUILayout.LabelField("Create Speedbump", titleLabelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(20);

                if (waypointTotal < 2) waypointTotal = 2;
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = nonProColor;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on snap layer - SHIFT+W")))
                    CreateTempWaypoint();
                if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a straight line - SHIFT+R")))
                    CreateSpeedBump();
                if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints - SHIFT+Z")))
                    ClearAllTempWaypoints();
                if (!EditorGUIUtility.isProSkin)
                    GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                break;
            default:
                break;
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);

        if (!EditorGUIUtility.isProSkin)
            GUI.backgroundColor = nonProColor;
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                SerializedObject serializedObject = new SerializedObject(data);
                if (serializedObject != null)
                {
                    EditorGUILayout.LabelField($"{data.transform.name} Inspector", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField("Selected scene object", data.gameObject, typeof(GameObject), true);
                    GUI.enabled = true;
                    serializedObject.Update();
                    ShowBool(serializedObject.FindProperty("displayHandles"));
                    GUILayout.Space(10);
                    ShowList(serializedObject.FindProperty("mapLocalPositions"));
                    serializedObject.ApplyModifiedProperties();
                    Repaint();

                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Map Local Position Array Helpers", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent("Append", "Add point to end of local positions")))
                        AppendLocalPositions();
                    if (GUILayout.Button(new GUIContent("Prepend", "Add point to beginning of local positions")))
                        PrependLocalPositions();
                    if (GUILayout.Button(new GUIContent("Remove First", "Remove first local position")))
                        RemoveFirstLocalPosition();
                    if (GUILayout.Button(new GUIContent("Remove Last", "Remove last local position")))
                        RemoveLastLocalPosition();
                    if (GUILayout.Button(new GUIContent("Reverse", "Reverse local position order")))
                        ReverseLocalPositions();
                    if (GUILayout.Button(new GUIContent("Clear", "Clear local positions")))
                        ClearLocalPositions();
                    if (GUILayout.Button(new GUIContent("Double", "Double local positions")))
                        DoubleLocalPositions();
                    if (GUILayout.Button(new GUIContent("Half", "Half local positions")))
                        HalfLocalPositions();
                    if (!EditorGUIUtility.isProSkin)
                        GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.Space(20);
            if (!EditorGUIUtility.isProSkin)
                GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndScrollView();
    }

    private void SnapAllToLayer()
    {
        RaycastHit hit;
        List<MapDataPoints> temp = new List<MapDataPoints>();
        temp.AddRange(FindObjectsOfType<MapDataPoints>());
        Undo.RecordObjects(temp.ToArray(), "local positions");

        foreach (var item in temp)
        {
            for (int i = 0; i < item.mapLocalPositions.Count; i++)
            {
                if (Physics.Raycast(item.transform.TransformPoint(item.mapLocalPositions[i]) + new Vector3(0, 100, 0), new Vector3(0, -1, 0), out hit, 200f, layerMask))
                {
                    item.mapLocalPositions[i] = item.transform.InverseTransformPoint(hit.point);
                }
            }
        }
        Debug.Log("All local positions snapped to ground layer");
        SceneView.RepaintAll();
    }

    private void ShowBool(SerializedProperty enabled)
    {
        enabled.boolValue = GUILayout.Toggle(enabled.boolValue, "Toggle Handles", new GUIStyle(GUI.skin.button), GUILayout.MaxHeight(25), GUILayout.ExpandHeight(false));
    }

    private void ShowList(SerializedProperty list)
    {
        EditorGUILayout.PropertyField(list);
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

    private void AppendLocalPositions()
    {
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                Undo.RecordObject(data, "local positions change");
                data.mapLocalPositions.Add(data.mapLocalPositions.Count > 0 ? data.mapLocalPositions[data.mapLocalPositions.Count - 1] : data.transform.InverseTransformPoint(data.transform.position));
            }
        }
    }

    private void PrependLocalPositions()
    {
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                Undo.RecordObject(data, "local positions change");
                if (data.mapLocalPositions.Count > 0)
                    data.mapLocalPositions.Insert(0, data.mapLocalPositions[0]);
                else
                    data.mapLocalPositions.Add(data.transform.InverseTransformPoint(data.transform.position));
            }
        }
    }

    private void RemoveFirstLocalPosition()
    {
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                Undo.RecordObject(data, "local positions change");
                if (data.mapLocalPositions.Count > 0)
                    data.mapLocalPositions.RemoveAt(0);
            }
        }
    }

    private void RemoveLastLocalPosition()
    {
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                Undo.RecordObject(data, "local positions change");
                if (data.mapLocalPositions.Count > 0)
                    data.mapLocalPositions.RemoveAt(data.mapLocalPositions.Count - 1);
            }
        }
    }

    private void ReverseLocalPositions()
    {
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                Undo.RecordObject(data, "local positions change");
                if (data.mapLocalPositions.Count > 0)
                    data.mapLocalPositions.Reverse();
            }
        }
    }

    private void ClearLocalPositions()
    {
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                Undo.RecordObject(data, "local positions change");
                for (int i = 0; i < data.mapLocalPositions.Count; i++)
                {
                    data.mapLocalPositions[i] = data.transform.InverseTransformPoint(data.transform.position);
                }
            }
        }
    }

    private void DoubleLocalPositions()
    {
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                Undo.RecordObject(data, "local positions change");
                for (int i = data.mapLocalPositions.Count - 1; i > 0; --i)
                {
                    var mid = (data.mapLocalPositions[i] + data.mapLocalPositions[i - 1]) / 2f;
                    data.mapLocalPositions.Insert(i, mid);
                }
            }
        }
    }

    private void HalfLocalPositions()
    {
        if (Selection.activeGameObject != null)
        {
            var data = Selection.activeGameObject.GetComponent<MapDataPoints>();
            if (data != null)
            {
                Undo.RecordObject(data, "local positions change");
                for (int i = data.mapLocalPositions.Count - 2; i > 0; i -= 2)
                {
                    data.mapLocalPositions.RemoveAt(i);
                }
            }
        }
    }

    private static void IncrementCreateMode()
    {
        List<MapAnnotationTool.CreateMode> modes = Enum.GetValues(typeof(MapAnnotationTool.CreateMode)).Cast<MapAnnotationTool.CreateMode>().ToList();
        MapAnnotationTool.CreateMode currentMode = MapAnnotationTool.createMode;

        int i = modes.IndexOf(currentMode);
        MapAnnotationTool.createMode = modes[(++i) % modes.Count];

        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        tool.ChangeCreateMode();
    }

    private void ChangeCreateMode()
    {
        ClearTargetWaypoint();
        ClearAllTempWaypoints();
        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
            case MapAnnotationTool.CreateMode.SIGN:
            case MapAnnotationTool.CreateMode.SIGNAL:
                break;
            case MapAnnotationTool.CreateMode.LANE_LINE:
            case MapAnnotationTool.CreateMode.POLE:
            case MapAnnotationTool.CreateMode.PEDESTRIAN:
            case MapAnnotationTool.CreateMode.JUNCTION:
            case MapAnnotationTool.CreateMode.CROSSWALK:
            case MapAnnotationTool.CreateMode.CLEARAREA:
            case MapAnnotationTool.CreateMode.PARKINGSPACE:
            case MapAnnotationTool.CreateMode.SPEEDBUMP:
                CreateTargetWaypoint();
                break;
        }
    }

    private void ClearModes()
    {
        MapAnnotationTool.TOOL_ACTIVE = false;
        MapAnnotationTool.SHOW_HELP = false;
        MapAnnotationTool.SHOW_MAP_ALL = false;
        MapAnnotationTool.SHOW_MAP_SELECTED = false;
        targetWaypointGO = null;
        List<MapTargetWaypoint> missedTargetWP = new List<MapTargetWaypoint>();
        missedTargetWP.AddRange(FindObjectsOfType<MapTargetWaypoint>());
        for (int i = 0; i < missedTargetWP.Count; i++)
            DestroyImmediate(missedTargetWP[i].gameObject);
        
        List<MapWaypoint> missedWP = new List<MapWaypoint>();
        missedWP.AddRange(FindObjectsOfType<MapWaypoint>());
        for (int i = 0; i < missedWP.Count; i++)
            DestroyImmediate(missedWP[i].gameObject);
    }

    private void ClearTargetWaypoint()
    {
        targetWaypointGO = null;
        List<MapTargetWaypoint> missedTargetWP = new List<MapTargetWaypoint>();
        missedTargetWP.AddRange(FindObjectsOfType<MapTargetWaypoint>());
        for (int i = 0; i < missedTargetWP.Count; i++)
            Undo.DestroyObjectImmediate(missedTargetWP[i].gameObject);
    }

    private void CreateMapHolder()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));

        var tempGO = new GameObject("Map" + SceneManager.GetActiveScene().name);
        tempGO.transform.position = Vector3.zero;
        tempGO.transform.rotation = Quaternion.identity;
        mapHolder = tempGO.AddComponent<MapHolder>();
        var trafficLanes = new GameObject("TrafficLanes").transform;
        trafficLanes.SetParent(tempGO.transform);
        mapHolder.trafficLanesHolder = trafficLanes;
        var intersections = new GameObject("Intersections").transform;
        intersections.SetParent(tempGO.transform);
        mapHolder.intersectionsHolder = intersections;
        Undo.RegisterCreatedObjectUndo(tempGO, nameof(tempGO));

        SceneView.RepaintAll();
        Debug.Log("Holder object for this scenes annotations, intersections and lanes created");
    }

    private void CreateIntersectionHolder()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));

        var cam = SceneView.lastActiveSceneView.camera;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, tool.layerMask.value))
        {
            var tempGO = new GameObject("MapIntersection");
            tempGO.transform.position = hit.point;
            tempGO.AddComponent<MapIntersection>();
            if (mapHolder != null && mapHolder.intersectionsHolder != null)
                tempGO.transform.SetParent(mapHolder.intersectionsHolder, true);
            Undo.RegisterCreatedObjectUndo(tempGO, nameof(tempGO));
        }
        SceneView.RepaintAll();
        Debug.Log("Holder object for this intersection's annotations created,  TriggerBounds must fit inside intersection stop lines");
    }

    private void CreateLaneSectionHolder()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));

        var cam = SceneView.lastActiveSceneView.camera;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, tool.layerMask.value))
        {
            var tempGO = new GameObject("MapLaneSection");
            tempGO.transform.position = hit.point;
            tempGO.AddComponent<MapLaneSection>();
            if (mapHolder != null && mapHolder.trafficLanesHolder != null)
                tempGO.transform.SetParent(mapHolder.trafficLanesHolder, true);
            Undo.RegisterCreatedObjectUndo(tempGO, nameof(tempGO));
        }
        SceneView.RepaintAll();
        Debug.Log("Holder object for lane section annotations created");
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

    public static void CreateTempWaypoint()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));

        var cam = SceneView.lastActiveSceneView.camera;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f, tool.layerMask.value))
        {
            var tempWaypointGO = new GameObject("TEMP_WAYPOINT");
            tempWaypointGO.transform.position = hit.point;
            tempWaypointGO.AddComponent<MapWaypoint>().layerMask = tool.layerMask;
            tool.tempWaypoints.Add(tempWaypointGO.GetComponent<MapWaypoint>());
            Undo.RegisterCreatedObjectUndo(tempWaypointGO, nameof(tempWaypointGO));
        }
        SceneView.RepaintAll();
    }

    private static void ClearAllTempWaypoints()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));

        tool.tempWaypoints.Clear();
        List<MapWaypoint> missedWP = new List<MapWaypoint>();
        missedWP.AddRange(FindObjectsOfType<MapWaypoint>());
        for (int i = 0; i < missedWP.Count; i++)
            Undo.DestroyObjectImmediate(missedWP[i].gameObject);
        SceneView.RepaintAll();
    }

    private static void CreateStraight()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.LANE_LINE)
        {
            Debug.LogWarning("Create Mode 'Lane/Line' must be selected in Map Annotation Tool");
            return;
        }

        tool.tempWaypoints.RemoveAll(p => p == null);
        if (tool.tempWaypoints.Count < 2)
        {
            Debug.Log("You need at least two temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject();
        switch (tool.createType)
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

        Vector3 avePos = Vector3.Lerp(tool.tempWaypoints[0].transform.position, tool.tempWaypoints[tool.tempWaypoints.Count - 1].transform.position, 0.5f);
        newGo.transform.position = avePos;
        var dir = (tool.tempWaypoints[tool.tempWaypoints.Count - 1].transform.position - tool.tempWaypoints[0].transform.position).normalized;
        if (tool.createType == 1) // stopline
        {
            newGo.transform.rotation = Quaternion.LookRotation(dir);
            if (tool.stopLineFacing == 0)
                newGo.transform.rotation = Quaternion.LookRotation(newGo.transform.TransformDirection(Vector3.right).normalized, newGo.transform.TransformDirection(Vector3.up).normalized);
            else
                newGo.transform.rotation = Quaternion.LookRotation(newGo.transform.TransformDirection(-Vector3.right).normalized, newGo.transform.TransformDirection(Vector3.up).normalized);
        }
        else
            newGo.transform.rotation = Quaternion.LookRotation(dir);

        List<Vector3> tempLocalPos = new List<Vector3>();
        if (tool.tempWaypoints.Count == 2)
        {
            Debug.Log("Connect with Waypoint Count");
            float t = 0f;
            Vector3 position = Vector3.zero;
            Vector3 p0 = tool.tempWaypoints[0].transform.position;
            Vector3 p1 = tool.tempWaypoints[1].transform.position;
            for (int i = 0; i < tool.waypointTotal; i++)
            {
                t = i / (tool.waypointTotal - 1.0f);
                position = (1.0f - t) * p0 + t * p1;
                tempLocalPos.Add(position);
            }
        }
        else
        {
            Debug.Log("Connect Waypoint Count ignored");
            for (int i = 0; i < tool.tempWaypoints.Count; i++)
            {
                tempLocalPos.Add(tool.tempWaypoints[i].transform.position);
            }
        }

        switch (tool.createType)
        {
            case 0: // lane
                var lane = newGo.GetComponent<MapLane>();
                foreach (var pos in tempLocalPos)
                    lane.mapLocalPositions.Add(newGo.transform.InverseTransformPoint(pos));
                lane.laneTurnType = (MapData.LaneTurnType)tool.laneTurnType + 1;
                lane.speedLimit = tool.laneSpeedLimit;
                break;
            case 1: // stopline
                var line = newGo.GetComponent<MapLine>();
                foreach (var pos in tempLocalPos)
                    line.mapLocalPositions.Add(newGo.transform.InverseTransformPoint(pos));
                newGo.GetComponent<MapLine>().lineType = MapData.LineType.STOP;
                newGo.GetComponent<MapLine>().isStopSign = tool.isStopSign;
                break;
            case 2: // boundry line
                var bLine = newGo.GetComponent<MapLine>();
                foreach (var pos in tempLocalPos)
                    bLine.mapLocalPositions.Add(newGo.transform.InverseTransformPoint(pos));
                newGo.GetComponent<MapLine>().lineType = (MapData.LineType)(tool.boundryLineType - 1);
                break;
        }

        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);

        tool.tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tool.tempWaypoints.Clear();

        Selection.activeObject = newGo;
    }

    public static void CreateCurved()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.LANE_LINE)
        {
            Debug.LogWarning("Create Mode 'Lane/Line' must be selected in Map Annotation Tool");
            return;
        }

        tool.tempWaypoints.RemoveAll(p => p == null);
        if (tool.tempWaypoints.Count != 3)
        {
            Debug.Log("You need three temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject();
        switch (tool.createType)
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

        Vector3 avePos = Vector3.Lerp(tool.tempWaypoints[0].transform.position, tool.tempWaypoints[2].transform.position, 0.5f);
        newGo.transform.position = avePos;

        float t = 0f;
        Vector3 position = Vector3.zero;
        Vector3 p0 = tool.tempWaypoints[0].transform.position;
        Vector3 p1 = tool.tempWaypoints[1].transform.position;
        Vector3 p2 = tool.tempWaypoints[2].transform.position;
        List<Vector3> tempLocalPos = new List<Vector3>();
        for (int i = 0; i < tool.waypointTotal; i++)
        {
            t = i / (tool.waypointTotal - 1.0f);
            position = (1.0f - t) * (1.0f - t) * p0 + 2.0f * (1.0f - t) * t * p1 + t * t * p2;
            tempLocalPos.Add(position);
        }

        var dir = (tempLocalPos[1] - tempLocalPos[0]).normalized;
        newGo.transform.rotation = Quaternion.LookRotation(dir);

        switch (tool.createType)
        {
            case 0: // lane
                var lane = newGo.GetComponent<MapLane>();
                foreach (var pos in tempLocalPos)
                    lane.mapLocalPositions.Add(newGo.transform.InverseTransformPoint(pos));
                newGo.GetComponent<MapLane>().laneTurnType = (MapData.LaneTurnType)tool.laneTurnType + 1;
                newGo.GetComponent<MapLane>().speedLimit = tool.laneSpeedLimit;
                break;
            case 1: // stopline
                var line = newGo.GetComponent<MapLine>();
                foreach (var pos in tempLocalPos)
                    line.mapLocalPositions.Add(newGo.transform.InverseTransformPoint(pos));
                newGo.GetComponent<MapLine>().lineType = MapData.LineType.STOP;
                newGo.GetComponent<MapLine>().isStopSign = tool.isStopSign;
                break;
            case 2: // boundry line
                var bLine = newGo.GetComponent<MapLine>();
                foreach (var pos in tempLocalPos)
                    bLine.mapLocalPositions.Add(newGo.transform.InverseTransformPoint(pos));
                newGo.GetComponent<MapLine>().lineType = (MapData.LineType)tool.boundryLineType - 1;
                break;
        }
        
        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);

        tool.tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tool.tempWaypoints.Clear();

        Selection.activeObject = newGo;
    }

    private static void CreateSignal()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.SIGNAL)
        {
            Debug.LogWarning("Create Mode 'Signal' must be selected in Map Annotation Tool");
            return;
        }

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
        tool.signalType += 1;
        signal.signalType = (MapData.SignalType)tool.signalType;
        switch (signal.signalType)
        {
            case MapData.SignalType.UNKNOWN:
                signal.signalData = new List<MapData.SignalData>();
                break;
            case MapData.SignalType.MIX_2_HORIZONTAL:
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.right * 0.25f, signalColor = MapData.SignalColorType.Red },
                    new MapData.SignalData() { localPosition = Vector3.right * -0.25f, signalColor = MapData.SignalColorType.Green },
                };
                break;
            case MapData.SignalType.MIX_2_VERTICAL:
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.up * 0.25f, signalColor = MapData.SignalColorType.Red },
                    new MapData.SignalData() { localPosition = Vector3.up * -0.25f, signalColor = MapData.SignalColorType.Green },
                };
                break;
            case MapData.SignalType.MIX_3_HORIZONTAL:
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.right * 0.4f, signalColor = MapData.SignalColorType.Red },
                    new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Yellow },
                    new MapData.SignalData() { localPosition = Vector3.right * -0.4f, signalColor = MapData.SignalColorType.Green },
                };
                break;
            case MapData.SignalType.MIX_3_VERTICAL:
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.up * 0.4f, signalColor = MapData.SignalColorType.Red },
                    new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Yellow },
                    new MapData.SignalData() { localPosition = Vector3.up * -0.4f, signalColor = MapData.SignalColorType.Green },
                };
                break;
            case MapData.SignalType.SINGLE:
                signal.signalData = new List<MapData.SignalData> {
                    new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Red },
                };
                break;
        }

        switch (tool.currentSignalForward)
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
        switch (tool.currentSignalUp)
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
        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);
        Selection.activeObject = newGo;
    }

    private static void CreateSign()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.SIGN)
        {
            Debug.LogWarning("Create Mode 'Sign' must be selected in Map Annotation Tool");
            return;
        }

        var signMesh = Selection.activeTransform;
        if (signMesh == null)
        {
            Debug.Log("Must have a sign mesh selected");
            return;
        }

        var newGo = new GameObject("MapSign");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var sign = newGo.AddComponent<MapSign>();
        sign.signMesh = signMesh.GetComponent<Renderer>();

        sign.signType = tool.signType == SignType.STOP ? MapData.SignType.STOP : MapData.SignType.YIELD;

        if (sign.signMesh == null)
        {
            Debug.Log("Sign mesh must have Renderer Component");
            DestroyImmediate(newGo);
            return;
        }

        Vector3 targetFwdVec = Vector3.forward;
        Vector3 targetUpVec = Vector3.up;

        switch (tool.currentSignForward)
        {
            case 0: // z
                targetFwdVec = Vector3.forward;
                sign.boundScale = new Vector3(sign.signMesh.bounds.size.x, sign.signMesh.bounds.size.y, 0f);
                break;
            case 1: // -z
                targetFwdVec = -Vector3.forward;
                sign.boundScale = new Vector3(sign.signMesh.bounds.size.x, sign.signMesh.bounds.size.y, 0f);
                break;
            case 2: // x
                targetFwdVec = Vector3.right;
                sign.boundScale = new Vector3(sign.signMesh.bounds.size.z, sign.signMesh.bounds.size.y, 0f);
                break;
            case 3: // -x
                targetFwdVec = -Vector3.right;
                sign.boundScale = new Vector3(sign.signMesh.bounds.size.z, sign.signMesh.bounds.size.y, 0f);
                break;
            case 4: // y
                targetFwdVec = Vector3.up;
                sign.boundScale = new Vector3(sign.signMesh.bounds.size.z, sign.signMesh.bounds.size.y, 0f);
                break;
            case 5: // -y
                targetFwdVec = -Vector3.up;
                sign.boundScale = new Vector3(sign.signMesh.bounds.size.z, sign.signMesh.bounds.size.y, 0f);
                break;
        }

        switch (tool.currentSignUp)
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

        targetFwdVec = signMesh.transform.TransformDirection(targetFwdVec).normalized;
        targetUpVec = signMesh.transform.TransformDirection(targetUpVec).normalized;

        var targetPos = signMesh.position;
        if (Physics.Raycast(signMesh.position, -targetUpVec, out RaycastHit hit, 1000f, LayerMask.GetMask("Default")))
        {
            targetPos = hit.point;
            sign.boundOffsets = new Vector3(0f, hit.distance, 0f);
        }

        newGo.transform.position = targetPos;
        newGo.transform.rotation = Quaternion.LookRotation(targetFwdVec, targetUpVec);

        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);
        Selection.activeObject = newGo;
    }

    private static void CreatePole()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.POLE)
        {
            Debug.LogWarning("Create Mode 'Pole' must be selected in Map Annotation Tool");
            return;
        }

        var newGo = new GameObject("MapPole");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var sign = newGo.AddComponent<MapPole>();

        newGo.transform.position = tool.targetWaypointGO.transform.position;
        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);
        Selection.activeObject = newGo;
    }

    private static void CreatePedestrian()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.PEDESTRIAN)
        {
            Debug.LogWarning("Create Mode 'Pedestrian' must be selected in Map Annotation Tool");
            return;
        }

        tool.tempWaypoints.RemoveAll(p => p == null);
        if (tool.tempWaypoints.Count < 2)
        {
            Debug.Log("You need at least two temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject("MapPedestrian");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var ped = newGo.AddComponent<MapPedestrian>();

        Vector3 avePos = Vector3.Lerp(tool.tempWaypoints[0].transform.position, tool.tempWaypoints[tool.tempWaypoints.Count - 1].transform.position, 0.5f);
        newGo.transform.position = avePos;
        var dir = (tool.tempWaypoints[1].transform.position - tool.tempWaypoints[0].transform.position).normalized;
        newGo.transform.rotation = Quaternion.LookRotation(dir);

        for (int i = 0; i < tool.tempWaypoints.Count; i++)
        {
            ped.mapLocalPositions.Add(newGo.transform.InverseTransformPoint(tool.tempWaypoints[i].transform.position));
        }
        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);

        ped.type = (MapAnnotationTool.PedestrianPathType)tool.pedType;

        tool.tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tool.tempWaypoints.Clear();

        Selection.activeObject = newGo;
    }

    private static void CreateJunction()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.JUNCTION)
        {
            Debug.LogWarning("Create Mode 'Junction' must be selected in Map Annotation Tool");
            return;
        }

        tool.tempWaypoints.RemoveAll(p => p == null);
        if (tool.tempWaypoints.Count < 3)
        {
            Debug.Log("You need three temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject("MapJunction");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var junction = newGo.AddComponent<MapJunction>();

        newGo.transform.position = Average(tool.tempWaypoints);

        foreach (var tempWayPoint in tool.tempWaypoints)
        {
            Vector3 position = Vector3.zero;
            Vector3 p = tempWayPoint.transform.position;
            newGo.GetComponent<MapJunction>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
        }

        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);

        tool.tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tool.tempWaypoints.Clear();

        Selection.activeObject = newGo;

        junction.displayHandles = true;
    }

    private static void CreateCrossWalk()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.CROSSWALK)
        {
            Debug.LogWarning("Create Mode 'Crosswalk' must be selected in Map Annotation Tool");
            return;
        }

        tool.tempWaypoints.RemoveAll(p => p == null);
        if (tool.tempWaypoints.Count != 4)
        {
            Debug.Log("You need four temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject("MapCrossWalk");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var crossWalk = newGo.AddComponent<MapCrossWalk>();

        newGo.transform.position = Average(tool.tempWaypoints);

        foreach (var tempWayPoint in tool.tempWaypoints)
        {
            Vector3 position = Vector3.zero;
            Vector3 p = tempWayPoint.transform.position;
            newGo.GetComponent<MapCrossWalk>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
        }

        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);

        tool.tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tool.tempWaypoints.Clear();

        Selection.activeObject = newGo;
    }

    private static void CreateClearArea()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.CLEARAREA)
        {
            Debug.LogWarning("Create Mode 'Clear Area' must be selected in Map Annotation Tool");
            return;
        }

        tool.tempWaypoints.RemoveAll(p => p == null);
        if (tool.tempWaypoints.Count != 4)
        {
            Debug.Log("You need four temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject("MapClearArea");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var clearArea = newGo.AddComponent<MapClearArea>();

        newGo.transform.position = Average(tool.tempWaypoints);

        foreach (var tempWayPoint in tool.tempWaypoints)
        {
            Vector3 position = Vector3.zero;
            Vector3 p = tempWayPoint.transform.position;
            newGo.GetComponent<MapClearArea>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
        }

        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);

        tool.tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tool.tempWaypoints.Clear();

        Selection.activeObject = newGo;

        clearArea.displayHandles = true;
    }

    private static void CreateParkingSpace()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.PARKINGSPACE)
        {
            Debug.LogWarning("Create Mode 'Parking Space' must be selected in Map Annotation Tool");
            return;
        }

        tool.tempWaypoints.RemoveAll(p => p == null);
        if (tool.tempWaypoints.Count != 4)
        {
            Debug.Log("You need four temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject("MapParkingSpace");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var parkingSpace = newGo.AddComponent<MapParkingSpace>();

        newGo.transform.position = Average(tool.tempWaypoints);

        foreach (var tempWayPoint in tool.tempWaypoints)
        {
            Vector3 position = Vector3.zero;
            Vector3 p = tempWayPoint.transform.position;
            newGo.GetComponent<MapParkingSpace>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
        }

        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);

        tool.tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tool.tempWaypoints.Clear();

        Selection.activeObject = newGo;
    }

    private static void CreateSpeedBump()
    {
        MapAnnotations tool = (MapAnnotations)GetWindow(typeof(MapAnnotations));
        if (MapAnnotationTool.createMode != MapAnnotationTool.CreateMode.SPEEDBUMP)
        {
            Debug.LogWarning("Create Mode 'Speed Bump' must be selected in Map Annotation Tool");
            return;
        }

        tool.tempWaypoints.RemoveAll(p => p == null);

        if (tool.tempWaypoints.Count != 2)
        {
            Debug.Log("You need two temp waypoints for this operation");
            return;
        }

        var newGo = new GameObject("MapSpeedBump");
        Undo.RegisterCreatedObjectUndo(newGo, nameof(newGo));
        var speedBump = newGo.AddComponent<MapSpeedBump>();

        newGo.transform.position = Average(tool.tempWaypoints);

        foreach (var tempWayPoint in tool.tempWaypoints)
        {
            Vector3 position = Vector3.zero;
            Vector3 p = tempWayPoint.transform.position;
            newGo.GetComponent<MapSpeedBump>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
        }

        newGo.transform.SetParent(tool.parentObj == null ? null : tool.parentObj.transform);

        tool.tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tool.tempWaypoints.Clear();

        Selection.activeObject = newGo;

        speedBump.displayHandles = true;
    }

    static Vector3 Average(IEnumerable<MapWaypoint> items)
    {
        var x = items.Average(item => item.transform.position.x);
        var y = items.Average(item => item.transform.position.y);
        var z = items.Average(item => item.transform.position.z);

        return new Vector3(x, y, z);
    }

    [Shortcut("HD Map Annotation/Connect", KeyCode.R, ShortcutModifiers.Shift)]
    private static void ConnectShortcut()
    {
        if (Resources.FindObjectsOfTypeAll<MapAnnotations>().Length != 1) return;

        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
                break;
            case MapAnnotationTool.CreateMode.SIGNAL:
                CreateSignal();
                break;
            case MapAnnotationTool.CreateMode.LANE_LINE:
                CreateStraight();
                break;
            case MapAnnotationTool.CreateMode.SIGN:
                CreateSign();
                break;
            case MapAnnotationTool.CreateMode.JUNCTION:
                CreateJunction();
                break;
            case MapAnnotationTool.CreateMode.CROSSWALK:
                CreateCrossWalk();
                break;
            case MapAnnotationTool.CreateMode.CLEARAREA:
                CreateClearArea();
                break;
            case MapAnnotationTool.CreateMode.PARKINGSPACE:
                CreateParkingSpace();
                break;
            case MapAnnotationTool.CreateMode.SPEEDBUMP:
                CreateSpeedBump();
                break;
            case MapAnnotationTool.CreateMode.POLE:
                CreatePole();
                break;
            case MapAnnotationTool.CreateMode.PEDESTRIAN:
                CreatePedestrian();
                break;
        }
    }

    [Shortcut("HD Map Annotation/Delete Temp Waypoints", KeyCode.Z, ShortcutModifiers.Shift)]
    private static void DeleteShortcut()
    {
        if (Resources.FindObjectsOfTypeAll<MapAnnotations>().Length != 1) return;

        ClearAllTempWaypoints();
    }

    [Shortcut("HD Map Annotation/Change Create Mode", KeyCode.G, ShortcutModifiers.Shift)]
    private static void CycleCreateModeShortcut()
    {
        if (Resources.FindObjectsOfTypeAll<MapAnnotations>().Length != 1) return;

        IncrementCreateMode();
    }

    [Shortcut("HD Map Annotation/Create Curved Lane", KeyCode.R, ShortcutModifiers.Alt)]
    private static void CurvedLaneShortcut()
    {
        if (Resources.FindObjectsOfTypeAll<MapAnnotations>().Length != 1) return;

        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.LANE_LINE:
                CreateCurved();
                break;
            default:
                break;
        }
    }

    [Shortcut("HD Map Annotation/Create Temp Waypoint", KeyCode.W, ShortcutModifiers.Shift)]
    private static void TempWaypointShortcut()
    {
        if (Resources.FindObjectsOfTypeAll<MapAnnotations>().Length != 1) return;

        switch (MapAnnotationTool.createMode)
        {
            case MapAnnotationTool.CreateMode.NONE:
            case MapAnnotationTool.CreateMode.SIGNAL:
            case MapAnnotationTool.CreateMode.POLE:
            case MapAnnotationTool.CreateMode.SIGN:
                Debug.LogWarning("Current Create Mode does not support Temp Waypoints");
                return;
            case MapAnnotationTool.CreateMode.LANE_LINE:
            case MapAnnotationTool.CreateMode.JUNCTION:
            case MapAnnotationTool.CreateMode.CROSSWALK:
            case MapAnnotationTool.CreateMode.CLEARAREA:
            case MapAnnotationTool.CreateMode.PARKINGSPACE:
            case MapAnnotationTool.CreateMode.SPEEDBUMP:
            case MapAnnotationTool.CreateMode.PEDESTRIAN:
                CreateTempWaypoint();
                break;
        }
    }

    private int GetLaneNumber(MapLane lane)
    {
        var laneName = lane.name;
        var laneNumber = Regex.Match(laneName, @"\d+").Value;
        if (laneNumber != "") {return int.Parse(laneNumber);}
        return lane.GetInstanceID();
    }

    private string RenameLine(MapLane lane, MapLane otherLane)
    {
        var newName = new List<string>(){"MapLine"};
        var otherLaneNumber = GetLaneNumber(otherLane);
        var laneNumber = GetLaneNumber(lane);
        newName.Add("Shared");
        var laneNames = laneNumber < otherLaneNumber ? laneNumber + "_" + otherLaneNumber : otherLaneNumber + "_" + laneNumber;
        newName.Add(laneNames);

        return String.Join("_", newName);
    }

    public static void AddWorldPositions(MapDataPoints mapDataPoints)
    {
        mapDataPoints.mapWorldPositions.Clear();
        foreach (var localPos in mapDataPoints.mapLocalPositions)
            mapDataPoints.mapWorldPositions.Add(mapDataPoints.transform.TransformPoint(localPos));
    }

    private void AddWorldPositions(List<MapLane> lanes)
    {
        foreach (var lane in lanes)
        {
            AddWorldPositions(lane);
            var leftLine = lane.leftLineBoundry;
            var rightLine = lane.rightLineBoundry;
            AddWorldPositions(leftLine);
            AddWorldPositions(rightLine);
        }
    }

    private void FindAndRemoveExtraLines(List<MapLane> lanes)
    {
        var visitedLanesLeft = new HashSet<MapLane>();
        var visitedLanesRight = new HashSet<MapLane>();
        var threshold = 1.0;

        foreach (var lane in lanes)
        {
            if (!visitedLanesLeft.Contains(lane))
            {
                CheckLeftLine(lanes, visitedLanesLeft, visitedLanesRight, threshold, lane);
            }

            if (!visitedLanesRight.Contains(lane))
            {
                CheckRightLine(lanes, visitedLanesLeft, visitedLanesRight, threshold, lane);
            }
        }
    }

    private Vector3 getClosestPoint(Vector3 point, MapLine line)
    {
        var closestPoint = line.mapWorldPositions.First();
        if ((line.mapWorldPositions.First() - point).magnitude >
            (line.mapWorldPositions.Last() - point).magnitude)
        {
            closestPoint =line.mapWorldPositions.Last();
        }

        return closestPoint;
    }

    private bool isSameDirection(MapLane lane, MapLine line)
    {
        var laneVec = lane.mapWorldPositions.Last() - lane.mapWorldPositions.First();
        var lineVec = line.mapWorldPositions.Last() - line.mapWorldPositions.First();

        return Vector3.Dot(laneVec, lineVec) > 0;
    }

    private void CheckLeftLine(List<MapLane> lanes, HashSet<MapLane> visitedLanesLeft, HashSet<MapLane> visitedLanesRight, double threshold, MapLane lane)
    {
        var leftLine = lane.leftLineBoundry;
        var leftStartPoint = leftLine.mapWorldPositions.First();
        var leftEndPoint = leftLine.mapWorldPositions.Last();
        foreach (var otherLane in lanes)
        {
            if (lane == otherLane) continue;

            if (!visitedLanesLeft.Contains(otherLane))
            {
                var otherLeftLine = otherLane.leftLineBoundry;
                var otherLeftStartPoint = otherLeftLine.mapWorldPositions.First();
                var otherLeftEndPoint = otherLeftLine.mapWorldPositions.Last();
                if ((leftStartPoint - otherLeftEndPoint).magnitude < threshold &&
                    (leftEndPoint - otherLeftStartPoint).magnitude < threshold)
                {
                    if (leftLine == otherLeftLine)
                    {
                        // Extra line already removed.
                        break;
                    }

                    if (otherLane.leftLineBoundry != null) GameObject.DestroyImmediate(otherLeftLine.gameObject);
                    ExtraLinesCnt += 1;
                    otherLane.leftLineBoundry = leftLine;
                    leftLine.name = RenameLine(lane, otherLane);

                    visitedLanesLeft.Add(lane);
                    visitedLanesLeft.Add(otherLane);
                    break;
                }
            }

            if (!visitedLanesRight.Contains(otherLane))
            {
                var otherRightLine = otherLane.rightLineBoundry;
                var otherRightStartPoint = otherRightLine.mapWorldPositions.First();
                var otherRightEndPoint = otherRightLine.mapWorldPositions.Last();
                if ((leftStartPoint - otherRightStartPoint).magnitude < threshold &&
                    (leftEndPoint - otherRightEndPoint).magnitude < threshold)
                {
                    if (leftLine == otherRightLine)
                    {
                        // Extra line already removed.
                        break;
                    }

                    if (otherLane.rightLineBoundry != null) GameObject.DestroyImmediate(otherRightLine.gameObject);
                    ExtraLinesCnt += 1;
                    otherLane.rightLineBoundry = leftLine;
                    leftLine.name = RenameLine(lane, otherLane);

                    visitedLanesLeft.Add(lane);
                    visitedLanesRight.Add(otherLane);
                    break;
                }
            }
        }
    }

    private void CheckRightLine(List<MapLane> lanes, HashSet<MapLane> visitedLanesLeft, HashSet<MapLane> visitedLanesRight, double threshold, MapLane lane)
    {
        var rightLine = lane.rightLineBoundry;
        var rightStartPoint = rightLine.mapWorldPositions.First();
        var rightEndPoint = rightLine.mapWorldPositions.Last();
        foreach (var otherLane in lanes)
        {
            if (lane == otherLane) continue;

            if (!visitedLanesLeft.Contains(otherLane))
            {
                var otherLeftLine = otherLane.leftLineBoundry;
                var otherLeftStartPoint = otherLeftLine.mapWorldPositions.First();
                var otherLeftEndPoint = otherLeftLine.mapWorldPositions.Last();
                if ((rightStartPoint - otherLeftStartPoint).magnitude < threshold &&
                    (rightEndPoint - otherLeftEndPoint).magnitude < threshold)
                {
                    if (rightLine == otherLeftLine)
                    {
                        // Extra line already removed.
                        break;
                    }

                    if (otherLane.leftLineBoundry != null) GameObject.DestroyImmediate(otherLeftLine.gameObject);
                    ExtraLinesCnt += 1;
                    otherLane.leftLineBoundry = rightLine;
                    rightLine.name = RenameLine(lane, otherLane);

                    visitedLanesRight.Add(lane);
                    visitedLanesLeft.Add(otherLane);
                    break;
                }
            }

            if (!visitedLanesRight.Contains(otherLane))
            {
                var otherRightLine = otherLane.rightLineBoundry;
                var otherRightStartPoint = otherRightLine.mapWorldPositions.First();
                var otherRightEndPoint = otherRightLine.mapWorldPositions.Last();
                if ((rightStartPoint - otherRightEndPoint).magnitude < threshold &&
                    (rightEndPoint - otherRightStartPoint).magnitude < threshold)
                {
                    if (rightLine == otherRightLine)
                    {
                        // Extra line already removed.
                        break;
                    }

                    if (otherLane.rightLineBoundry != null) GameObject.DestroyImmediate(otherRightLine.gameObject);
                    ExtraLinesCnt += 1;
                    otherLane.rightLineBoundry = rightLine;
                    rightLine.name = RenameLine(lane, otherLane);

                    visitedLanesRight.Add(lane);
                    visitedLanesRight.Add(otherLane);
                    break;
                }
            }
        }
    }

    private void AlignLineEndPoints(List<MapLane> mapLanes)
    {
        foreach (MapLane lane in mapLanes)
        {
            AlignLineEndPoints(lane, lane.leftLineBoundry, true);
            AlignLineEndPoints(lane, lane.rightLineBoundry, false);
        }
    }

    private void AlignLineEndPoints(MapLane lane, MapLine line, bool isLeft)
    {
        var sameDirection = isSameDirection(lane, line);
        if (lane.befores.Count > 0)
        {
            var lineBefore = isLeft ? lane.befores[0].leftLineBoundry : lane.befores[0].rightLineBoundry;
            var startIdx = sameDirection ? 0 : line.mapWorldPositions.Count - 1;
            line.mapWorldPositions[startIdx] = getClosestPoint(line.mapWorldPositions[startIdx], lineBefore);
        }

        if (lane.afters.Count > 0)
        {
            var lineAfter = isLeft ? lane.afters[0].leftLineBoundry : lane.afters[0].rightLineBoundry;
            var endIndex = sameDirection ? line.mapWorldPositions.Count - 1 : 0;
            line.mapWorldPositions[endIndex] = getClosestPoint(line.mapWorldPositions[endIndex], lineAfter);
        }
        ApolloMapImporter.UpdateLocalPositions(line);
    }

    public void RemoveExtraLines(bool showMsg=true, MapHolder mapHolder=null)
    {
        var mapAnnotationData = new MapManagerData();
        if (mapAnnotationData.MapHolder == null)
            return;

        if (mapHolder != null) mapAnnotationData.MapHolder = mapHolder;

        Record(mapAnnotationData, out UnityEngine.GameObject root,
            out string assetPath, out bool isPrefab, "Remove extra boundary lines");

        var mapIntersections = mapAnnotationData.GetIntersections();
        var mapLaneSections = mapAnnotationData.GetLaneSections();

        ExtraLinesCnt = 0;
        var allLanes = new HashSet<MapLane>(mapAnnotationData.GetData<MapLane>());
        AddWorldPositions(allLanes.ToList());
        ApolloMapImporter.LinkSegments(allLanes);
        foreach (var mapLaneSection in mapLaneSections)
        {
            var mapLanes = new List<MapLane>(mapLaneSection.GetComponentsInChildren<MapLane>());
            FindAndRemoveExtraLines(mapLanes);
        }
        if (showMsg) Debug.Log($"Removed {ExtraLinesCnt} extra boundary lines from MapLaneSections.");
        var changed = ExtraLinesCnt > 0;

        ExtraLinesCnt = 0;
        foreach (var mapIntersection in mapIntersections)
        {
            var mapLanes = new List<MapLane>(mapIntersection.GetComponentsInChildren<MapLane>());
            AlignLineEndPoints(mapLanes);
            FindAndRemoveExtraLines(mapLanes);
        }
        if (showMsg) Debug.Log($"Removed {ExtraLinesCnt} extra boundary lines from MapIntersections.");
        changed = changed || ExtraLinesCnt > 0;

        SaveOrUndo(root, assetPath, isPrefab, changed);
    }

    private static void Record(MapManagerData mapAnnotationData, out UnityEngine.GameObject root,
        out string assetPath, out bool isPrefab, string recordName)
    {
        Undo.RecordObject(mapAnnotationData.MapHolder, recordName);
        PrefabUtility.RecordPrefabInstancePropertyModifications(mapAnnotationData.MapHolder);

        root = PrefabUtility.GetOutermostPrefabInstanceRoot(mapAnnotationData.MapHolder);
        assetPath = "";
        isPrefab = root != null;
        if (isPrefab)
        {
            assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
        }
    }

    private static void SaveOrUndo(UnityEngine.GameObject root, string assetPath, bool isPrefab, bool changed)
    {
        if (isPrefab)
        {
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, assetPath, InteractionMode.UserAction);
                Debug.Log($"Updated map prefab: {assetPath}");
            }
            else
            {
                Undo.PerformUndo(); // Undo changes due to precision
            }
        }
    }

    private void CreateLines()
    {
        var mapAnnotationData = new MapManagerData();
        if (mapAnnotationData.MapHolder == null)
            return;
        mapAnnotationData.GetTrafficLanes(); // Set lane relations

        Record(mapAnnotationData, out UnityEngine.GameObject root,
            out string assetPath, out bool isPrefab, "Create fake boundary lines");

        var laneSegments = new HashSet<MapLane>(mapAnnotationData.GetData<MapLane>());
        var fakeBoundaryLineList = new List<MapLine>();
        var changed = false;
        if (!Lanelet2MapExporter.AreAllLanesWithBoundaries(laneSegments))
        {
            fakeBoundaryLineList = Lanelet2MapExporter.CreateFakeBoundariesFromLanes(laneSegments);
            changed = true;
        }

        Debug.Log($"Created {fakeBoundaryLineList.Count} fake boundary lines.");

        SaveOrUndo(root, assetPath, isPrefab, changed);
    }
}
