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

public class HDMapToolAdditionEditorWindow : EditorWindow
{
    [MenuItem("Window/HD Map Tool/Double Segment Resolution")]
    static void DoubleSegmentResolution()
    {
        var Ts = Selection.transforms;

        var builders = Ts.Select(x => x.GetComponent<MapSegmentBuilder>()).ToList();
        foreach (var builder in builders)
        {
            Undo.RegisterFullObjectHierarchyUndo(builder, "builder");
            Map.MapTool.DoubleSegmentResolution(builder.segment);
        }
    }

    [MenuItem("Window/HD Map Tool/Link Lane from Left")]
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

    [MenuItem("Window/HD Map Tool/Link Lane from Right")]
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

    [MenuItem("Window/HD Map Tool/Link Reverse Lanes")]
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
