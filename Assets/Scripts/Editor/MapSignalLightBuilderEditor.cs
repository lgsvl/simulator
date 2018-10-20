/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapSignalLightBuilder)), CanEditMultipleObjects]
public class MapSignalLightBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    protected virtual void OnSceneGUI()
    {
        //Placeholder
    }
}
