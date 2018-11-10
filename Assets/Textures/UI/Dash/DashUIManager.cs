using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum DashStateTypes
{
    Ignition,
    Wiper,
    Lights,
    ParkingBrake,
    Shift
};

public class DashStateMissive : Missive
{
    public DashStateTypes type = DashStateTypes.Ignition;
    public int state = 0;
}

public class DashUIManager : MonoBehaviour
{
    #region Singleton
    private static DashUIManager _instance = null;
    public static DashUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<DashUIManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>DashUIManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public Color initColor;
    public Color enabledColor;
    public Color disabledColor;

    public Image ignitionImage;
    public Image wiperImage;
    public Image lightsImage;
    public Image parkingBrakeImage;
    public Image shiftImage;

    public Sprite[] wiperStateSprites;
    public Sprite[] lightsStateSprites;
    public Sprite[] shiftStateSprites;
    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
    }

    private void Start()
    {
        //InitDashUI();
    }

    private void OnEnable()
    {
        Missive.AddListener<DashStateMissive>(OnDashStateChange);
    }

    private void OnDisable()
    {
        Missive.RemoveListener<DashStateMissive>(OnDashStateChange);
    }

    private void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region toggles
    public void InitDashUI()
    {
        ToggleShiftUI(1);
        ToggleIgnitionUI(1);
        ToggleLightsUI(0);
        ToggleParkingBrakeUI(0);
        ToggleWiperUI(0);
    }

    private void ToggleIgnitionUI(int state)
    {
        ignitionImage.color = Convert.ToBoolean(state) ? enabledColor : disabledColor;
    }

    private void ToggleWiperUI(int state)
    {
        wiperImage.color = state == 0 ? initColor : enabledColor;
        switch (state)
        {
            case 0:
                wiperImage.sprite = wiperStateSprites[0];
                break;
            case 1:
                wiperImage.sprite = wiperStateSprites[1];
                break;
            case 2:
                wiperImage.sprite = wiperStateSprites[2];
                break;
            case 3:
                wiperImage.sprite = wiperStateSprites[3];
                break;
            default:
                Debug.Log("Wiper state out of range!!!");
                break;
        }
    }

    private void ToggleLightsUI(int state)
    {
        lightsImage.color = state == 0 ? initColor : enabledColor;
        switch (state)
        {
            case 0:
                lightsImage.sprite = lightsStateSprites[0];
                break;
            case 1:
                lightsImage.sprite = lightsStateSprites[0];
                break;
            case 2:
                lightsImage.sprite = lightsStateSprites[1];
                break;
            default:
                Debug.Log("Lights state out of range!!!");
                break;
        }
    }

    private void ToggleParkingBrakeUI(int state)
    {
        parkingBrakeImage.color = Convert.ToBoolean(state) ? disabledColor : initColor;
    }

    private void ToggleShiftUI(int state)
    {
        shiftImage.color = Convert.ToBoolean(state) ? enabledColor : disabledColor;
        shiftImage.sprite = Convert.ToBoolean(state) ? shiftStateSprites[0] : shiftStateSprites[1];
    }
    #endregion

    #region buttons
    public void IgnitionOnClick()
    {
        SimulatorManager.Instance.GetCurrentActiveVehicle().GetComponent<VehicleController>().ToggleIgnition();
    }

    public void WiperOnClick()
    {
        SimulatorManager.Instance.GetCurrentActiveVehicle().GetComponent<VehicleController>().IncrementWiperState();
    }

    public void LightsOnClick()
    {
        SimulatorManager.Instance.GetCurrentActiveVehicle().GetComponent<VehicleController>().ChangeHeadlightMode();
    }

    public void ParkingBrakeOnClick()
    {
        SimulatorManager.Instance.GetCurrentActiveVehicle().GetComponent<VehicleController>().EnableHandbrake();
    }

    public void ShiftOnClick()
    {
        SimulatorManager.Instance.GetCurrentActiveVehicle().GetComponent<VehicleController>().ToggleShift();
    }
    #endregion

    #region missives
    private void OnDashStateChange(DashStateMissive missive)
    {
        switch (missive.type)
        {
            case DashStateTypes.Ignition:
                ToggleIgnitionUI(missive.state);
                break;
            case DashStateTypes.Wiper:
                ToggleWiperUI(missive.state);
                break;
            case DashStateTypes.Lights:
                ToggleLightsUI(missive.state);
                break;
            case DashStateTypes.ParkingBrake:
                ToggleParkingBrakeUI(missive.state);
                break;
            case DashStateTypes.Shift:
                ToggleShiftUI(missive.state);
                break;
            default:
                Debug.Log("Dash state out of range");
                break;
        }
    }
    #endregion
}
