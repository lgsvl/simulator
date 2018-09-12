/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceSetup : MonoBehaviour
{
    public RectTransform MainPanel;
    public Text BridgeStatus;
    public InputField WheelScale;
    public InputField CameraFramerate;
    public Scrollbar CameraSaturation;
    public Toggle MainCameraToggle;
    public Toggle SideCameraToggle;
    public Toggle TelephotoCamera;
    public Toggle ColorSegmentCamera;
    public Toggle HDToggle;
    public Toggle Imu;
    public Toggle Lidar;
    public Toggle Radar;
    public Toggle Gps;
    public Toggle TrafficToggle;
    public RenderTextureDisplayer CameraPreview;
    public RenderTextureDisplayer ColorSegmentPreview;
    public DuckiebotPositionResetter PositionReset;
    public Toggle HighQualityRendering;
    public GameObject exitScreen;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            exitScreen.SetActive(!exitScreen.activeInHierarchy);
        }

		if (Input.GetKeyDown(KeyCode.F5))
		{
			Debug.Log("Save pos and rot");
			SaveAutoPositionRotation();
		}

		if (Input.GetKeyDown(KeyCode.F9))
		{
			Debug.Log("Load pos and rot");
			LoadAutoPositionRotation();
		}
    }

	public void SaveAutoPositionRotation()
	{
		if (PositionReset.RobotController == null)
		{
			Debug.LogError("Missing PositionReset RobotController!");
			return;
		}

		PlayerPrefs.SetString("AUTO_POSITION", PositionReset.RobotController.transform.position.ToString());
		PlayerPrefs.SetString("AUTO_ROTATION", PositionReset.RobotController.transform.rotation.eulerAngles.ToString());
	}

	public void LoadAutoPositionRotation()
	{
		if (PositionReset.RobotController == null)
		{
			Debug.LogError("Missing PositionReset RobotController!");
			return;
		}
		// calls method passing pos and rot saved instead of init position and rotation. Init pos and rot are still used on reset button in UI
		PositionReset.RobotController.ResetSavedPosition(StringToVector3(PlayerPrefs.GetString("AUTO_POSITION", Vector3.zero.ToString())), Quaternion.Euler(StringToVector3(PlayerPrefs.GetString("AUTO_ROTATION", Vector3.zero.ToString()))));
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
