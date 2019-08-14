/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator;
using Simulator.Utilities;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Space(5, order = 0)]
    [Header("Loader", order = 1)]
    public Canvas LoaderUICanvas;
    public Button StopButton;
    public Text StopButtonText;

    [Space(10)]

    [Space(5, order = 0)]
    [Header("Simulator", order = 1)]
    public Canvas SimulatorCanvas;
    public GameObject MenuHolder;
    public GameObject ControlsPanel;
    public GameObject InfoPanel;
    public GameObject EnvironmentPanel;
    public Button PauseButton;
    public Button InfoButton;
    public Button ControlsButton;
    public Button EnvironmentButton;
    public Text PlayText;
    public Text PauseText;

    [Space(10)]

    [Space(5, order = 0)]
    [Header("Environment", order = 1)]
    public Slider TimeOfDaySlider;
    public Text TimeOfDayValueText;
    public Slider RainSlider;
    public Text RainValueText;
    public Slider WetSlider;
    public Text WetValueText;
    public Slider FogSlider;
    public Text FogValueText;
    public Slider CloudSlider;
    public Text CloudValueText;
    public Toggle NPCToggle;
    public Toggle PedestrianToggle;

    private bool _uiActive = false;
    public bool UIActive
    {
        get => _uiActive;
        set
        {
            _uiActive = value;
            ControlsButtonOnClick();
        }
    }

    private void Start()
    {
        PauseButton.gameObject.SetActive(false);
        EnvironmentButton.gameObject.SetActive(false);

        var config = Loader.Instance?.SimConfig;
        if (config != null)
        {
            if (config.Headless)
            {
                LoaderUICanvas.gameObject.SetActive(true);
                SimulatorCanvas.gameObject.SetActive(false);
                SimulatorManager.Instance.CameraManager.SimulatorCamera.cullingMask = 1  << LayerMask.NameToLayer("UI");
                StopButton.onClick.AddListener(StopButtonOnClick);
            }

            PauseButton.gameObject.SetActive(config.Interactive);
            EnvironmentButton.gameObject.SetActive(config.Interactive);

            if (config.Interactive)
            {
                // set sliders
                TimeOfDaySlider.value = (float)config.TimeOfDay.TimeOfDay.TotalHours;
                TimeOfDayValueText.text = config.TimeOfDay.TimeOfDay.TotalHours.ToString("F2");
                RainSlider.value = config.Rain;
                RainValueText.text = config.Rain.ToString("F2");
                FogSlider.value = config.Fog;
                FogValueText.text = config.Fog.ToString("F2");
                WetSlider.value = config.Wetness;
                WetValueText.text = config.Wetness.ToString("F2");
                CloudSlider.value = config.Cloudiness;
                CloudValueText.text = config.Cloudiness.ToString("F2");

                // set toggles
                NPCToggle.isOn = config.UseTraffic;
                PedestrianToggle.isOn = config.UsePedestrians;

                PauseButton.onClick.AddListener(PauseButtonOnClick);
                EnvironmentButton.onClick.AddListener(EnvironmentButtonOnClick);
            }
        }

        MenuHolder.SetActive(false);
        ControlsPanel.SetActive(false);
        InfoPanel.SetActive(false);
        EnvironmentPanel.SetActive(false);

        PlayText.gameObject.SetActive(Time.timeScale == 0f ? true : false);
        PauseText.gameObject.SetActive(Time.timeScale == 0f ? false : true);

        var info = Resources.Load<BuildInfo>("BuildInfo");
        if (info != null)
        {
            var timestamp = DateTime.ParseExact(info.Timestamp, "o", CultureInfo.InvariantCulture);
            Debug.Log($"Build Timestamp = {timestamp}");
            Debug.Log($"Version = {info.Version}");
            Debug.Log($"GitCommit = {info.GitCommit}");
            Debug.Log($"GitBranch = {info.GitBranch}");

            var infoText = InfoPanel.transform.GetChild(InfoPanel.transform.childCount - 1).GetComponent<Text>();
            if (infoText != null)
            {
                infoText.text = String.Join("\n", new [] {
                    $"Build Timestamp = {timestamp}",
                    $"Version = {info.Version}",
                    $"GitCommit = {info.GitCommit}",
                    $"GitBranch = {info.GitBranch}"
                });
            }
        }

        InfoButton.onClick.AddListener(InfoButtonOnClick);
        ControlsButton.onClick.AddListener(ControlsButtonOnClick);
    }

    private void OnDestroy()
    {
        StopButton.onClick.RemoveListener(StopButtonOnClick);
        PauseButton.onClick.RemoveListener(PauseButtonOnClick);
        InfoButton.onClick.RemoveListener(InfoButtonOnClick);
        ControlsButton.onClick.RemoveListener(ControlsButtonOnClick);
        EnvironmentButton.onClick.RemoveListener(EnvironmentButtonOnClick);
    }

    private void StopButtonOnClick()
    {
        Loader.StopAsync();
    }

    private void PauseButtonOnClick()
    {
        bool paused = Time.timeScale == 0.0f;
        SimulatorManager.SetTimeScale(paused ? 1f : 0f);
        PlayText.gameObject.SetActive(!paused);
        PauseText.gameObject.SetActive(paused);
    }

    private void InfoButtonOnClick()
    {
        ControlsPanel.SetActive(false);
        EnvironmentPanel.SetActive(false);
        MenuHolder.SetActive(!InfoPanel.activeInHierarchy);
        InfoPanel.SetActive(!InfoPanel.activeInHierarchy);
    }

    private void ControlsButtonOnClick()
    {
        InfoPanel.SetActive(false);
        EnvironmentPanel.SetActive(false);
        MenuHolder.SetActive(!ControlsPanel.activeInHierarchy);
        ControlsPanel.SetActive(!ControlsPanel.activeInHierarchy);
    }

    private void EnvironmentButtonOnClick()
    {
        if (SimulatorManager.Instance == null) return;

        TimeOfDaySlider.value = SimulatorManager.Instance.EnvironmentEffectsManager.currentTimeOfDay;
        TimeOfDayValueText.text = SimulatorManager.Instance.EnvironmentEffectsManager.currentTimeOfDay.ToString("F2");
        RainSlider.value = SimulatorManager.Instance.EnvironmentEffectsManager.rain;
        RainValueText.text = SimulatorManager.Instance.EnvironmentEffectsManager.rain.ToString("F2");
        FogSlider.value = SimulatorManager.Instance.EnvironmentEffectsManager.fog;
        FogValueText.text = SimulatorManager.Instance.EnvironmentEffectsManager.fog.ToString("F2");
        WetSlider.value = SimulatorManager.Instance.EnvironmentEffectsManager.wet;
        WetValueText.text = SimulatorManager.Instance.EnvironmentEffectsManager.wet.ToString("F2");
        CloudSlider.value = SimulatorManager.Instance.EnvironmentEffectsManager.cloud;
        CloudValueText.text = SimulatorManager.Instance.EnvironmentEffectsManager.cloud.ToString("F2");
        NPCToggle.isOn = SimulatorManager.Instance.NPCManager.NPCActive;
        PedestrianToggle.isOn = SimulatorManager.Instance.PedestrianManager.PedestriansActive;
        
        InfoPanel.SetActive(false);
        ControlsPanel.SetActive(false);
        MenuHolder.SetActive(!EnvironmentPanel.activeInHierarchy);
        EnvironmentPanel.SetActive(!EnvironmentPanel.activeInHierarchy);
    }
    
    public void TimeOfDayOnValueChanged(float value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.EnvironmentEffectsManager.currentTimeOfDay = value;
        TimeOfDayValueText.text = value.ToString("F2");
    }

    public void RainOnValueChanged(float value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.EnvironmentEffectsManager.rain = value;
        RainValueText.text = value.ToString("F2");
    }

    public void FogOnValueChanged(float value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.EnvironmentEffectsManager.fog = value;
        FogValueText.text = value.ToString("F2");
    }

    public void WetOnValueChanged(float value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.EnvironmentEffectsManager.wet = value;
        WetValueText.text = value.ToString("F2");
    }

    public void CloudOnValueChanged(float value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.EnvironmentEffectsManager.cloud = value;
        CloudValueText.text = value.ToString("F2");
    }

    public void NPCOnValueChanged(bool value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.NPCManager.NPCActive = value;
    }

    public void PedestrianOnValueChanged(bool value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.PedestrianManager.PedestriansActive = value;
    }
}
