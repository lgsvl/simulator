/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEngine;
using System.Collections;
using UnityEditor;

// Create a 180 degrees wire arc with a ScaleValueHandle attached to the disc
// lets you visualize some info of the transform

[CustomEditor(typeof(HandleComponent))]
class LabelHandle : Editor
{
    void OnSceneGUI()
    {
        HandleComponent handleComponent = (HandleComponent)target;

        if (handleComponent == null) return;
        if (handleComponent.thisCamera == null) return;

        Handles.Label(handleComponent.transform.position + Vector3.up * 2, handleComponent.gameObject.name + " FOV \nV: " + handleComponent.thisCamera.fieldOfView.ToString());
    }
}