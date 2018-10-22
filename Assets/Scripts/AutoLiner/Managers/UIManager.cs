/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using System.IO;

public class UIManager : MonoBehaviour
{
    #region Singelton
    private static UIManager _instance = null;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<UIManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>UIManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public GameObject mainMenuPanelGO;
    public GameObject helpPanelGO;
    public Text stateText;
    public GameObject sceneCamera;

    public Dropdown vehicleSelectDropdown;
    public Dropdown loadFileDropdown;
    private List<string> loadFileNames = new List<string>();
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

    private void Start()
    {
        PopulateTestVehicleDropDown();
        PopulateLoadFileDropdown();
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
    public void ToggleMainMenu()
    {
        if (mainMenuPanelGO == null) return;

        mainMenuPanelGO.SetActive(!mainMenuPanelGO.activeInHierarchy);
    }

    public void ToggleMainMenu(bool state)
    {
        if (mainMenuPanelGO == null) return;

        mainMenuPanelGO.SetActive(state);
    }

    public void ToggleHelp()
    {
        if (helpPanelGO == null) return;

        helpPanelGO.SetActive(!helpPanelGO.activeInHierarchy);
    }

    public void ToggleHelp(bool state)
    {
        if (helpPanelGO == null) return;

        helpPanelGO.SetActive(state);
    }

    public void ToggleSceneCamera(bool state)
    {
        if (sceneCamera == null) return;

        sceneCamera.SetActive(state);
    }

    private void PopulateTestVehicleDropDown()
    {
        if (VehicleManager.Instance.testVehicles.Count == 0) return;

        List<string> dropOptions = new List<string>();

        for (int i = 0; i < VehicleManager.Instance.testVehicles.Count; i++)
        {
            dropOptions.Add(VehicleManager.Instance.testVehicles[i].name);
        }
        vehicleSelectDropdown.AddOptions(dropOptions);
    }

    #region missive
    private void OnTestStateChange(TestStateMissive missive)
    {
        switch (missive.state)
        {
            case TestState.None:
                ToggleSceneCamera(true);
                ToggleMainMenu(true);
                ToggleHelp(true);
                break;
            case TestState.Init:
                ToggleSceneCamera(false);
                break;
            case TestState.Warmup:
                break;
            case TestState.Running:
                break;
            default:
                break;
        }
        SetStateText(missive.state.ToString());
    }
    #endregion

    private void SetStateText(string state)
    {
        stateText.text = state;
    }

    public void OnTestVehicleChange()
    {
        //
    }

    public void PopulateLoadFileDropdown()
    {
        List<string> dropOptions = new List<string>();
        var info = new DirectoryInfo("Assets/Resources/XMLFiles");
        // TODO build Application.dataPath + path
        var fileInfo = info.GetFiles();
        for (int i = 0; i < fileInfo.Length; i++)
        {
            if (fileInfo[i].Name.Contains(".meta")) continue;
            if (fileInfo[i].Name.Contains(".xosc"))
                loadFileNames.Add(fileInfo[i].Name);
        }

        for (int i = 0; i < loadFileNames.Count; i++)
        {
            dropOptions.Add(loadFileNames[i]);
        }
        dropOptions.Add("Select File"); // default
        loadFileDropdown.AddOptions(dropOptions);
        loadFileDropdown.value = dropOptions.Count - 1;
    }

    public void LoadTestData()
    {
        if (loadFileDropdown.value == loadFileNames.Count)
        {
            DataManager.Instance.ClearData();
            return;
        }

        DataManager.Instance.LoadData(loadFileNames[loadFileDropdown.value]);
    }

    public void InitTest()
    {
        ActiveSceneManager.Instance.SetTestState(TestState.Init);
    }

    public void WarmupTest()
    {
        //ActiveSceneManager.Instance.SetTestState(TestState.Warmup);
    }

    public void RunTest()
    {
        ActiveSceneManager.Instance.SetTestState(TestState.Running);
    }

    public void StopTest()
    {
        ActiveSceneManager.Instance.SetTestState(TestState.None);
    }
    
    public void CloseHelp()
    {
        ToggleHelp(false);
    }
    #endregion
}
