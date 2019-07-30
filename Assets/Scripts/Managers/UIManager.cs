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
    public GameObject menuHolder;
    public GameObject controlsPanel;
    public GameObject infoPanel;
    public Button PauseButton;
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

    private bool _uiActive = false;
    public bool UIActive
    {
        get => _uiActive;
        set
        {
            _uiActive = value;
            ToggleControlsUI();
        }
    }

    private void Start()
    {
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

            // set sliders
            TimeOfDaySlider.value = (float)config.TimeOfDay.TimeOfDay.TotalHours;
            RainSlider.value = config.Rain;
            RainValueText.text = config.Rain.ToString("F2");
            FogSlider.value = config.Fog;
            FogValueText.text = config.Fog.ToString("F2");
            WetSlider.value = config.Wetness;
            WetValueText.text = config.Wetness.ToString("F2");
            CloudSlider.value = config.Cloudiness;
            CloudValueText.text = config.Cloudiness.ToString("F2");
        }
        else
        {
            PauseButton.gameObject.SetActive(false);
        }
        
        menuHolder.SetActive(false);
        controlsPanel.SetActive(false);
        infoPanel.SetActive(false);
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

            var infoText = infoPanel.transform.GetChild(infoPanel.transform.childCount - 1).GetComponent<Text>();
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

        PauseButton.onClick.AddListener(PauseButtonOnClick);
    }

    private void OnDestroy()
    {
        StopButton.onClick.RemoveListener(StopButtonOnClick);
        PauseButton.onClick.RemoveListener(PauseButtonOnClick);
    }

    private void StopButtonOnClick()
    {
        Loader.StopAsync();
    }

    private void PauseButtonOnClick()
    {
        Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
        PlayText.gameObject.SetActive(Time.timeScale == 0f ? true : false);
        PauseText.gameObject.SetActive(Time.timeScale == 0f ? false : true);
    }

    public void ToggleControlsUI()
    {
        if (controlsPanel.activeInHierarchy)
        {
            menuHolder.SetActive(false);
            controlsPanel.SetActive(false);
        }
        else
        {
            menuHolder.SetActive(true);
            controlsPanel.SetActive(true);
        }
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
}
