/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
[CanEditMultipleObjects]
[CustomEditor(typeof(AutomotivePositionManager))]
public class AutomotivePositionEditor : Editor
{

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		AutomotivePositionManager myTarget = (AutomotivePositionManager)target;

		if (GUILayout.Button("Save Location"))
		{
			myTarget.SetAutomotiveLocation();
		}

		if (GUILayout.Button("Load Location"))
		{
			myTarget.GetAutomotiveLocation();
		}

		if (Event.current.Equals(Event.KeyboardEvent("F5")))
			Debug.Log("F5");
		if (Event.current.Equals(Event.KeyboardEvent("F9")))
			Debug.Log("F9");

	}

}
