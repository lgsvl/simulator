/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    #region Singelton
    private static VehicleManager _instance = null;
    public static VehicleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<VehicleManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>VehicleManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public List<GameObject> testVehicles = new List<GameObject>();
    public GameObject currentTestVehicle { get; set; }

    public List<GameObject> npcVehicles = new List<GameObject>();
    public List<GameObject> currentNPCs = new List<GameObject>();

    public Transform testVehicleHolder;
    public Transform npcHolder;

    public List<OpenScenarioData.StoryboardInit> initData = new List<OpenScenarioData.StoryboardInit>();
    public OpenScenarioData.StoryboardInit initEgoData = new OpenScenarioData.StoryboardInit();
    public List<OpenScenarioData.StoryboardInit> initNPCData = new List<OpenScenarioData.StoryboardInit>();
    private VehicleController currentVehicleController;

    // testing
    private float targetSpeed = 0f;
    #endregion

    #region mono
    void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Missive.AddListener<TestStateMissive>(OnTestStateChange);
    }

    private void OnDisable()
    {
        Missive.RemoveListener<TestStateMissive>(OnTestStateChange);
    }

    void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region methods
    private void OnTestStateChange(TestStateMissive missive)
    {
        CancelInvoke();
        switch (missive.state)
        {
            case TestState.None:
                DeSpawnNPCVehicles();
                DespawnTestVehicle();
                break;
            case TestState.Init:
                SpawnVehicles();
                break;
            case TestState.Warmup:
                //
                break;
            case TestState.Running:
                SetTestVehicleSpeed();
                SetNPCVehicleSpeed();
                break;
            default:
                break;
        }
    }

    private void SpawnTestVehicle(Vector3 pos)
    {
        if (UIManager.Instance.vehicleSelectDropdown == null) return;

        if (currentTestVehicle != null)
            Destroy(currentTestVehicle);

        currentTestVehicle = Instantiate(testVehicles[UIManager.Instance.vehicleSelectDropdown.value], testVehicleHolder);
        currentTestVehicle.transform.position = pos;
        currentVehicleController = currentTestVehicle.GetComponent<VehicleController>();
    }

    public void SpawnVehicles()
    {
        for (int i = 0; i < currentNPCs.Count; i++)
            Destroy(currentNPCs[i]);
        currentNPCs.Clear();
        initEgoData = new OpenScenarioData.StoryboardInit();
        initNPCData.Clear();

        // get data
        initData = DataManager.Instance.GetInitVehicleData();
        for (int i = 0; i < initData.Count; i++)
        {
            if (initData[i].name == "Ego")
                initEgoData = initData[i];
            else
                initNPCData.Add(initData[i]);
        }

        float tempX, tempY, tempZ;

        // test vehicle
        if (initEgoData.name == "Ego")
        {
            if (float.TryParse(initEgoData.positionX, out tempX) && float.TryParse(initEgoData.positionY, out tempZ) && float.TryParse(initEgoData.positionZ, out tempY))
            {
                SpawnTestVehicle(new Vector3(tempX, tempY, tempZ));
                float tempSpeed;
                if (float.TryParse(initEgoData.speed, out tempSpeed))
                    targetSpeed = tempSpeed;
            }
        }

        // npcs
        for (int i = 0; i < initNPCData.Count; i++)
        {
            if (float.TryParse(initNPCData[i].positionX, out tempX) && float.TryParse(initNPCData[i].positionY, out tempZ) && float.TryParse(initNPCData[i].positionZ, out tempY))
            {
                GameObject tempVehicle = Instantiate(npcVehicles[0]); // lookup vehicle name TODO
                tempVehicle.transform.SetParent(npcHolder);
                tempVehicle.transform.position = new Vector3(tempX, tempY, tempZ);
                tempVehicle.GetComponent<AIVehicleController>().isScenario = true;
                currentNPCs.Add(tempVehicle);
            }    
            else
                Debug.Log("No pos data");
        }

        // just spawn a test vehicle
        if (currentTestVehicle == null)
            SpawnTestVehicle(Vector3.zero);
    }

    private void DespawnTestVehicle()
    {
        if (currentTestVehicle != null)
            Destroy(currentTestVehicle);
    }

    private void DeSpawnNPCVehicles()
    {
        for (int i = 0; i < currentNPCs.Count; i++)
            Destroy(currentNPCs[i]);
        currentNPCs.Clear();
        initEgoData = new OpenScenarioData.StoryboardInit();
        initNPCData.Clear();
    }

    private void SetTestVehicleSpeed()
    {
        currentVehicleController.ToggleCruiseMode();
        currentVehicleController.cruiseTargetSpeed = targetSpeed;
    }

    private void SetNPCVehicleSpeed()
    {
        for (int i = 0; i < currentNPCs.Count; i++)
        {   
            AIVehicleController tempController = currentNPCs[i].GetComponent<AIVehicleController>();
            float tempSpeed = 0f;
            if (float.TryParse(initNPCData[i].speed, out tempSpeed))
            {
                if (tempController != null)
                {
                    tempController.ToggleCruiseMode();
                    tempController.cruiseTargetSpeed = tempSpeed;
                }
            }
        }
    }
    #endregion
}
