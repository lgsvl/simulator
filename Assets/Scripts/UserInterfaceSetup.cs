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
    public Toggle SensorEffectsToggle;
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

    public GameObject[] obstacleVehicles;
    public float obstacleDistance = 20f;
    private bool isInObstacleMode = false;
    private GameObject currentObstacle;
    private VehicleController vehicleController;

    protected virtual void Start()
    {
        vehicleController = FindObjectOfType<VehicleController>();
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            exitScreen.SetActive(!exitScreen.activeInHierarchy);
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            // save pos/rot
            SaveAutoPositionRotation();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            // load saved pos and rot and apply to controller transform
            LoadAutoPositionRotation();
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            // move car in front of user vehicle
            ToggleNPCObstacleToUser();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            var ui = GetComponent<RectTransform>();
            var camView = CameraPreview.GetComponent<RectTransform>();
            var extraView = ColorSegmentPreview.GetComponent<RectTransform>();

            int w, h;

            if (camView.offsetMax.y == 480)
            {
                // make it big
                camView.offsetMax = new Vector2(0, ui.sizeDelta.y * 2);
                camView.offsetMin = new Vector2(-ui.sizeDelta.x, 0);

                extraView.offsetMax = new Vector2(-ui.sizeDelta.x/2, ui.sizeDelta.y);
                extraView.offsetMin = new Vector2(-ui.sizeDelta.x, 0);

                w = (int)camView.sizeDelta.x;
                h = (int)camView.sizeDelta.y;
            }
            else
            {
                // revert
                camView.offsetMax = new Vector2(0.0f, 480.0f);
                camView.offsetMin = new Vector2(-640.0f, 0.0f);

                extraView.offsetMax = new Vector2(-320.0f, 240.0f);
                extraView.offsetMin = new Vector2(-640.0f, 0.0f);

                w = 640;
                h = 480;
            }

            CameraPreview.renderTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            {
                var vs = CameraPreview?.renderCamera?.GetComponent<VideoToROS>();
                if (vs != null)
                {
                    vs.SwitchResolution(w, h);
                }
            }

            ColorSegmentPreview.renderTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf);
            {
                var rendCam = ColorSegmentPreview?.renderCamera;
                if (rendCam != null)
                {
                    rendCam.targetTexture = ColorSegmentPreview.renderTexture;
                }
            }
        }
    }

    #region save pos/rot
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
        Vector3 tempPos = StringToVector3(PlayerPrefs.GetString("AUTO_POSITION", Vector3.zero.ToString()));
        Quaternion tempRot = Quaternion.Euler(StringToVector3(PlayerPrefs.GetString("AUTO_ROTATION", Vector3.zero.ToString())));
        PositionReset.RobotController.ResetSavedPosition(tempPos, tempRot);
    }
    #endregion

    #region obstacle
    public void ToggleNPCObstacleToUser()
    {
        if (vehicleController == null)
        {
            Debug.Log("Error returning VehicleController!");
            return;
        }

        // static obstacle NPC
        if (obstacleVehicles.Length == 0)
        {
            Debug.Log("No obstacle vehicles in pool!");
            return;
        }

        isInObstacleMode = !isInObstacleMode;
        if (isInObstacleMode)
        {
            Vector3 spawnPos = vehicleController.transform.position + vehicleController.transform.forward * obstacleDistance;
            currentObstacle = Instantiate(obstacleVehicles[(int)Random.Range(0, obstacleVehicles.Length)], spawnPos, vehicleController.carCenter.rotation);
        }
        else
        {
            if (currentObstacle != null)
                Destroy(currentObstacle);
        }


        // dynamic obstacle NPC wip
        //CarAIController tempController = TrafPerformanceManager.Instance.GetRandomAICarGO();
        //if (tempController == null)
        //{
        //    Debug.Log("Error returning CarAIController!");
        //    return;
        //}
        //isInObstacleMode = !isInObstacleMode;
        //Vector3 spawnPos = vehicleController.carCenter.position + vehicleController.carCenter.forward * obstacleDistance;
        //tempController.aiMotor.Init(tempController.aiMotor.currentIndex, tempController.aiMotor.currentEntry);
        //tempController.Init();
        //tempController.aiMotor.rb.position = spawnPos;
        //tempController.aiMotor.rb.rotation = vehicleController.carCenter.rotation;
    }
    #endregion

    #region utilities
    protected Vector3 StringToVector3(string str)
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
    #endregion
}
