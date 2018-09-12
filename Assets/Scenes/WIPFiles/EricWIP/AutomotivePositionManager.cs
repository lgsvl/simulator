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

public class AutomotivePositionManager : MonoBehaviour
{
	#region singleton
	private static AutomotivePositionManager _instance;

	public static AutomotivePositionManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = GameObject.FindObjectOfType<AutomotivePositionManager>();
				if (_instance == null)
					Debug.LogError("AutomotivePositionManager not found!");
			}

			return _instance;
		}
	}
	#endregion

	public GameObject automotivePrefab;

	private Vector3 tempPos;
	private Quaternion tempRot;

	// Maybe check if auto is null and look?
	//static AutomotivePositionManager()
	//{
	//	Debug.Log("Constructor");
	//}

	public void SetAutomotiveLocation()
	{
		if (automotivePrefab == null)
		{
			Debug.LogError("Please set auto gameobject in manager");
			return;
		}

		Debug.Log(automotivePrefab.transform.position);
		Debug.Log(automotivePrefab.transform.rotation.eulerAngles);

		EditorPrefs.SetString("EDITOR_AUTO_POSITION", automotivePrefab.transform.position.ToString());
		EditorPrefs.SetString("EDITOR_AUTO_ROTATION", automotivePrefab.transform.rotation.eulerAngles.ToString());
	}

	public void GetAutomotiveLocation()
	{
		if (automotivePrefab == null)
		{
			Debug.LogError("Please set auto gameobject in manager");
			return;
		}


		Debug.Log(StringToVector3(automotivePrefab.transform.position.ToString()));
		Debug.Log(StringToVector3(automotivePrefab.transform.rotation.eulerAngles.ToString()));

		automotivePrefab.transform.position = StringToVector3(EditorPrefs.GetString("EDITOR_AUTO_POSITION", Vector3.zero.ToString()));
		automotivePrefab.transform.rotation = Quaternion.Euler(StringToVector3(EditorPrefs.GetString("EDITOR_AUTO_ROTATION", Vector3.zero.ToString())));
	}

	private Vector3 StringToVector3(string str)
	{
		Vector3 tempVector3 = Vector3.zero;

		if (str.StartsWith("(") && str.EndsWith(")"))
			str = str.Substring(1, str.Length - 2);

		// split the items
		string[] sArray = str.Split(',');

		// store as a Vector3
		if (!string.IsNullOrEmpty(str))
			tempVector3 = new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
	
		return tempVector3;
	}
}
