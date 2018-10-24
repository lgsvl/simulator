/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class MapToolUtilEditorWindow : EditorWindow
{
    [MenuItem("Window/Map Tool Panel")]
    public static void MapToolPanel()
    {
        MapToolUtilEditorWindow window = (MapToolUtilEditorWindow)EditorWindow.GetWindow(typeof(MapToolUtilEditorWindow));
        window.Show();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Show/Hide Map"))
        {
            ToggleMap();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Hide All MapSegment Handles"))
        {
            HideAllMapSegmentHandles();
        }
        if (GUILayout.Button("Double Selected Segment's Subsegment Resolution"))
        {
            DoubleSelectionSubsegmentResolution();
        }

        if (GUILayout.Button("Half Selected Segment's Subsegment Resolution"))
        {
            HalfSelectionSubsegmentResolution();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Link Lane from Left"))
        {
            LinkFromLeft();
        }
        if (GUILayout.Button("Link Lane from Right"))
        {
            LinkFromRight();
        }
        if (GUILayout.Button("Link Reverse Lanes"))
        {
            LinkLeftReverse();
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

    static void ToggleMap()
    {
        Map.MapTool.showMap = !Map.MapTool.showMap;
    }

    static void HideAllMapSegmentHandles()
    {
        var mapSegmentBuilders = FindObjectsOfType<MapSegmentBuilder>();
        foreach (var mapSegBldr in mapSegmentBuilders)
        {
            Undo.RegisterFullObjectHierarchyUndo(mapSegBldr, "builder");
            mapSegBldr.displayHandles = false;
        }
    }
    
    static void LinkFromLeft()
    {
        var Ts = Selection.transforms;

        Reorder(ref Ts);

        Ts = Ts.Reverse().ToArray();

        for (int i = 0; i < Ts.Length - 1; i++)
        {
            var A = Ts[i];
            var B = Ts[i + 1];
            Undo.RegisterFullObjectHierarchyUndo(A, "A");
            Undo.RegisterFullObjectHierarchyUndo(B, "B");
            A.GetComponent<MapLaneSegmentBuilder>().leftNeighborForward = B.GetComponent<MapLaneSegmentBuilder>();
            B.GetComponent<MapLaneSegmentBuilder>().rightNeighborForward = A.GetComponent<MapLaneSegmentBuilder>();
        }
    }
    
    static void LinkFromRight()
    {
        var Ts = Selection.transforms;

        Reorder(ref Ts);

        for (int i = 0; i < Ts.Length - 1; i++)
        {
            var A = Ts[i];
            var B = Ts[i + 1];
            Undo.RegisterFullObjectHierarchyUndo(A, "A");
            Undo.RegisterFullObjectHierarchyUndo(B, "B");
            A.GetComponent<MapLaneSegmentBuilder>().leftNeighborForward = B.GetComponent<MapLaneSegmentBuilder>();
            B.GetComponent<MapLaneSegmentBuilder>().rightNeighborForward = A.GetComponent<MapLaneSegmentBuilder>();
        }
    }
    
    static void LinkLeftReverse()
    {
        Undo.RecordObjects(Selection.transforms, nameof(LinkFromRight));

        var Ts = Selection.transforms;

        if (Ts.Length != 2)
        {
            return;
        }

        Reorder(ref Ts);

        for (int i = 0; i < Ts.Length - 1; i++)
        {
            var A = Ts[i];
            var B = Ts[i + 1];
            Undo.RegisterFullObjectHierarchyUndo(A, "A");
            Undo.RegisterFullObjectHierarchyUndo(B, "B");
            A.GetComponent<MapLaneSegmentBuilder>().leftNeighborReverse = B.GetComponent<MapLaneSegmentBuilder>();
            B.GetComponent<MapLaneSegmentBuilder>().leftNeighborReverse = A.GetComponent<MapLaneSegmentBuilder>();
        }
    }

    static void Reorder(ref Transform[] Ts)
    {
        var list = new List<Transform>(Ts);
        var retList = new List<Transform>();
        foreach (Transform child in (list[0].parent))
        {
            if (list.Contains(child))
            {
                retList.Add(child);
            }
        }
        Ts = retList.ToArray();
    }
}
