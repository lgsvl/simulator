/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using Autoware;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapStopLineSegmentBuilder), true), CanEditMultipleObjects]
public class MapStopLineSegmentBuilderEditor : MapSegmentBuilderEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    protected override void OnSceneGUI()
    {
        base.OnSceneGUI();        
    }
}