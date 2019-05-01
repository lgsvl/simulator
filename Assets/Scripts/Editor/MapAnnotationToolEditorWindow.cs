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
    private List<MapWaypoint> tempWaypoints = new List<MapWaypoint>();
    //private List<MapLaneSegmentBuilder> mapLaneBuilder_selected = new List<MapLaneSegmentBuilder>();
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
    private int laneLeftBoundryType = 0;
    private int laneRightBoundryType = 0;
    private GUIContent[] laneBoundryTypeContent = {
        new GUIContent { text = "UNKOWN", tooltip = "Unknown boundry" },
        new GUIContent { text = "DOTTED_YELLOW", tooltip = "Dotted yellow boundry" },
        new GUIContent { text = "DOTTED_WHITE", tooltip = "Dotted white boundry" },
        new GUIContent { text = "SOLID_YELLOW", tooltip = "Solid yellow boundry" },
        new GUIContent { text = "SOLID_WHITE", tooltip = "Solid white boundry" },
        new GUIContent { text = "DOUBLE_YELLOW", tooltip = "Double yellow boundry" },
        new GUIContent { text = "CURB", tooltip = "Curb boundry" },
    };
    private int laneSpeedLimit = 25;

    private int boundryType = 0;

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
    }

    private void OnEnable()
    {
        layerMask = 1 << LayerMask.NameToLayer("Default");
        if (targetWaypointGO != null)
            DestroyImmediate(targetWaypointGO);
    }

    private void OnGUI()
    {
        // Modes
        var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 10 };

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Map Annotation Modes", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);

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
        if (GUILayout.Button(new GUIContent("Create Waypoint Mode: " + MapAnnotationTool.TEMP_WAYPOINT_MODE, "Enter mode to create waypoints for annotation creation"), GUILayout.Height(25)))
        {
            ToggleTempWaypointMode();
        }

        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);

        // waypoint
        if (MapAnnotationTool.TEMP_WAYPOINT_MODE)
        {
            EditorGUILayout.LabelField("Create Waypoint", titleLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            parentObj = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Parent Object", "This object will hold all new annotation objects created"), parentObj, typeof(GameObject), true);
            LayerMask tempMask = EditorGUILayout.MaskField(new GUIContent("Ground Snap Layer Mask", "The ground and road layer to snap objects waypoints"),
                                                           UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask),
                                                           UnityEditorInternal.InternalEditorUtility.layers);
            layerMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            waypointTotal = EditorGUILayout.IntField(new GUIContent("Waypoint count", "Number of waypoints when connected *MINIMUM 3*"), waypointTotal);
            if (waypointTotal < 3)
                waypointTotal = 3;

            EditorGUILayout.LabelField("Map Object Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
            createType = GUILayout.Toolbar(createType, createTypeContent);

            switch (createType)
            {
                case 0:
                    // TODO needs drop down menu
                    EditorGUILayout.LabelField("Lane Turn Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    laneTurnType = GUILayout.Toolbar(laneTurnType, laneTurnTypeContent);
                    EditorGUILayout.LabelField("Left Boundry Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    laneLeftBoundryType = GUILayout.SelectionGrid(laneLeftBoundryType, laneBoundryTypeContent, 3);
                    EditorGUILayout.LabelField("Right Boundry Type", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                    laneRightBoundryType = GUILayout.SelectionGrid(laneRightBoundryType, laneBoundryTypeContent, 3);
                    laneSpeedLimit = EditorGUILayout.IntField(new GUIContent("Speed Limit", "Lane speed limit in MPH"), laneSpeedLimit);
                    break;
                case 1:
                    //
                    break;
                case 2:
                    //
                default:
                    break;
            }

            GUILayout.BeginHorizontal("create");
            if (GUILayout.Button(new GUIContent("Waypoint", waypointButtonImages[0], "Create a temporary waypoint object in scene on layer")))
                CreateTempWaypoint();
            if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[1], "Connect waypoints to make a straight line")))
                CreateStraight();
            if (GUILayout.Button(new GUIContent("Connect", waypointButtonImages[2], "Connect waypoints to make a curved line")))
                CreateCurved();
            if (GUILayout.Button(new GUIContent("Delete All", waypointButtonImages[3], "Delete all temporary waypoints")))
                ClearAllTempWaypoints();
            GUILayout.EndHorizontal();
        }

    }

    private void Update()
    {
        if (MapAnnotationTool.TEMP_WAYPOINT_MODE && targetWaypointGO != null)
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

    private void ToggleTempWaypointMode()
    {
        MapAnnotationTool.TEMP_WAYPOINT_MODE = !MapAnnotationTool.TEMP_WAYPOINT_MODE;

        MapAnnotationTool.SHOW_MAP_ALL = MapAnnotationTool.TEMP_WAYPOINT_MODE ? true : false;
        MapAnnotationTool.SHOW_MAP_SELECTED = MapAnnotationTool.TEMP_WAYPOINT_MODE ? true : false;
        if (MapAnnotationTool.TEMP_WAYPOINT_MODE)
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
                case 0:
                    newGo.GetComponent<MapLane>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    break;
                case 1:
                    newGo.GetComponent<MapLine>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLine>().lineType = MapData.LineType.STOP;
                    break;
                case 2:
                    newGo.GetComponent<MapLine>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLine>().lineType = MapData.LineType.SOLID_WHITE;
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
                case 0:
                    newGo.GetComponent<MapLane>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    break;
                case 1:
                    newGo.GetComponent<MapLine>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLine>().lineType = MapData.LineType.STOP;
                    break;
                case 2:
                    newGo.GetComponent<MapLine>().mapLocalPositions.Add(newGo.transform.InverseTransformPoint(p));
                    newGo.GetComponent<MapLine>().lineType = MapData.LineType.SOLID_WHITE;
                    break;
            }
        }

        newGo.transform.SetParent(parentObj == null ? null : parentObj.transform);

        tempWaypoints.ForEach(p => Undo.DestroyObjectImmediate(p.gameObject));
        tempWaypoints.Clear();

        Selection.activeObject = newGo;
    }
}
