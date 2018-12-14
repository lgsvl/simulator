/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(TrafIntersection))]
public class TrafIntersectionEditor : Editor
{

    private const float lightBelongsThreshold = 15f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("GO!"))
        {
            var t = target as TrafIntersection;
            int sub = 0;

            foreach (var p in t.paths)
            {
                t.trafSystem.DeleteIntersectionIfExists(t.systemId, sub);
                p.subId = sub;
                var i = new TrafEntry();
                i.identifier = 1000 + t.systemId;
                i.subIdentifier = sub++;
                i.intersection = t;
                i.path = p;
                i.road = GetRoad(p);
                i.light = GetLight(p.light);
                t.trafSystem.intersections.Add(i);

                //p.start.renderer.enabled = false;
                //p.end.renderer.enabled = false;
            }

        }
        EditorGUILayout.EndVertical();
    }

    private static Dictionary<GameObject, TrafficLightContainer> lightTable;

    public static TrafficLightContainer GetLight(GameObject marker)
    {
        if (marker == null)
            return null;

        if (lightTable != null && lightTable.ContainsKey(marker))
            return lightTable[marker];

        float minDist = 9999f;
        var allLights = Object.FindObjectsOfType<TrafficLightContainer>();
        TrafficLightContainer closest = null;

        foreach (var l in allLights)
        {
            float dist = Vector3.Distance(l.transform.position, marker.transform.position);
            if (dist < minDist)
            {
                closest = l;
                minDist = dist;
            }
        }

        if (minDist < lightBelongsThreshold)
        {
            lightTable.Add(marker, closest);
            return closest;
        }
        else
            Debug.Log("Not found: " + marker.transform.root.name + " mindist = " + minDist);

        return null;

    }

    public static void GoAll(TrafSystem trafSystem)
    {
        var allIntersections = Object.FindObjectsOfType(typeof(TrafIntersection)) as TrafIntersection[];

        lightTable = new Dictionary<GameObject, TrafficLightContainer>();

        foreach (var t in allIntersections)
        {
            int sub = 0;

            foreach (var p in t.paths)
            {
                if (t.trafSystem == null)
                    t.trafSystem = trafSystem;

                t.trafSystem.DeleteIntersectionIfExists(t.systemId, sub);
                p.subId = sub;
                var i = new TrafEntry();
                i.identifier = 1000 + t.systemId;
                i.subIdentifier = sub++;
                i.intersection = t;
                i.path = p;
                i.road = GetRoad(p);
                i.waypoints = new List<Vector3>();
                i.spline = new List<SplineNode>();
                i.spline.Add(new SplineNode()
                {
                    position = p.start.transform.position,
                    tangent = p.start.transform.forward

                });
                i.spline.Add(new SplineNode()
                {
                    position = p.end.transform.position,
                    tangent = p.end.transform.forward

                });
                foreach (var v in i.road.waypoints)
                {
                    i.waypoints.Add(v.position);
                }
                i.light = GetLight(p.light);
                t.trafSystem.intersections.Add(i);
                //p.start.renderer.enabled = false;
                //p.end.renderer.enabled = false;
            }
        }
        lightTable.Clear();
    }

    void OnSceneGUI()
    {
        var t = target as TrafIntersection;
        foreach (var p in t.paths)
        {
            Handles.color = Color.yellow;
            Handles.DrawPolyLine(p.start.transform.position,
                HermiteMath.HermiteVal(p.start.transform.position, p.end.transform.position, p.start.transform.forward, p.end.transform.forward, 0.2f),
                HermiteMath.HermiteVal(p.start.transform.position, p.end.transform.position, p.start.transform.forward, p.end.transform.forward, 0.4f),
                HermiteMath.HermiteVal(p.start.transform.position, p.end.transform.position, p.start.transform.forward, p.end.transform.forward, 0.6f),
                HermiteMath.HermiteVal(p.start.transform.position, p.end.transform.position, p.start.transform.forward, p.end.transform.forward, 0.8f),
                p.end.transform.position);
        }
    }

    private static TrafRoad GetRoad(TrafIntersectionPath p)
    {
        var r = ScriptableObject.CreateInstance(typeof(TrafRoad)) as TrafRoad;
        r.waypoints = new List<TrafRoadPoint>();
        r.waypoints.Add(new TrafRoadPoint() { position = HermiteMath.HermiteVal(p.start.transform.position, p.end.transform.position, p.start.transform.forward, p.end.transform.forward, 0f) });
        r.waypoints.Add(new TrafRoadPoint() { position = HermiteMath.HermiteVal(p.start.transform.position, p.end.transform.position, p.start.transform.forward, p.end.transform.forward, 0.33f) });
        r.waypoints.Add(new TrafRoadPoint() { position = HermiteMath.HermiteVal(p.start.transform.position, p.end.transform.position, p.start.transform.forward, p.end.transform.forward, 0.66f) });
        r.waypoints.Add(new TrafRoadPoint() { position = HermiteMath.HermiteVal(p.start.transform.position, p.end.transform.position, p.start.transform.forward, p.end.transform.forward, 1f) });
        return r;
    }
}
