/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator;
using Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Simulator.Sensors.UI;
using Simulator.Sensors;
using Simulator.Components;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;

public class UIManager : MonoBehaviour
{
    private enum PanelType
    {
        None,
        Info,
        Controls,
        Environment,
        Visualizer,
        Bridge
    };
    private PanelType currentPanelType = PanelType.None;

    [Space(5, order = 0)]
    [Header("Loader", order = 1)]
    public Canvas LoaderUICanvas;
    public Button StopButton;
    public Text StopButtonText;

    [Space(10)]

    [Space(5, order = 0)]
    [Header("Simulator", order = 1)]
    public Canvas SimulatorCanvas;
    public Image cinematicFadeImage;
    public GameObject MenuHolder;
    public GameObject ControlsPanel;
    public GameObject InfoPanel;
    public GameObject EnvironmentPanel;
    public GameObject VisualizerPanel;
    public GameObject BridgePanel;
    public Button MenuButton;
    public Button PauseButton;
    public Button CloseButton;
    public Button InfoButton;
    public Button ClearButton;
    public Button ControlsButton;
    public Button EnvironmentButton;
    public Button VisualizerButton;
    public Button ResetButton;
    public Button DisableAllButton;
    public Button BridgeButton;
    public Text PlayText;
    public Text PauseText;
    public Transform InfoContent;
    public string Log;
    public string Warning;
    public string Error;
    public Text InfoTextPrefab;
    public Transform BridgeContent;
    private BridgeClient BridgeClient;
    private Text BridgeClientStatusText;
    private bool paused = false;
    private Dictionary<string, Text> CurrentBridgePublisherInfo = new Dictionary<string, Text>();
    private Dictionary<string, Text> CurrentBridgeSubscriberInfo = new Dictionary<string, Text>();

    [Space(10)]

    [Space(5, order = 0)]
    [Header("Environment", order = 1)]
    public Slider TimeOfDaySlider;
    public Text TimeOfDayValueText;
    public Toggle TimeOfDayFreezeToggle;
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

    [Space(10)]

    [Space(5, order = 0)]
    [Header("Visualize", order = 1)]
    public VisualizerToggle VisualizerTogglePrefab;
    public Transform VisualizerContent;
    public GameObject VisualizerCanvasGO;
    private GridLayoutGroup VisualizerGridLayoutGroup;
    public Visualizer VisualizerPrefab;
    private List<VisualizerToggle> visualizerToggles = new List<VisualizerToggle>();
    private List<Visualizer> visualizers = new List<Visualizer>();
    private bool allVisualizersActive = false;

    [Space(10)]

    [Space(5, order = 0)]
    [Header("Agent Select", order = 1)]
    public Dropdown AgentDropdown;

    [Space(10)]

    [Space(5, order = 0)]
    [Header("Camera", order = 1)]
    public Button CameraButton;
    public Text LockedText;
    public Text UnlockedText;
    public Text CinematicText;
    public Text CameraStateText;

    private StringBuilder sb = new StringBuilder();

    ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();

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

    private void Awake()
    {
        VisualizerGridLayoutGroup = VisualizerCanvasGO.GetComponent<GridLayoutGroup>();
        ResetCinematicAlpha();
    }

    private void Start()
    {
        PauseButton.gameObject.SetActive(false);
        EnvironmentButton.gameObject.SetActive(false);
        MenuHolder.SetActive(false);

        var config = Loader.Instance?.SimConfig;
        if (config != null)
        {
            if (config.Headless) //TODO api and headless? reset?
            {
                LoaderUICanvas.gameObject.SetActive(true);
                SimulatorCanvas.gameObject.SetActive(false);
                SimulatorManager.Instance.CameraManager.SimulatorCamera.cullingMask = 1  << LayerMask.NameToLayer("UI");
            }

            var usePauseButton = config.Interactive;
            PauseButton.gameObject.SetActive(usePauseButton);
            if (usePauseButton)
            {
                PauseSimulation();
                SimulatorManager.Instance.TimeManager.TimeScaleSemaphore.LocksCountChanged += UpdatePauseButton;
            }

            EnvironmentButton.gameObject.SetActive(config.Interactive);
            MenuHolder.SetActive(config.Interactive);

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
            }
        }

        SetBuildInfo();

        PlayText.gameObject.SetActive(Time.timeScale == 0f ? true : false);
        PauseText.gameObject.SetActive(Time.timeScale == 0f ? false : true);

        MenuButton.onClick.AddListener(MenuButtonOnClick);
        StopButton.onClick.AddListener(StopButtonOnClick);
        CloseButton.onClick.AddListener(CloseButtonOnClick);
        PauseButton.onClick.AddListener(PauseButtonOnClick);
        InfoButton.onClick.AddListener(InfoButtonOnClick);
        ClearButton.onClick.AddListener(ClearButtonOnClick);
        ControlsButton.onClick.AddListener(ControlsButtonOnClick);
        EnvironmentButton.onClick.AddListener(EnvironmentButtonOnClick);
        VisualizerButton.onClick.AddListener(VisualizerButtonOnClick);
        ResetButton.onClick.AddListener(ResetOnClick);
        DisableAllButton.onClick.AddListener(DisableAllOnClick);
        BridgeButton.onClick.AddListener(BridgeButtonOnClick);
        AgentDropdown.onValueChanged.AddListener(OnAgentSelected);
        CameraButton.onClick.AddListener(CameraButtonOnClick);

        SetCameraButtonState();
    }

    private void Update()
    {
        if (BridgePanel.activeInHierarchy)
        {
            UpdateBridgeInfo();
        }

        if (EnvironmentPanel.activeInHierarchy)
        {
            if (!TimeOfDayFreezeToggle.isOn)
            {
                TimeOfDaySlider.value = SimulatorManager.Instance.EnvironmentEffectsManager.currentTimeOfDay;
            }
        }

        while (MainThreadActions.TryDequeue(out var action))
        {
            action();
        }
    }

    private void OnEnable()
    {
        SimulatorManager.Instance.AgentManager.AgentChanged += OnAgentChange;
        Application.logMessageReceivedThreaded += LogMessage;
    }

    private void OnDisable()
    {
        SimulatorManager.Instance.AgentManager.AgentChanged -= OnAgentChange;
        Application.logMessageReceivedThreaded -= LogMessage;
    }

    private void OnDestroy()
    {
        MenuButton.onClick.RemoveListener(MenuButtonOnClick);
        StopButton.onClick.RemoveListener(StopButtonOnClick);
        PauseButton.onClick.RemoveListener(PauseButtonOnClick);
        InfoButton.onClick.RemoveListener(InfoButtonOnClick);
        ClearButton.onClick.RemoveListener(ClearButtonOnClick);
        ControlsButton.onClick.RemoveListener(ControlsButtonOnClick);
        EnvironmentButton.onClick.RemoveListener(EnvironmentButtonOnClick);
        VisualizerButton.onClick.RemoveListener(VisualizerButtonOnClick);
        ResetButton.onClick.RemoveListener(ResetOnClick);
        DisableAllButton.onClick.RemoveListener(DisableAllOnClick);
        BridgeButton.onClick.RemoveListener(BridgeButtonOnClick);
        AgentDropdown.onValueChanged.RemoveListener(OnAgentSelected);
        CameraButton.onClick.RemoveListener(CameraButtonOnClick);
        CloseButton.onClick.RemoveListener(CloseButtonOnClick);
        if (Loader.Instance != null && Loader.Instance.SimConfig != null) // TODO fix for Editor needs SimConfig
        {
            if (Loader.Instance.SimConfig.Interactive)
                SimulatorManager.Instance.TimeManager.TimeScaleSemaphore.LocksCountChanged -= UpdatePauseButton;
        }
            
    }

    public void Reset() //api
    {
        SetCameraButtonState();
        CloseButtonOnClick();
    }

    public void SetCameraButtonState()
    {
        var current = SimulatorManager.Instance.CameraManager.GetCurrentCameraState();
        CameraStateText.text = current.ToString();
        switch (current)
        {
            case CameraStateType.Free:
                LockedText.gameObject.SetActive(false);
                UnlockedText.gameObject.SetActive(true);
                CinematicText.gameObject.SetActive(false);
                break;
            case CameraStateType.Follow:
                LockedText.gameObject.SetActive(true);
                UnlockedText.gameObject.SetActive(false);
                CinematicText.gameObject.SetActive(false);
                break;
            case CameraStateType.Cinematic:
                LockedText.gameObject.SetActive(false);
                UnlockedText.gameObject.SetActive(false);
                CinematicText.gameObject.SetActive(true);
                break;
        }
    }

    private void CameraButtonOnClick()
    {
        SimulatorManager.Instance.CameraManager.ToggleCameraState();
    }

    public void SetAgentsDropdown()
    {
        AgentDropdown.options.Clear();
        AgentDropdown.ClearOptions();
        for (int i = 0; i < SimulatorManager.Instance.AgentManager.ActiveAgents.Count; i++)
        {
            AgentDropdown.options.Add(new Dropdown.OptionData((i + 1) + " - " + SimulatorManager.Instance.AgentManager.ActiveAgents[i].AgentGO.name));
        }
        AgentDropdown.value = SimulatorManager.Instance.AgentManager.GetCurrentActiveAgentIndex();
        AgentDropdown.RefreshShownValue();
    }

    public void OnAgentSelected(int value)
    {
        SimulatorManager.Instance.AgentManager.SetCurrentActiveAgent(value);
    }

    private void LogMessage(string message, string stackTrace, LogType type)
    {
        var color = Color.white;
        var typeString = Log;
        switch (type)
        {
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                color = Color.red;
                typeString = Error;
                break;
            case LogType.Warning:
                color = Color.yellow;
                typeString = Warning;
                break;
            default:
                break;
        }
        CreateInfo(string.Format("<color=#{0}>{1}</color> {2}", ColorUtility.ToHtmlStringRGBA(color), typeString, message), stackTrace);
    }

    private void SetBuildInfo()
    {
        var timeStamp = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
        var version = "Development";
        var gitCommit = "Development";
        var gitBranch = "Development";

        var info = Resources.Load<BuildInfo>("BuildInfo");
        if (info != null)
        {
            timeStamp = DateTime.ParseExact(info.Timestamp, "o", CultureInfo.InvariantCulture).ToString();
            version = info.Version;
            gitCommit = info.GitCommit;
            gitBranch = info.GitBranch;
        }

        sb.Clear();
        sb.AppendLine($"Build Timestamp = {timeStamp}");
        sb.AppendLine($"Version = {version}");
        sb.AppendLine($"GitCommit = {gitCommit}");
        if (!string.IsNullOrEmpty(gitBranch))
        {
            sb.AppendLine($"GitBranch = {gitBranch}");
        }
        CreateInfo(sb.ToString(), isBuildInfo: true);
    }

    private void CreateInfo(string text, string stacktrace = null, bool isBuildInfo = false)
    {
        MainThreadActions.Enqueue(() =>
        {
            if (InfoContent.childCount > 25)
            {
                Destroy(InfoContent.GetChild(1).gameObject);
            }
            var info = Instantiate(InfoTextPrefab, InfoContent);
            var infoOnClick = info.GetComponent<InfoTextOnClick>();
            info.text = text;
            infoOnClick.BuildInfo = isBuildInfo;
            if (stacktrace != null)
                infoOnClick.SubString = stacktrace;
            info.transform.SetAsLastSibling();
        });
    }

    private void SetAgentBridgeInfo(GameObject agent)
    {
        CurrentBridgePublisherInfo.Clear();
        CurrentBridgeSubscriberInfo.Clear();
        BridgeClient = null;
        BridgeClientStatusText = null;
        var temp = BridgeContent.GetComponentsInChildren<InfoTextOnClick>();
        for (int i = 0; i < temp.Length; i++)
        {
            Destroy(temp[i].gameObject);
        }

        BridgeClient = agent.GetComponent<BridgeClient>();
        if (BridgeClient != null)
        {
            CreateBridgeInfo($"Vehicle: {agent.name}");
            CreateBridgeInfo($"Bridge Status: {BridgeClient.BridgeStatus}", true);
            CreateBridgeInfo($"Address: {BridgeClient.PrettyAddress.ToString()}");

            foreach (var pub in BridgeClient.Bridge.TopicPublishers)
            {
                sb.Clear();
                sb.AppendLine($"PUB:");
                sb.AppendLine($"Topic: {pub.Topic}");
                sb.AppendLine($"Type: {pub.Type}");
                sb.AppendLine($"Frequency: {pub.Frequency:F2} Hz");
                sb.AppendLine($"Count: {pub.Count}");
                if (!CurrentBridgePublisherInfo.ContainsKey(pub.Topic))
                {
                    CurrentBridgePublisherInfo.Add(pub.Topic, CreateBridgeInfo(sb.ToString()));
                }
            }

            foreach (var sub in BridgeClient.Bridge.TopicSubscriptions)
            {
                sb.Clear();
                sb.AppendLine($"SUB:");
                sb.AppendLine($"Topic: {sub.Topic}");
                sb.AppendLine($"Type: {sub.Type}");
                sb.AppendLine($"Frequency: {sub.Frequency:F2} Hz");
                sb.AppendLine($"Count: {sub.Count}");
                if (!CurrentBridgeSubscriberInfo.ContainsKey(sub.Topic))
                {
                    CurrentBridgeSubscriberInfo.Add(sub.Topic, CreateBridgeInfo(sb.ToString()));
                }
            }
        }
        else
        {
            CreateBridgeInfo($"Vehicle: {agent.name}");
            CreateBridgeInfo("Bridge Status: Null");
        }
    }

    private Text CreateBridgeInfo(string text, bool isBridgeStatus = false)
    {
        var bridgeInfo = Instantiate(InfoTextPrefab, BridgeContent);
        bridgeInfo.text = text;
        bridgeInfo.transform.SetAsLastSibling();
        if (isBridgeStatus)
        {
            BridgeClientStatusText = bridgeInfo;
        }
        return bridgeInfo;
    }

    private void UpdateBridgeInfo()
    {
        if (BridgeClient != null && BridgeClientStatusText != null)
        {
            BridgeClientStatusText.text = "Bridge Status: " + BridgeClient.BridgeStatus;
        }

        if (BridgeClient == null)
        {
            return;
        }

        if (CurrentBridgePublisherInfo == null && CurrentBridgeSubscriberInfo == null)
        {
            return;
        }

        foreach (var pub in BridgeClient.Bridge.TopicPublishers)
        {
            if (CurrentBridgePublisherInfo.ContainsKey(pub.Topic))
            {
                Text ui = CurrentBridgePublisherInfo[pub.Topic];
                if (pub.ElapsedTime >= 1 && pub.Count > pub.StartCount)
                {
                    pub.Frequency = (pub.Count - pub.StartCount) / pub.ElapsedTime;
                    pub.StartCount = pub.Count;
                    pub.ElapsedTime = 0f;
                }
                else
                {
                    pub.ElapsedTime += Time.unscaledDeltaTime;
                }

                sb.Clear();
                sb.AppendLine($"PUB:");
                sb.AppendLine($"Topic: {pub.Topic}");
                sb.AppendLine($"Type: {pub.Type}");
                sb.AppendLine($"Frequency: {pub.Frequency:F2} Hz");
                sb.AppendLine($"Count: {pub.Count}");
                ui.text = sb.ToString();
            }
        }

        foreach (var sub in BridgeClient.Bridge.TopicSubscriptions)
        {
            if (CurrentBridgeSubscriberInfo.ContainsKey(sub.Topic))
            {
                Text ui = CurrentBridgeSubscriberInfo[sub.Topic];
                if (sub.ElapsedTime >= 1 && sub.Count > sub.StartCount)
                {
                    sub.Frequency = (sub.Count - sub.StartCount) / sub.ElapsedTime;
                    sub.StartCount = sub.Count;
                    sub.ElapsedTime = 0f;
                }
                else
                {
                    sub.ElapsedTime += Time.unscaledDeltaTime;
                }

                sb.Clear();
                sb.AppendLine($"SUB:");
                sb.AppendLine($"Topic: {sub.Topic}");
                sb.AppendLine($"Type: {sub.Type}");
                sb.AppendLine($"Frequency: {sub.Frequency:F2} Hz");
                sb.AppendLine($"Count: {sub.Count}");
                ui.text = sb.ToString();
            }
        }
    }

    private void OnAgentChange(GameObject agent)
    {
        for (int i = 0; i < visualizerToggles.Count; i++)
        {
            Destroy(visualizerToggles[i].gameObject);
        }
        for (int i = 0; i < visualizers.Count; i++)
        {
            Destroy(visualizers[i].gameObject);
        }
        visualizerToggles.Clear();
        visualizers.Clear();
        VisualizerGridLayoutGroup.enabled = true;
        Array.ForEach(agent.GetComponentsInChildren<SensorBase>(), sensor =>
        {
            AddVisualizer(sensor);
        });
        VisualizerGridLayoutGroup.enabled = false;
        SetAgentBridgeInfo(agent);
        AgentDropdown.value = SimulatorManager.Instance.AgentManager.GetCurrentActiveAgentIndex();
    }

    private void SetCurrentPanel()
    {
        InfoPanel.SetActive(InfoPanel.activeInHierarchy ? false : currentPanelType == PanelType.Info);
        ControlsPanel.SetActive(ControlsPanel.activeInHierarchy ? false : currentPanelType == PanelType.Controls);
        EnvironmentPanel.SetActive(EnvironmentPanel.activeInHierarchy ? false : currentPanelType == PanelType.Environment);
        VisualizerPanel.SetActive(VisualizerPanel.activeInHierarchy ? false : currentPanelType == PanelType.Visualizer);
        BridgePanel.SetActive(BridgePanel.activeInHierarchy ? false : currentPanelType == PanelType.Bridge);
    }

    public void MenuButtonOnClick()
    {
        MenuHolder.SetActive(!MenuHolder.activeInHierarchy);
    }

    private void CloseButtonOnClick()
    {
        currentPanelType = PanelType.None;
        SetCurrentPanel();
        MenuHolder.SetActive(false);
    }

    private void StopButtonOnClick()
    {
        Loader.StopAsync();
    }

    public void PauseButtonOnClick()
    {
        var interactive = Loader.Instance?.SimConfig?.Interactive;
        if (interactive == null || interactive == false)
        {
            return;
        }

        if (paused)
            ContinueSimulation();
        else
            PauseSimulation();
    }

    private void PauseSimulation()
    {
        if (paused)
            return;
        var timeManager = SimulatorManager.Instance.TimeManager;
        timeManager.TimeScaleSemaphore.Lock();
        paused = true;
        UpdatePauseButton(timeManager.TimeScaleSemaphore.Locks);
    }

    private void ContinueSimulation()
    {
        if (!paused)
            return;
        var timeManager = SimulatorManager.Instance.TimeManager;
        timeManager.TimeScaleSemaphore.Unlock();
        //TODO integrate all scripts with the lock/unlock timescale and remove setting timescale here
        if (timeManager.TimeScaleSemaphore.IsUnlocked && Mathf.Approximately(timeManager.TimeScale, 0.0f))
            timeManager.TimeScale = 1.0f;
        paused = false;
        UpdatePauseButton(timeManager.TimeScaleSemaphore.Locks);
    }

    private void UpdatePauseButton(float timeScaleLocks)
    {
        PlayText.gameObject.SetActive(paused);
        PauseText.gameObject.SetActive(!paused);
        var externalPause = timeScaleLocks - (paused ? 1 : 0) > 0;
        PauseButton.interactable = !externalPause;
        //TODO better visual effect for disabled button
        (paused ? PlayText : PauseText).color = (externalPause ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.8f, 0.8f, 0.8f));
    }

    private void InfoButtonOnClick()
    {
        currentPanelType = PanelType.Info;
        SetCurrentPanel();
    }

    private void ClearButtonOnClick()
    {
        var temp = InfoContent.GetComponentsInChildren<InfoTextOnClick>();

        for (int i = 0; i < temp.Length; i++)
        {
            if (!temp[i].BuildInfo)
                Destroy(temp[i].gameObject);
        }
    }

    private void ControlsButtonOnClick()
    {
        currentPanelType = PanelType.Controls;
        SetCurrentPanel();
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

        currentPanelType = PanelType.Environment;
        SetCurrentPanel();
    }

    private void VisualizerButtonOnClick()
    {
        currentPanelType = PanelType.Visualizer;
        SetCurrentPanel();
    }

    private void BridgeButtonOnClick()
    {
        currentPanelType = PanelType.Bridge;
        SetCurrentPanel();
    }

    public void TimeOfDayOnValueChanged(float value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.EnvironmentEffectsManager.currentTimeOfDay = value;
        TimeOfDayValueText.text = value.ToString("F2");
    }

    public void TimeOfDayFreezeOnValueChanged(bool value)
    {
        if (SimulatorManager.Instance == null) return;
        SimulatorManager.Instance.EnvironmentEffectsManager.currentTimeOfDayCycle = value ? EnvironmentEffectsManager.TimeOfDayCycleTypes.Freeze : EnvironmentEffectsManager.TimeOfDayCycleTypes.Normal;
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

    private void AddVisualizer(SensorBase sensor)
    {
        var tog = Instantiate(VisualizerTogglePrefab, VisualizerContent);
        visualizerToggles.Add(tog);
        tog.VisualizerNameText.text = sensor.Name;
        tog.Sensor = sensor;

        var vis = Instantiate(VisualizerPrefab, VisualizerCanvasGO.transform);
        visualizers.Add(vis);
        vis.transform.localPosition = Vector2.zero;
        vis.Init(sensor.Name);
        vis.Sensor = sensor;
        vis.VisualizerToggle = tog;
        vis.gameObject.SetActive(false);
        tog.Visualizer = vis;

        if (!VisualizerCanvasGO.activeInHierarchy)
        {
            VisualizerCanvasGO.SetActive(true);
        }
    }

    private void ResetOnClick()
    {
        VisualizerGridLayoutGroup.enabled = true;
        for (int i = 0; i < visualizers.Count; i++)
        {
            visualizers[i].ResetWindow();
        }
        foreach (var vis in visualizers)
        {
            if (!vis.HeaderGO.activeInHierarchy)
                vis.transform.SetAsLastSibling();
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(VisualizerCanvasGO.GetComponent<RectTransform>());
        VisualizerGridLayoutGroup.enabled = false;
    }

    private void DisableAllOnClick()
    {
        visualizers.ForEach(x => x.gameObject.SetActive(false));
    }

    public void ToggleVisualizers()
    {
        allVisualizersActive = !allVisualizersActive;
        foreach (var vis in visualizers)
        {
            vis.gameObject.SetActive(allVisualizersActive);
        }
    }

    private void RemoveVisualizer(Visualizer visualizer)
    {
        visualizers.Remove(visualizer);
        Destroy(visualizer.gameObject);
        allVisualizersActive = false;

        if (visualizers.Count == 0)
        {
            VisualizerCanvasGO.SetActive(false);
        }
    }

    public void FadeOutIn(float duration)
    {
        cinematicFadeImage.color = Color.black;
        cinematicFadeImage.canvasRenderer.SetAlpha(0f);
        cinematicFadeImage.CrossFadeAlpha(1f, duration/2, true);
        cinematicFadeImage.color = Color.black;
        cinematicFadeImage.canvasRenderer.SetAlpha(1f);
        cinematicFadeImage.CrossFadeAlpha(0f, duration/2, true);
    }

    public void ResetCinematicAlpha()
    {
        cinematicFadeImage.color = Color.clear;
    }
}
