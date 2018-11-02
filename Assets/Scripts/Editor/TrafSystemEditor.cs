/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;

public enum TrafEditState { IDLE, EDIT, EDIT_MULTI, EDIT_MULTI_TWOWAY, EDIT_MULTI_RA, EDIT_MULTI_RA_TWOWAY, DISPLAY_ENTRIES, DISPLAY_SPLINES, DISPLAY_INTERSECTION_SPLINES, EDIT_ENDS, EDIT_MULTI_3, EDIT_MULTI_6, DISPLAY_INTERSECTIONS, DISPLAY_ENTRIES_INTERSECTIONS, DISPLAY_SINGLE }

[CustomEditor(typeof(TrafSystem))]
public class TrafSystemEditor : Editor {

    private const float terrainOffsetUp = 2f;
    private const float startDisplayOffsetRight = 10f;
    private const float maxEdgesConnectedDistance = 15f;

    private TrafEditState currentState = TrafEditState.IDLE;
    private TrafRoad currentRoad;
#pragma warning disable 0649
    private List<Vector3>[] currentRoadMulti;
#pragma warning restore 0649
    private float laneWidth = 4f;

    public int currentId;
    public int currentSubId;

    public int specificId;
    public int specificSubId;
    public int specificIndex;

    public int fromId;
    public int fromSubId;
    public int toId;
    public int toSubId;

    private bool showIds = false;

    

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical();
        //EditorGUILayout.BeginHorizontal();
        currentId = EditorGUILayout.IntField("ID", currentId);
        if(GUILayout.Button("Next Free ID"))
        {
            var t = target as TrafSystem;
            currentId = t.entries.Max(e => e.identifier) + 1;
        }
        currentSubId = EditorGUILayout.IntField("SubID", currentSubId);
        //EditorGUILayout.EndHorizontal();

        switch(currentState)
        {
            case TrafEditState.IDLE:
                if(GUILayout.Button("DELETE - CAREFUL!"))
                {
                    var t = target as TrafSystem;
                    t.entries.RemoveAll(e => e.identifier == currentId);
                }

                if(GUILayout.Button("new/edit"))
                {
                    var t = target as TrafSystem;
                    if(t.entries.Any(entry => entry.identifier == currentId && entry.subIdentifier == currentSubId))
                    {
                        var road  = t.entries.Find(entry => entry.identifier == currentId && entry.subIdentifier == currentSubId);
                        currentRoad = road.road;
                        t.entries.Remove(road);
                        currentState = TrafEditState.EDIT;
                    }
                    else
                    {
                        currentRoad = ScriptableObject.CreateInstance(typeof(TrafRoad)) as TrafRoad;
                        InitRoad(currentRoad);
                        currentState = TrafEditState.EDIT;
                    }
                }

                if(GUILayout.Button("new multi one way"))
                {
                    var t = target as TrafSystem;
                    if(t.entries.Any(entry => entry.identifier == currentId))
                    {
                        Debug.Log("TrafSystem: A road with that ID already exists. Multi editing not supported");
                    }
                    else
                    {
                        currentRoad = ScriptableObject.CreateInstance(typeof(TrafRoad)) as TrafRoad;
                        InitRoad(currentRoad);
                        currentState = TrafEditState.EDIT_MULTI;
                    }
                }

                if(GUILayout.Button("new multi two way"))
                {
                    var t = target as TrafSystem;
                    if(t.entries.Any(entry => entry.identifier == currentId))
                    {
                        Debug.Log("TrafSystem: A road with that ID already exists. Multi editing not supported");
                    }
                    else
                    {
                        currentRoad = ScriptableObject.CreateInstance(typeof(TrafRoad)) as TrafRoad;
                        InitRoad(currentRoad);
                        currentState = TrafEditState.EDIT_MULTI_TWOWAY;
                    }
                }

                if (GUILayout.Button("Visualize Entries and Intersections"))
                {
                    currentState = TrafEditState.DISPLAY_ENTRIES_INTERSECTIONS;
                    Debug.Log("Total entry count " + (target as TrafSystem).entries.Count);
                    Debug.Log("Total intersection count " + (target as TrafSystem).intersections.Count);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Visualize Entries"))
                {
                    currentState = TrafEditState.DISPLAY_ENTRIES;
                    Debug.Log("Total entry count " + (target as TrafSystem).entries.Count);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Visualize Intersections"))
                {
                    currentState = TrafEditState.DISPLAY_INTERSECTIONS;
                    Debug.Log("Total intersection count " + (target as TrafSystem).intersections.Count);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Register all intersections"))
                {
                    var t = target as TrafSystem;
                    t.intersections.Clear();
                    TrafIntersectionEditor.GoAll(t);
                    EditorUtility.SetDirty(target);
                }

                if(GUILayout.Button("Deregister all intersections"))
                {
                    var t = target as TrafSystem;
                    t.intersections.Clear();
                    EditorUtility.SetDirty(target);
                }

                if (GUILayout.Button("Populate all owning entries"))
                {
                    var t = target as TrafSystem;
                    foreach (var i in t.intersections)
                    {
                        if (i.intersection != null)
                        {
                            i.intersection.owningEntry = i;
                        }
                    }
                    EditorUtility.SetDirty(target);
                }

                if(GUILayout.Button("Generate RoadGraph"))
                {
                    GenerateRoadGraph();
                    EditorUtility.SetDirty(target);
                }

                if(GUILayout.Button("Clear RoadGraph"))
                {
                    var t = target as TrafSystem;
                    t.roadGraph = new TrafRoadGraph();
                    EditorUtility.SetDirty(target);
                }

                if(GUILayout.Button("Remove Markers"))
                {
                    var t = target as TrafSystem;
                    foreach(var g in t.GetComponentsInChildren<Renderer>())
                    {
                        g.enabled = false;
                    }
                }

                if(GUILayout.Button("TEMP"))
                {
                    var t = target as TrafSystem;
                    foreach(var e in t.entries)
                    {
                        if(e.road != null)
                        {
                            e.waypoints = new List<Vector3>();
                            foreach(var v in e.road.waypoints)
                            {
                                e.waypoints.Add(v.position);
                            }
                        }
                    }
                }

                if (GUILayout.Button("Localize Waypoint World Transformations"))
                {
                    var t = target as TrafSystem;
                    var allEntries = new List<TrafEntry>();
                    allEntries.AddRange(t.entries);
                    allEntries.AddRange(t.intersections);
                    foreach (var e in allEntries)
                    {
                        for (int i = 0; i < e.waypoints.Count; i++)
                        {
                            e.waypoints[i] = t.transform.localToWorldMatrix.MultiplyPoint(e.waypoints[i]);
                        }
                    }
                    t.transform.position = Vector3.zero;
                    t.transform.rotation = Quaternion.identity;
                    t.transform.localScale = Vector3.one;
                }

                if (GUILayout.Button("EXPORT DATA"))
                {
                    var t = target as TrafSystem;
                    var intersections = t.intersections;
                    string output = "";
                    for (int i = 0; i < intersections.Count; i++)
                    {
                        var inter = intersections[i];
                        string index = i.ToString();
                        string name = inter.light == null ? "" : inter.light.gameObject.name;
                        string x = inter.light == null ? "" : inter.light.transform.position.x.ToString();
                        string y = inter.light == null ? "" : inter.light.transform.position.y.ToString();
                        string z = inter.light == null ? "" : inter.light.transform.position.z.ToString();
                        output += $"{index},{name},{x},{y},{z}\n";
                    }
                    using (StreamWriter sw = File.CreateText("dataTransfer.rl"))
                    {
                        sw.Write(output);
                    }
                }

                if (GUILayout.Button("READ DATA"))
                {
                    var t = target as TrafSystem;
                    var intersections = t.intersections;
                    var lines = File.ReadAllLines("dataTransfer.rl");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        string[] items = line.Split(',');
                        string searchName = items[1];
                        if (searchName == "")
                        {
                            continue;
                        }
                        Vector3 pos = new Vector3(float.Parse(items[2]), float.Parse(items[3]), float.Parse(items[4]));
                        var containers = GameObject.FindObjectsOfType<TrafficLightContainer>();
                        foreach (var c in containers)
                        {
                            if (c.gameObject.name != searchName)
                            {
                                continue;
                            }
                            float dist = (c.transform.position - pos).magnitude;
                            if (dist < 0.01f)
                            {
                                Debug.Log("assign");
                                intersections[i].light = c;
                                break;
                            }
                        }
                    }
                }

                if (GUILayout.Button("Debug Number"))
                {
                    var t = target as TrafSystem;
                    var intersections = t.intersections;
                    int num = 0;
                    foreach (var inter in intersections)
                    {
                        if (inter.light != null)
                        {
                            num++;
                        }
                    }
                    Debug.Log(num);
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Generate Lanes Texture"))
                {
                    GenerateInfractionsTexture();
                }

                if (GUILayout.Button("Generate Highway Lanes Texture"))
                {
                    GenerateInfractionsTextureHighway();
                }

                if (GUILayout.Button("Apply stoplight infraction triggers"))
                {
                    ApplyStoplightInfractionTriggers();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Specific Entry:");            

                specificId = EditorGUILayout.IntField("ID", specificId);
                specificSubId = EditorGUILayout.IntField("SubID", specificSubId);
                specificIndex = EditorGUILayout.IntField("Index", specificIndex);

                if (GUILayout.Button("Visualize single entry/intersection"))
                {
                    currentState = TrafEditState.DISPLAY_SINGLE;
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Snap scene camera to specific entry point(\"-1\" is end point)"))
                {
                    SnapSceneCameraToEntry();
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Debug road graph"))
                {
                    DebugRoadGraph();
                }

                if (GUILayout.Button("Auto fix road graph"))
                {
                    AutoFixRoadGraph();
                }

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                fromId = EditorGUILayout.IntField("From ID", fromId);
                fromSubId = EditorGUILayout.IntField("From sub ID", fromSubId);
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Manual check road graph edge"))
                {
                    ManualCheckRoadGraph(fromId, fromSubId);
                }

                EditorGUILayout.BeginHorizontal();
                toId = EditorGUILayout.IntField("To ID", toId);
                toSubId = EditorGUILayout.IntField("To sub ID", toSubId);
                EditorGUILayout.EndHorizontal();


                if (GUILayout.Button("Manual connect road graph edge"))
                {
                    ManualConnectRoadGraph(fromId, fromSubId, toId, toSubId);
                }

                if (GUILayout.Button("Manual remove road graph edge"))
                {
                    ManualRemoveRoadGraph(fromId, fromSubId, toId, toSubId);
                }

                // eric
                EditorGUILayout.Space();
                if (GUILayout.Button("Find ID Index(s)"))
                {
                    var t = target as TrafSystem;
                    for (int i = 0; i < t.entries.Count; i++)
                    {
                        if (t.entries[i].identifier == specificId)
                        {
                            Debug.Log("entries Index: " + i);
                        }
                    }
                    for (int i = 0; i < t.intersections.Count; i++)
                    {
                        if (t.intersections[i].identifier == specificId)
                        {
                            Debug.Log("Intersection Index: " + i);
                        }
                    }
                }

                if (GUILayout.Button("Remove Entry by ID"))
                {
                    var t = target as TrafSystem;
                    int count = 0;
                    for (int i = 0; i < t.entries.Count; i++)
                    {
                        if (t.entries[i].identifier == specificId)
                        {
                            count++;
                            Debug.Log("Entry found count: " + count);
                        }
                    }

                    for (int i = 0; i < count; i++)
                    {
                        for (int j = 0; j < t.entries.Count; j++)
                        {
                            if (t.entries[j].identifier == specificId)
                            {
                                t.entries.RemoveAt(j);
                                Debug.Log("Removed entries index: " + j + " loop count: " + i);
                                break;
                            }
                        }
                    }
                }

                if (GUILayout.Button("Remove Intersection by ID"))
                {
                    var t = target as TrafSystem;
                    int count = 0;
                    for (int i = 0; i < t.intersections.Count; i++)
                    {
                        if (t.intersections[i].identifier == specificId)
                        {
                            count++;
                            Debug.Log("Intersection found count: " + count);
                        }
                    }

                    for (int i = 0; i < count; i++)
                    {
                        for (int j = 0; j < t.intersections.Count; j++)
                        {
                            if (t.intersections[j].identifier == specificId)
                            {
                                t.intersections.RemoveAt(j);
                                Debug.Log("Removed Intersection Index: " + j + " loop count: " + i);
                                break;
                            }
                        }
                    }
                }

                break;
            case TrafEditState.EDIT:
                if(GUILayout.Button("save"))
                {
                    var t = target as TrafSystem;
                    currentState = TrafEditState.IDLE;
                    t.entries.Add(new TrafEntry() { road = currentRoad, identifier = currentId, subIdentifier = currentSubId });
                }
                break;
            case TrafEditState.EDIT_MULTI:
                laneWidth = EditorGUILayout.Slider("lane width", laneWidth, 0f, 50f);

                if(GUILayout.Button("save"))
                {
                    var t = target as TrafSystem;
                    currentState = TrafEditState.IDLE;
                    t.entries.Add(new TrafEntry() { road = InitRoadMulti(currentRoad, laneWidth * -1.5f, false), identifier = currentId, subIdentifier = 0 });
                    t.entries.Add(new TrafEntry() { road = InitRoadMulti(currentRoad, laneWidth * -.5f, false), identifier = currentId, subIdentifier = 1 });
                    t.entries.Add(new TrafEntry() { road = InitRoadMulti(currentRoad, laneWidth * .5f, false), identifier = currentId, subIdentifier = 2 });
                    t.entries.Add(new TrafEntry() { road = InitRoadMulti(currentRoad, laneWidth * 1.5f, false), identifier = currentId, subIdentifier = 3 });
                }

                if(GUILayout.Button("cancel"))
                {
                    currentState = TrafEditState.IDLE;
                }
                break;

            case TrafEditState.EDIT_MULTI_TWOWAY:
                laneWidth = EditorGUILayout.Slider("lane width", laneWidth, 0f, 50f);

                if(GUILayout.Button("save"))
                {
                    var t = target as TrafSystem;
                    currentState = TrafEditState.IDLE;
                    t.entries.Add(new TrafEntry() { road = InitRoadMulti(currentRoad, laneWidth * -1.5f, true), identifier = currentId, subIdentifier = 0 });
                    t.entries.Add(new TrafEntry() { road = InitRoadMulti(currentRoad, laneWidth * -.5f, true), identifier = currentId, subIdentifier = 1 });
                    t.entries.Add(new TrafEntry() { road = InitRoadMulti(currentRoad, laneWidth * .5f, false), identifier = currentId, subIdentifier = 2 });
                    t.entries.Add(new TrafEntry() { road = InitRoadMulti(currentRoad, laneWidth * 1.5f, false), identifier = currentId, subIdentifier = 3 });
                }
                break;

            case TrafEditState.EDIT_MULTI_RA:
                laneWidth = EditorGUILayout.Slider("lane width", laneWidth, 0f, 50f);
                if(GUILayout.Button("save"))
                {
                    var t = target as TrafSystem;
                    currentState = TrafEditState.IDLE;
                    t.entries.Add(new TrafEntry() { identifier = currentId, subIdentifier = 0, waypoints = currentRoadMulti[0] });
                    t.entries.Add(new TrafEntry() { identifier = currentId, subIdentifier = 1, waypoints = currentRoadMulti[1] });
                    t.entries.Add(new TrafEntry() { identifier = currentId, subIdentifier = 2, waypoints = currentRoadMulti[2] });
                    t.entries.Add(new TrafEntry() { identifier = currentId, subIdentifier = 3, waypoints = currentRoadMulti[3] });
                }
                break;

            case TrafEditState.DISPLAY_ENTRIES:
                if (GUILayout.Button("Back"))
                {
                    currentState = TrafEditState.IDLE;
                    SceneView.RepaintAll();
                }
                showIds = GUILayout.Toggle(showIds, "Show IDs?");
                break;

            case TrafEditState.DISPLAY_INTERSECTIONS:
                if (GUILayout.Button("Back"))
                {
                    currentState = TrafEditState.IDLE;
                    SceneView.RepaintAll();
                }
                showIds = GUILayout.Toggle(showIds, "Show IDs?");
                break;

            case TrafEditState.DISPLAY_ENTRIES_INTERSECTIONS:
                if (GUILayout.Button("Back"))
                {
                    currentState = TrafEditState.IDLE;
                    SceneView.RepaintAll();
                }
                showIds = GUILayout.Toggle(showIds, "Show IDs?");
                break;
            case TrafEditState.DISPLAY_SINGLE:
                if (GUILayout.Button("Back"))
                {
                    currentState = TrafEditState.IDLE;
                    SceneView.RepaintAll();
                }
                showIds = GUILayout.Toggle(showIds, "Show IDs?");
                break;
        }

        EditorGUILayout.EndVertical();
        DrawDefaultInspector();
    }

    void OnSceneGUI()
    {
        switch(currentState)
        {
            case TrafEditState.EDIT:
                DrawPath(currentRoad);
                break;
            case TrafEditState.EDIT_MULTI:
                DrawPathMulti(currentRoad, laneWidth);
                break;
            case TrafEditState.EDIT_MULTI_TWOWAY:
                DrawPathMulti(currentRoad, laneWidth);
                break;
            case TrafEditState.DISPLAY_ENTRIES:
                VisualizeEntries();
                break;
            case TrafEditState.DISPLAY_INTERSECTIONS:
                VisualizeIntersections();
                break;
            case TrafEditState.DISPLAY_ENTRIES_INTERSECTIONS:
                VisualizeEntriesAndIntersections();
                break;
            case TrafEditState.DISPLAY_SINGLE:
                VisualizeSingle();
                break;
        }
    }

    private void ApplyStoplightInfractionTriggers()
    {
        var system = target as TrafSystem;
        foreach (Transform t in system.transform)
        {
            var inter = t.GetComponent<TrafIntersection>();
            if (!inter.stopSign)
            {
                GameObject go = new GameObject("infractionTrigger");
                var b = go.AddComponent<BoxCollider>();
                b.size = new Vector3(14.7f, 11f, 14.7f);

                go.transform.parent = t;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;

                b.isTrigger = true;
            }
        }
    }

    private void SnapSceneCameraToEntry()
    {
        var trafSystem = target as TrafSystem;
        var entry = trafSystem.GetEntry(specificId, specificSubId);
        if (entry != null)
        {
            if (specificIndex < 0)
            {
                specificIndex += entry.waypoints.Count;
            }

            if (specificIndex > -1 && specificIndex < entry.waypoints.Count)
            {
                Vector3 snapPoint = entry.waypoints[specificIndex];
                SceneView.lastActiveSceneView.pivot = snapPoint;
                SceneView.lastActiveSceneView.Repaint();
            }
        }
    }

    private void VisualizeSingle()
    {
        var trafSystem = target as TrafSystem;
        var e = trafSystem.GetEntry(specificId, specificSubId);

        if (e == null)
        {
            Debug.Log("No Such Entry!");
            return;
        }
        var points = e.GetPoints();
        if (e.isIntersection())
        {
            Handles.color = Color.green;
            Handles.DrawPolyLine(points);
            Handles.color = Color.red;
            //Vector3 position = Vector3.Lerp(points[points.Length / 2 - 1], points[points.Length / 2], 0.5f);
            //Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(points[points.Length / 2] - points[points.Length / 2 - 1]), 15f, EventType.Repaint);

            foreach (var p in e.GetPoints())
            {
                Handles.DrawWireCube(p, Vector3.one);
            }

            if (showIds)
            {
                //Vector3 pos = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(position);
                GUIStyle style = new GUIStyle(); style.normal.textColor = Color.white;
                Handles.Label(points[0], e.identifier + "_" + e.subIdentifier + "_" + 0 + "(ST)", style);
                for (int i = 1; i < points.Length - 1; i++)
                { Handles.Label(points[i], e.identifier + "_" + e.subIdentifier + "_" + i, style); }
                Handles.Label(points[points.Length - 1], e.identifier + "_" + e.subIdentifier + "_" + (points.Length - 1) + "(ED)", style);
                //GUI.Label(new Rect(pos.x, pos.y, 200, 100) , r.identifier + "_" + r.subIdentifier);
            }
        }
        else
        {
            Handles.color = Color.green;
            Handles.DrawPolyLine(points);
            Handles.color = Color.red;
            Vector3 position = Vector3.Lerp(points[points.Length / 2 - 1], points[points.Length / 2], 0.5f);
            Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(points[points.Length / 2] - points[points.Length / 2 - 1]), 15f, EventType.Repaint);

            foreach (var p in e.GetPoints())
            {
                Handles.DrawWireCube(p, Vector3.one);
            }

            if (showIds)
            {
                //Vector3 pos = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(position);
                GUIStyle style = new GUIStyle(); style.normal.textColor = Color.white;
                Handles.Label(points[0], e.identifier + "_" + e.subIdentifier + "_" + 0 + "(ST)", style);
                for (int i = 1; i < points.Length - 1; i++)
                { Handles.Label(points[i], e.identifier + "_" + e.subIdentifier + "_" + i, style); }
                Handles.Label(points[points.Length - 1], e.identifier + "_" + e.subIdentifier + "_" + (points.Length - 1) + "(ED)", style);
                //GUI.Label(new Rect(pos.x, pos.y, 200, 100) , r.identifier + "_" + r.subIdentifier);
            }
        }        
    }

    private void DebugRoadGraph()
    {
        var trafSystem = target as TrafSystem;
        foreach (var e in trafSystem.entries)
        {
            var node = trafSystem.roadGraph.GetNode(e.identifier, e.subIdentifier);
            if (node.edges == null)
            {
                Debug.Log(e.identifier + "_" + e.subIdentifier + "(non-intersection) has NULL edges");
            }
            else if (node.edges.Count < 1)
            {
                Debug.Log(e.identifier + "_" + e.subIdentifier + "(non-intersection) has EMPTY edges");
            }
        }

        foreach (var i in trafSystem.intersections)
        {
            var node = trafSystem.roadGraph.GetNode(i.identifier, i.subIdentifier);
            if (node.edges == null)
            {
                Debug.Log(i.identifier + "_" + i.subIdentifier + "(intersection) has NULL edges");
            }
            else if (node.edges.Count < 1)
            {
                Debug.Log(i.identifier + "_" + i.subIdentifier + "(intersection) has EMPTY edges");
            }
        }
    }

    private void AutoFixRoadGraph()
    {
        var trafSystem = target as TrafSystem;
        foreach (var e in trafSystem.entries)
        {
            var node = trafSystem.roadGraph.GetNode(e.identifier, e.subIdentifier);
            if (node.edges == null)
            {
                Debug.Log(e.identifier + "_" + e.subIdentifier + "(non-intersection) has NULL edges, fixing...");
                node.edges = new List<RoadGraphEdge>();
            }

            if (node.edges.Count < 1)
            {
                Debug.Log(e.identifier + "_" + e.subIdentifier + "(non-intersection) has EMPTY edges, fixing");

                //Start fixing
                Vector3 start = e.waypoints[0];
                Vector3 end = e.waypoints[e.waypoints.Count - 1];

                float minDist = 99999f;
                TrafEntry minEntry = null;

                foreach (var isection in trafSystem.intersections)
                {
                    float dist = Vector3.Distance(isection.waypoints[0], end);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minEntry = isection;
                    }
                }

                if (minDist < maxEdgesConnectedDistance)
                {
                    foreach (var iEntry in trafSystem.intersections.FindAll(x => (x.waypoints[0] - minEntry.waypoints[0]).magnitude < 0.001f))
                    {
                        AddEdge(trafSystem.roadGraph.roadGraph, e.identifier, e.subIdentifier, iEntry.identifier, iEntry.subIdentifier);
                    }
                }

                if (node.edges.Count < 1)
                {
                    Debug.Log(e.identifier + "_" + e.subIdentifier + "(non-intersection) still has EMPTY edges after the fix");
                }
                else
                {
                    Debug.Log(e.identifier + "_" + e.subIdentifier + "(non-intersection) now  has " + node.edges.Count + " edges after the fix");
                    Debug.Log("Added edges: ");
                    foreach (var edge in node.edges)
                    {
                        Debug.Log("Id: " + edge.id + " | SubId: " + edge.subId);
                    }
                }

                Debug.Log("Fix done");
            }
        }

        foreach (var i in trafSystem.intersections)
        {
            var node = trafSystem.roadGraph.GetNode(i.identifier, i.subIdentifier);
            if (node.edges == null)
            {
                Debug.Log(i.identifier + "_" + i.subIdentifier + "(intersection) has NULL edges, fixing...");
                node.edges = new List<RoadGraphEdge>();
            }

            if (node.edges.Count < 1)
            {
                Debug.Log(i.identifier + "_" + i.subIdentifier + "(intersection) has EMPTY edges, fixing");

                //Start fixing
                Vector3 start = i.waypoints[0];
                Vector3 end = i.waypoints[i.waypoints.Count - 1];

                float minDist = 99999f;
                TrafEntry minEntry = null;

                foreach (var entry in trafSystem.entries)
                {
                    float dist = Vector3.Distance(entry.waypoints[0], end);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minEntry = entry;
                    }
                }

                if (minDist < maxEdgesConnectedDistance)
                {
                    //dirty hack to exclude isection with no entry for 66_2 and 66_3
                    if (i.identifier == 62 && (i.subIdentifier == 2 || i.subIdentifier == 3))
                    {
                        //just ignore for now
                    }
                    else
                    {
                        foreach (var eEntry in trafSystem.entries.FindAll(x => (x.waypoints[0] - minEntry.waypoints[0]).magnitude < 0.001f))
                        {
                            AddEdge(trafSystem.roadGraph.roadGraph, i.identifier, i.subIdentifier, eEntry.identifier, eEntry.subIdentifier);
                        }
                    }
                }


                if (node.edges.Count < 1)
                {
                    Debug.Log(i.identifier + "_" + i.subIdentifier + "(intersection) still has EMPTY edges after the fix");
                }
                else
                {
                    Debug.Log(i.identifier + "_" + i.subIdentifier + "(intersection) now  has " + node.edges.Count + " edges after the fix");
                    Debug.Log("Added edges: ");
                    foreach (var edge in node.edges)
                    {
                        Debug.Log("Id: " + edge.id + " | SubId: " + edge.subId);
                    }
                }

                Debug.Log("Fix done");
            }
        }
    }

    private void ManualCheckRoadGraph(int fromId, int fromSubId)
    {
        var trafSystem = target as TrafSystem;
        var roadGraphNode = trafSystem.roadGraph.roadGraph[fromId * 50 + fromSubId];
        if (roadGraphNode.edges == null)
        {
            Debug.Log("NULL road graph edges");
        }
        else if (roadGraphNode.edges.Count == 0)
        {
            Debug.Log("EMPTY road graph edges");
        }
        else
        {
            Debug.Log("Connected road graph edges:");
            foreach (var e in roadGraphNode.edges)
            {
                Debug.Log("id: " + e.id + " sub id: " + e.subId); 
            }
        }
    }

    private void ManualConnectRoadGraph(int fromId, int fromSubId, int toId, int toSubId)
    {
        var trafSystem = target as TrafSystem;
        AddEdge(trafSystem.roadGraph.roadGraph, fromId, fromSubId, toId, toSubId);
    }

    private void ManualRemoveRoadGraph(int fromId, int fromSubId, int toId, int toSubId)
    {
        var trafSystem = target as TrafSystem;
        RemoveEdge(trafSystem.roadGraph.roadGraph, fromId, fromSubId, toId, toSubId);
    }

    private void GenerateRoadGraph()
    {
        var system = target as TrafSystem;
        system.roadGraph = new TrafRoadGraph();


        foreach(var entry in system.entries)
        {
            Vector3 start = entry.waypoints[0];
            Vector3 end = entry.waypoints[entry.waypoints.Count - 1];

            //set up spline path
            entry.spline = new List<SplineNode>();
            entry.spline.Add(new SplineNode()
            {
                position = entry.waypoints[0],
                tangent = (entry.waypoints[1] - entry.waypoints[0]).normalized
            });
            for (int i = 1; i < entry.waypoints.Count - 1; i++)
            {
                entry.spline.Add(new SplineNode()
                {
                    position = entry.waypoints[i],
                    tangent = Vector3.Lerp((entry.waypoints[i] - entry.waypoints[i-1]).normalized, (entry.waypoints[i + 1] - entry.waypoints[i]).normalized, 0.5f).normalized
                });

            }
            entry.spline.Add(new SplineNode()
            {
                position = entry.waypoints[entry.waypoints.Count - 1],
                tangent = (entry.waypoints[entry.waypoints.Count - 1] - entry.waypoints[entry.waypoints.Count - 2]).normalized
            });

            float minDist = 99999f;
            TrafEntry minEntry = null;

            //intersections we can end up at
            foreach(var isection in system.intersections)
            {
                float dist = Vector3.Distance(isection.waypoints[0], end);
                  if( dist < minDist)
                {
                    minDist = dist;
                    minEntry = isection;
                }
            }

            if(minDist < maxEdgesConnectedDistance)
            {
                foreach(var e in system.intersections.FindAll(i => i.waypoints[0] == minEntry.waypoints[0]))
                {
                    AddEdge(system.roadGraph.roadGraph, entry.identifier, entry.subIdentifier, e.identifier, e.subIdentifier);

                    //fix spline end tangent
                    entry.spline[entry.spline.Count - 1].tangent = e.path.start.transform.forward;
                }
            }

            //intersections we could have come from
            minDist = 99999f;
            minEntry = null;
            foreach(var isection in system.intersections)
            {
                float dist = Vector3.Distance(isection.waypoints[isection.waypoints.Count - 1], start);
                if(dist < minDist)
                {
                    minDist = dist;
                    minEntry = isection;
                }
            }

            if(minDist < maxEdgesConnectedDistance)
            {
                //dirty hack to exclude isection with no entry for 66_2 and 66_3
                if(entry.identifier == 62 && (entry.subIdentifier == 2 || entry.subIdentifier == 3))
                {
                    //just ignore for now

                }
                else
                {
                    foreach (var e in system.intersections.FindAll(i => i.waypoints[i.waypoints.Count - 1] == minEntry.waypoints[i.waypoints.Count - 1]))
                    {
                        AddEdge(system.roadGraph.roadGraph, e.identifier, e.subIdentifier, entry.identifier, entry.subIdentifier);

                        //fix spline start tangent
                        entry.spline[0].tangent = e.path.end.transform.forward;
                    }
                }
            }
        }
    }

    private void AddEdge(RoadGraphNode[] roadGraph, int id, int subId, int toId, int toSubId)
    {

        if (roadGraph[id * 50 + subId] == null)
        {
            roadGraph[id * 50 + subId] = new RoadGraphNode();
            roadGraph[id * 50 + subId].edges = new List<RoadGraphEdge>();
        }

        if (roadGraph[id * 50 + subId].edges.Any(e => e.id == toId && e.subId == toSubId))
        {
            //already exists

        }
        else
        {
            roadGraph[id * 50 + subId].edges.Add(new RoadGraphEdge() { id = toId, subId = toSubId });

        }
    }

    private void RemoveEdge(RoadGraphNode[] roadGraph, int id, int subId, int toId, int toSubId)
    {

        if (roadGraph[id * 50 + subId] == null)
        {
            roadGraph[id * 50 + subId] = new RoadGraphNode();
            roadGraph[id * 50 + subId].edges = new List<RoadGraphEdge>();
        }

        if (roadGraph[id * 50 + subId].edges.Any(e => e.id == toId && e.subId == toSubId))
        {
            //remove only exist
            roadGraph[id * 50 + subId].edges.RemoveAll(e => e.id == toId && e.subId == toSubId);
        }
        else
        {
            Debug.Log("No corresponding edge exist, stop removing");
        }
    }


    private TrafRoad InitRoadMulti(TrafRoad cursor, float offset, bool reverse)
    {
        var r = ScriptableObject.CreateInstance(typeof(TrafRoad)) as TrafRoad;
        r.waypoints = new List<TrafRoadPoint>();
        for(int wp = 0; wp < cursor.waypoints.Count; wp++)
        {
            if(wp < cursor.waypoints.Count - 1)
            {
                Vector3 tangent = cursor.waypoints[wp + 1].position - cursor.waypoints[wp].position;
                r.waypoints.Add(new TrafRoadPoint() { position = cursor.waypoints[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * offset });
            }
            else
            {
                Vector3 tangent = cursor.waypoints[wp].position - cursor.waypoints[wp - 1].position;
                r.waypoints.Add(new TrafRoadPoint() { position = cursor.waypoints[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * offset });
            }
        }

        if(reverse)
        {
            r.waypoints.Reverse();
        }

        return r;
    }

    public void InitRoad(TrafRoad road)
    {
        var t = road;
        t.waypoints = new List<TrafRoadPoint>();
        //var scenecam = SceneView.currentDrawingSceneView.camera;
        var scenecam = SceneView.lastActiveSceneView.camera;
        RaycastHit[] hits = Physics.RaycastAll(scenecam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 2f)), 1000f);
        for(int i = 0; i < hits.Length; i++)
        {

            if(t != null)
            {
                Vector3 pos = hits[i].point + Vector3.up * terrainOffsetUp;
                t.waypoints.Add(new TrafRoadPoint { position = pos });
                t.waypoints.Add(new TrafRoadPoint { position = pos + Vector3.right * startDisplayOffsetRight });
                break;
            }
        }
    }

    public static void DrawPath(TrafRoad t)
    {
        if(t.waypoints != null && t.waypoints.Count > 0)
        {
            var wps = t.waypoints;
            for(int wp = 0; wp < wps.Count; wp++)
            {

                    wps[wp].position = Handles.PositionHandle(wps[wp].position, Quaternion.identity);

                if(wp == 0)
                    Handles.color = Color.red;
                else if(wp == wps.Count - 1)
                    Handles.color = Color.green;
                else
                    Handles.color = Color.yellow;

                Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position, Quaternion.identity, 1f, EventType.Repaint);
            }
            Handles.DrawPolyLine(wps.Select(w => w.position).ToArray());
        }
    }

    public static void DrawPathMulti(TrafRoad r, float laneWidth)
    {
        if(r.waypoints != null && r.waypoints.Count > 0)
        {
            var wps = r.waypoints;
            for(int wp = 0; wp < wps.Count; wp++)
            {

                wps[wp].position = Handles.PositionHandle(wps[wp].position, Quaternion.identity);

                if(wp == 0)
                    Handles.color = Color.red;
                else if(wp == wps.Count - 1)
                    Handles.color = Color.green;
                else
                    Handles.color = Color.yellow;

                if(wp < r.waypoints.Count - 1)
                {
                    Vector3 tangent = wps[wp + 1].position - wps[wp].position;
                    Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * -1.5f, Quaternion.identity, 1f, EventType.Repaint);
                    Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * -.5f, Quaternion.identity, 1f, EventType.Repaint);
                    Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * .5f, Quaternion.identity, 1f, EventType.Repaint);
                    Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * 1.5f, Quaternion.identity, 1f, EventType.Repaint);
                }
                else
                {
                    Vector3 tangent = wps[wp].position - wps[wp - 1].position;
                    Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * -1.5f, Quaternion.identity, 1f, EventType.Repaint);
                    Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * -.5f, Quaternion.identity, 1f, EventType.Repaint);
                    Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * .5f, Quaternion.identity, 1f, EventType.Repaint);
                    Handles.SphereHandleCap(HandleUtility.nearestControl, wps[wp].position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * 1.5f, Quaternion.identity, 1f, EventType.Repaint);
                }


            }
            Handles.DrawPolyLine(wps.Select(w => w.position).ToArray());
        }
    }

    private void VisualizeEntries()
    {
        var t = target as TrafSystem;
        foreach(var r in t.entries)
        {
            var points = r.GetPoints();
            Handles.color = Color.green;
            Handles.DrawPolyLine(points);
            Handles.color = Color.red;
            Vector3 position = Vector3.Lerp(points[points.Length / 2 - 1], points[points.Length/2], 0.5f);
            Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(points[points.Length / 2] - points[points.Length / 2 - 1]), 15f, EventType.Repaint);

            foreach (var p in r.GetPoints())
            {
                Handles.DrawWireCube(p, Vector3.one);
            }

            if (showIds)
            {
                //Vector3 pos = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(position);
                GUIStyle style = new GUIStyle(); style.normal.textColor = Color.white;
                Handles.Label(points[0], r.identifier + "_" + r.subIdentifier + "_ST", style);
                for (int i = 1; i < points.Length - 1; i++)
                { Handles.Label(points[i], r.identifier + "_" + r.subIdentifier + "_" + i, style); }
                Handles.Label(points[points.Length-1], r.identifier + "_" + r.subIdentifier + "_ED", style);
                //GUI.Label(new Rect(pos.x, pos.y, 200, 100) , r.identifier + "_" + r.subIdentifier);
            }
        }
    }

    private void VisualizeIntersections()
    {
        var t = target as TrafSystem;
        foreach (var r in t.intersections)
        {
            var points = r.GetPoints();
            Handles.color = Color.green;
            Handles.DrawPolyLine(points);
            Handles.color = Color.red;
            //Vector3 position = Vector3.Lerp(points[points.Length / 2 - 1], points[points.Length / 2], 0.5f);
            //Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(points[points.Length / 2] - points[points.Length / 2 - 1]), 15f, EventType.Repaint);

            foreach (var p in r.GetPoints())
            {
                Handles.DrawWireCube(p, Vector3.one);
            }

            if (showIds)
            {
                //Vector3 pos = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(position);
                GUIStyle style = new GUIStyle(); style.normal.textColor = Color.white;
                Handles.Label(points[0], r.identifier + "_" + r.subIdentifier + "_ST", style);
                for (int i = 1; i < points.Length - 1; i++)
                { Handles.Label(points[i], r.identifier + "_" + r.subIdentifier + "_" + i, style); }
                Handles.Label(points[points.Length - 1], r.identifier + "_" + r.subIdentifier + "_ED", style);
                //GUI.Label(new Rect(pos.x, pos.y, 200, 100) , r.identifier + "_" + r.subIdentifier);
            }
        }
    }

    private void VisualizeEntriesAndIntersections()
    {
        VisualizeEntries();
        VisualizeIntersections();
    }

    private int[] GetIndices(Vector3 p, Vector3 tr, Vector3 bl)
    {
        int[] indices = new int[2];
        indices[0] = Mathf.Clamp(Mathf.RoundToInt(((p.x - bl.x) / (tr.x - bl.x)) * 4096), 0, 4095);
        indices[1] = Mathf.Clamp(Mathf.RoundToInt(((p.z - bl.z) / (tr.z - bl.z)) * 4096), 0, 4095);

        return indices;
    }

    private Vector3 GetPosition(int[] index, Vector3 tr, Vector3 bl)
    {
        Vector3 p = new Vector3();
        float x = index[0];
        float y = index[1];
        p.x = bl.x + x / 4096f * (tr.x - bl.x);
        p.z = bl.z + y / 4096f * (tr.z - bl.z);
        return p;
    }

    private void GenerateInfractionsTexture()
    {

        Vector3 bl = new Vector3(721f, 10f, -22f);
        Vector3 tr = new Vector3(2419, 10f, 1774f);

        Texture2D workingTex = new Texture2D(4096, 4096, TextureFormat.RGB24, false);
         float[,] pixels = new float[4096, 4096];



        var traf = target as TrafSystem;

        foreach (var entry in traf.entries)
        {
            float checkDist = entry.GetTotalDistance() / 0.1f;
            for (float i = 0f; i <= 1f; i += 1 / checkDist)
            {
                var p = entry.GetInterpolatedPositionInfractions(i);

                Vector3 tangent;
                if (i < 1f)
                {
                    tangent = entry.GetInterpolatedPositionInfractions(i + 1 / checkDist).position - p.position;
                }
                else
                {
                    tangent = p.position - entry.GetInterpolatedPositionInfractions(i - 1 / checkDist).position;
                }


                float rot = Quaternion.LookRotation(tangent).eulerAngles.y;

                if (rot < 0)
                    rot += 360f;
                else if (rot > 360f)
                    rot -= 360f;

                int[] indices;

                for (float ff = -0.4f; ff < 0.4f; ff += 0.1f) {
                    indices = GetIndices(p.position + (Quaternion.Euler(0, 90f, 0) * tangent).normalized * laneWidth * ff, tr, bl);
                    pixels[indices[0], indices[1]] = (rot / 360f) * 0.8f + 0.2f;
                }
            }
        }


        for (int x = 0; x < 4096; x++)
        {
            for (int y = 0; y < 4096; y++)
            {
                workingTex.SetPixel(x, y, new Color(pixels[x, y],pixels[x, y], pixels[x, y]));
              //  if (Random.value > 0.98f)
              //      Debug.Log("x: " + x + " y: " + y + "val: " + pixels[x, y] * 0.8f + 0.2f);
            }
        }
        workingTex.Apply();
        System.IO.File.WriteAllBytes(Application.streamingAssetsPath + "/infractionsSF.png", workingTex.EncodeToPNG());
        Destroy(workingTex);

    }
    private void GenerateInfractionsTextureHighway()
    {

        Vector3 bl = new Vector3(721f, 10f, -22f);
        Vector3 tr = new Vector3(2419, 10f, 1774f);

        Texture2D workingTex = new Texture2D(4096, 4096, TextureFormat.RGB24, false);



        for (int x = 0; x < 4096; x++)
        {
            for (int y = 0; y < 4096; y++)
            {
                Vector3 p = GetPosition(new int[] { x, y }, tr, bl);
                RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 1000, Vector3.down, 1200f);
                if(hits.Length > 0 && hits.Any(h => h.collider.gameObject.layer == LayerMask.NameToLayer("BridgeRoad")))
                {
                    workingTex.SetPixel(x, y, new Color(0f, 0f, 0f));
                }
                else
                {
                    workingTex.SetPixel(x, y, new Color(1f, 1f, 1f));
                }

                //  if (Random.value > 0.98f)
                //      Debug.Log("x: " + x + " y: " + y + "val: " + pixels[x, y] * 0.8f + 0.2f);
            }
        }
        workingTex.Apply();
        System.IO.File.WriteAllBytes(Application.streamingAssetsPath + "/infractionsBridgeSF.png", workingTex.EncodeToPNG());
        Destroy(workingTex);

    }

}
