/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapParkingSpaceBuilder)), CanEditMultipleObjects]
public class MapParkingSpaceBuilderEditor : MapSegmentBuilderEditor
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