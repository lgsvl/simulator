/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SimulatorManager : MonoBehaviour
{
    #region Singleton
    private static SimulatorManager _instance = null;
    public static SimulatorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<SimulatorManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>SimulatorManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public GameObject[] managers;

    //singleros
    public string Address = "localhost";
    public int Port = RosBridgeConnector.DefaultPort;
    public RosBridgeConnector Connector { get; private set; }
    public UserInterfaceSetup uiPrefab;
    public AgentSetup agentSetup;
    private UserInterfaceSetup userInterface;
    private Text bridgeStatus;
    private bool isQuickStart = false;

    private List<GameObject> activeFoci = new List<GameObject>();
    private GameObject currentActiveFocus = null;

    public KeyCode exitKey = KeyCode.Escape;
    public KeyCode toggleUIKey = KeyCode.Space;
    public KeyCode saveRobotPos = KeyCode.F5;
    public KeyCode loadRobotPos = KeyCode.F9;
    public KeyCode spawnObstacle = KeyCode.F10;
    public KeyCode demo = KeyCode.F11;

    //public KeyCode exitKey = KeyCode.Escape; // d depth camera
    //public KeyCode exitKey = KeyCode.Escape; // h k traffic
    //public KeyCode exitKey = KeyCode.Escape; // f1 help
    //public KeyCode exitKey = KeyCode.Escape; // f12 tweaks
    //public KeyCode exitKey = KeyCode.Escape; // f2 camerafollow ???
    //public KeyCode exitKey = KeyCode.Escape; // vehicle inputs
    //public KeyCode exitKey = KeyCode.Escape; // left shift
    //public KeyCode exitKey = KeyCode.Escape; // m ros to video???
    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }

        //singleros
        if (FindObjectOfType<ROSAgentManager>() != null)
        {
            var agents = FindObjectsOfType<AgentSetup>();
            foreach (var item in agents)
            {
                item.RemoveTweakables();
                Destroy(item.gameObject);
            }
            isQuickStart = false;
        }
        else
        {
            isQuickStart = true;
            if (FindObjectOfType<AnalyticsManager>() == null)
                new GameObject("Analytics").AddComponent<AnalyticsManager>();
        }
    }

    private void Start()
    {
        activeFoci.Clear();
        SpawnManagers();
        
        //singleros
        if (isQuickStart)
        {
            List<VehicleController> tempL = FindObjectsOfType<VehicleController>().ToList();
            foreach (var item in tempL)
            {
                activeFoci.Add(item.gameObject);
            }
            SetCurrentActiveFocus(0);

            userInterface = Instantiate(uiPrefab);
            bridgeStatus = userInterface.BridgeStatus;
            Connector = new RosBridgeConnector();
            Connector.BridgeStatus = bridgeStatus;
            if (agentSetup == null)
            {
                agentSetup = GetCurrentActiveFocus().GetComponent<AgentSetup>();
                agentSetup.Setup(userInterface, Connector, null);
            }

            string overrideAddress = System.Environment.GetEnvironmentVariable("ROS_BRIDGE_HOST");
            if (overrideAddress != null)
            {
                Address = overrideAddress;
            }

            Ros.Bridge.canConnect = true;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(exitKey))
        {

        }
        if (Input.GetKeyDown(toggleUIKey))
        {

        }
        if (Input.GetKeyDown(saveRobotPos))
        {

        }
        if (Input.GetKeyDown(loadRobotPos))
        {

        }
        if (Input.GetKeyDown(spawnObstacle))
        {

        }
        if (Input.GetKeyDown(demo))
        {

        }

        //singleros
        if (isQuickStart)
        {
            if (Address != Connector.Address || Port != Connector.Port || agentSetup != Connector.agentType)
                Connector.Disconnect();
            
            Connector.Address = Address;
            Connector.Port = Port;
            Connector.agentType = agentSetup;
            Connector.Update();
        }

        CheckStateErrors();
    }

    private void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region active vehicles
    public void AddActiveFocus(GameObject go)
    {
        if (go == null) return;
        activeFoci.Add(go);
    }

    public void RemoveActiveFocus(GameObject go)
    {
        if (go == null) return;
        activeFoci.Remove(go);
    }

    public void SetCurrentActiveFocus(GameObject go)
    {
        if (go == null) return;
        currentActiveFocus = go;
        currentActiveFocus?.GetComponent<VehicleController>()?.SetDashUIState();
    }

    public void SetCurrentActiveFocus(int index)
    {
        if (activeFoci.Count == 0) return;

        currentActiveFocus = activeFoci[index];
        currentActiveFocus?.GetComponent<VehicleController>()?.SetDashUIState();
    }

    public bool GetCurrentActiveFocus(GameObject go)
    {
        return go == currentActiveFocus;
    }

    public float GetDistanceToActiveFocus(Vector3 pos)
    {
        return Vector3.Distance(currentActiveFocus.transform.position, pos);
    }

    public GameObject GetCurrentActiveFocus()
    {
        return currentActiveFocus;
    }

    public bool IsFoci()
    {
        return activeFoci != null || activeFoci.Count != 0;
    }
    #endregion

    #region managers
    public void SpawnManagers()
    {
        foreach (var item in managers)
        {
            Instantiate(item);
        }
    }
    #endregion

    // TODO
    #region save pos/rot
    //public void SaveAutoPositionRotation()
    //{
    //    if (PositionReset.RobotController == null)
    //    {
    //        Debug.LogError("Missing PositionReset RobotController!");
    //        return;
    //    }

    //    PlayerPrefs.SetString("AUTO_POSITION", PositionReset.RobotController.transform.position.ToString());
    //    PlayerPrefs.SetString("AUTO_ROTATION", PositionReset.RobotController.transform.rotation.eulerAngles.ToString());
    //}

    //public void LoadAutoPositionRotation()
    //{
    //    if (PositionReset.RobotController == null)
    //    {
    //        Debug.LogError("Missing PositionReset RobotController!");
    //        return;
    //    }
    //    // calls method passing pos and rot saved instead of init position and rotation. Init pos and rot are still used on reset button in UI
    //    Vector3 tempPos = StringToVector3(PlayerPrefs.GetString("AUTO_POSITION", Vector3.zero.ToString()));
    //    Quaternion tempRot = Quaternion.Euler(StringToVector3(PlayerPrefs.GetString("AUTO_ROTATION", Vector3.zero.ToString())));
    //    PositionReset.RobotController.ResetSavedPosition(tempPos, tempRot);
    //}
    #endregion

    #region obstacle
    //public void ToggleNPCObstacleToUser()
    //{
    //    if (vehicleController == null)
    //    {
    //        Debug.Log("Error returning VehicleController!");
    //        return;
    //    }

    //    // static obstacle NPC
    //    if (obstacleVehicles.Length == 0)
    //    {
    //        Debug.Log("No obstacle vehicles in pool!");
    //        return;
    //    }

    //    isInObstacleMode = !isInObstacleMode;
    //    if (isInObstacleMode)
    //    {
    //        Vector3 spawnPos = vehicleController.transform.position + vehicleController.transform.forward * obstacleDistance;
    //        currentObstacle = Instantiate(obstacleVehicles[(int)Random.Range(0, obstacleVehicles.Length)], spawnPos, vehicleController.carCenter.rotation);
    //    }
    //    else
    //    {
    //        if (currentObstacle != null)
    //            Destroy(currentObstacle);
    //    }


    //    // dynamic obstacle NPC wip
    //    //CarAIController tempController = TrafPerformanceManager.Instance.GetRandomAICarGO();
    //    //if (tempController == null)
    //    //{
    //    //    Debug.Log("Error returning CarAIController!");
    //    //    return;
    //    //}
    //    //isInObstacleMode = !isInObstacleMode;
    //    //Vector3 spawnPos = vehicleController.carCenter.position + vehicleController.carCenter.forward * obstacleDistance;
    //    //tempController.aiMotor.Init(tempController.aiMotor.currentIndex, tempController.aiMotor.currentEntry);
    //    //tempController.Init();
    //    //tempController.aiMotor.rb.position = spawnPos;
    //    //tempController.aiMotor.rb.rotation = vehicleController.carCenter.rotation;
    //}
    #endregion

    #region utilities
    //protected Vector3 StringToVector3(string str)
    //{
    //    Vector3 tempVector3 = Vector3.zero;

    //    if (str.StartsWith("(") && str.EndsWith(")"))
    //        str = str.Substring(1, str.Length - 2);

    //    // split the items
    //    string[] sArray = str.Split(',');

    //    // store as a Vector3
    //    if (!string.IsNullOrEmpty(str))
    //        tempVector3 = new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));

    //    return tempVector3;
    //}
    #endregion

    private void CheckStateErrors()
    {
        //errorContent.text = ""; //clear

        //var steerwheels = FindObjectsOfType<SteeringWheelInputController>();
        //foreach (var steerwheel in steerwheels)
        //{
        //    if (steerwheel.stateFail != "")
        //    {
        //        errorContent.text += $"{steerwheel.stateFail}\n";
        //    }
        //}
    }

    //public static void ChangeFocusUI(RosBridgeConnector connector, RosRobots robots)
    //{
    //    for (int k = 0; k < robots.Robots.Count; k++)
    //    {
    //        var robotConnector = robots.Robots[k];
    //        bool isFocus = robotConnector == connector;
    //        robotConnector.UiObject.enabled = isFocus;
    //        var b = robotConnector.UiButton.GetComponent<Button>();
    //        var c = b.colors;
    //        c.normalColor = isFocus ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f);
    //        b.colors = c;
    //        var robotSetup = robotConnector.Robot.GetComponent<RobotSetup>();
    //        robotSetup.FollowCamera.gameObject.SetActive(isFocus);
    //        robotSetup.FollowCamera.enabled = isFocus;
    //        var inputControllers = robotConnector.Robot.GetComponentsInChildren<IInputController>().ToList();
    //        if (isFocus)
    //        {
    //            FocusUI = robotSetup.UI;
    //            inputControllers.ForEach(i => i.Enable());

    //            // TODO move to gameobject based
    //            SimulatorManager.Instance?.SetCurrentActiveRobot(robotSetup.gameObject);
    //        }
    //        else
    //        {
    //            inputControllers.ForEach(i => i.Disable());
    //        }
    //    }

    //    VehicleList.Instances?.ForEach(x => x.ToggleDisplay(FocusUI.MainPanel.gameObject.activeSelf)); //hack
    //}
}
