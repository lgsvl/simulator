/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using Simulator.Database.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Simulator.Web
{
    public class ConnectionUI : MonoBehaviour
    {
        public GameObject statusMenuRoot;
        public GameObject dropdownArrow;
        public Text statusText;
        public Button statusButton;
        public Text statusButtonText;
        public Image statusButtonIcon;
        public Button statusMenuButton;
        public Text statusMenuButtonText;
        public Button unlinkButton;
        public Button linkButton;
        public Button clearAssetCacheButton;
        public Button LoadedAssetsButton;
        public Button quitButton;
        public Button SettingsButton;
        public Text linkButtonText;
        public static ConnectionUI instance;
        public Color offlineColor;
        public Color onlineColor;
        public Dropdown offlineDropdown;
        public Button offlineStartButton;
        public Button offlineStopButton;
        public Text CloudTypeText;
        public Text SimulatorVersionText;
        public Text UnityVersionText;
        public Button VSEButton;
        public CacheControlWindow CacheControlWindow;
        public GameObject LoadedAssetsWindow;
        public GameObject SettingsWindow;

        public enum LoaderUIStateType { START, PROGRESS, READY };
        public LoaderUIStateType LoaderUIState = LoaderUIStateType.START;

        private SimulationService simulationService = new SimulationService();
        private List<SimulationData> simulationData;
        private int selectedSim;

        public void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            SimulatorVersionText.text = $"Simulator Version: {CloudAPI.GetInfo().version}";
            UnityVersionText.text = $"Unity Version: {Application.unityVersion}";
            ColorUtility.TryParseHtmlString("#1F2940", out offlineColor);
            ColorUtility.TryParseHtmlString("#FFFFFF", out onlineColor);
            statusButtonIcon.material.color = Color.white;
            instance = this;
            statusButton.onClick.AddListener(OnStatusButtonClicked);
            statusMenuButton.onClick.AddListener(OnStatusMenuButtonClicked);
            linkButton.onClick.AddListener(OnLinkButtonClicked);
            offlineStartButton.onClick.AddListener(OnOfflineStartButtonClicked);
            offlineStopButton.onClick.AddListener(OnOfflineStopButtonClicked);
            clearAssetCacheButton.onClick.AddListener(() =>
            {
                CacheControlWindow.gameObject.SetActive(true);
            });
            LoadedAssetsButton.onClick.AddListener(() =>
            {
                LoadedAssetsWindow.SetActive(true);
            });
            unlinkButton.onClick.AddListener(OnUnlinkButtonClicked);
            quitButton.onClick.AddListener(OnQuitButtonClicked);
            SettingsButton.onClick.AddListener(OnSettingsButtonClicked);
            UpdateDropdown();
            offlineDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            UpdateStatus(ConnectionManager.Status, "");
            TaskProgressManager.Instance.OnUpdate += UpdateDownloadProgress;
            ConnectionManager.OnStatusChanged += UpdateStatus;
        }

        public void UpdateDownloadProgress()
        {
            var text = string.Empty;
            foreach (var item in TaskProgressManager.Instance.Tasks)
            {
                text += $"{item.Description} {Mathf.Floor(item.Progress * 100)}%\n";
            }
            if (statusText != null)
                statusText.text = text;
        }

        public void UpdateStatus(ConnectionManager.ConnectionStatus status, string message)
        {
            if (statusText == null || linkButton == null || statusButtonIcon == null || statusButtonText == null || linkButtonText == null || statusButton == null)
                return; // fix for editor stop playmode null

            switch (status)
            {
                case ConnectionManager.ConnectionStatus.Connecting:
                    statusText.text = message;
                    unlinkButton.interactable = false;
                    linkButton.gameObject.SetActive(false);
                    statusButtonIcon.color = offlineColor;
                    offlineDropdown.gameObject.SetActive(false);
                    offlineStartButton.gameObject.SetActive(false);
                    VSEButton.gameObject.SetActive(false);
                    CloudTypeText.text = $"URL: {ConnectionManager.API?.CloudType}";
                    break;
                case ConnectionManager.ConnectionStatus.Connected:
                    statusText.text = "";
                    statusButtonText.text = "Online";
                    statusMenuButtonText.text = "Go Offline";
                    statusButtonIcon.color = offlineColor;
                    linkButtonText.text = "LINK TO CLOUD";
                    unlinkButton.interactable = false;
                    linkButton.gameObject.SetActive(true);
                    offlineDropdown.gameObject.SetActive(false);
                    offlineStartButton.gameObject.SetActive(false);
                    VSEButton.gameObject.SetActive(false);
                    CloudTypeText.text = $"URL: {ConnectionManager.API?.CloudType}";
                    break;
                case ConnectionManager.ConnectionStatus.Offline:
                    statusText.text = message ?? "Go Online to start new simulation or run previous simulations while being Offline";
                    statusButtonText.text = "Offline";
                    statusMenuButtonText.text = "Go Online";
                    statusButtonIcon.color = offlineColor;
                    unlinkButton.interactable = true;
                    linkButton.gameObject.SetActive(false);
                    offlineDropdown.gameObject.SetActive(true);
                    offlineStartButton.gameObject.SetActive(true);
                    VSEButton.gameObject.SetActive(false);
                    UpdateDropdown();
                    CloudTypeText.text = $"OFFLINE ({ConnectionManager.API?.CloudType})";
                    break;
                case ConnectionManager.ConnectionStatus.Online:
                    statusButtonText.text = "Online";
                    statusMenuButtonText.text = "Go Offline";
                    statusButtonIcon.color = onlineColor;
                    statusText.text = "";
                    linkButtonText.text = "OPEN BROWSER";
                    unlinkButton.interactable = true;
                    linkButton.gameObject.SetActive(true);
                    offlineDropdown.gameObject.SetActive(false);
                    offlineStartButton.gameObject.SetActive(false);
                    VSEButton.gameObject.SetActive(true);
                    CloudTypeText.text = $"URL: {ConnectionManager.API?.CloudType}";
                    break;
            }
        }

        public void UpdateStatusText(string text)
        {
            statusText.text = text;
        }

        public void UpdateDropdown()
        {
            simulationData = simulationService.List().ToList();
            offlineDropdown.ClearOptions();
            offlineDropdown.AddOptions(simulationData.Select(s => s.Name).ToList());
            offlineDropdown.value = 0;
            selectedSim = 0;
            if (simulationData.Count == 0)
            {
                offlineDropdown.gameObject.SetActive(false);
                offlineStartButton.gameObject.SetActive(false);
            }
        }

        public void OnDropdownValueChanged(int value)
        {
            selectedSim = value;
        }

        public void OnStatusButtonClicked()
        {
            bool active = !statusMenuRoot.gameObject.activeSelf;
            statusMenuRoot.SetActive(active);
            dropdownArrow.transform.localScale = new Vector3(1, active ? 1 : -1, 1);
        }

        public void OnSettingsButtonClicked()
        {
            if (SettingsWindow == null)
                return;

            SettingsWindow.SetActive(true);
        }

        public void OnQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
             Application.Quit();
#endif
        }

        public void OnOfflineStartButtonClicked()
        {
            Loader.Instance.StartSimulation(simulationData[selectedSim]);
            if (simulationData[selectedSim].ApiOnly)
            {
                offlineStopButton.gameObject.SetActive(true);
            }
        }

        public void OnOfflineStopButtonClicked()
        {
            Loader.Instance.StopAsync();
        }

        public void SetLinkingButtonActive(bool active)
        {
            linkButton.gameObject.SetActive(active);
        }

        public void SetVSEButtonActive(bool active)
        {
            VSEButton.gameObject.SetActive(active);
        }

        public void OnStatusMenuButtonClicked()
        {
            statusMenuRoot.gameObject.SetActive(false);
            ConnectionManager.instance.ConnectionStatusEvent();
        }

        public void OnLinkButtonClicked()
        {
            if (ConnectionManager.Status == ConnectionManager.ConnectionStatus.Connected)
            {
                Application.OpenURL(ConnectionManager.instance.LinkUrl);
            }
            else if (ConnectionManager.Status == ConnectionManager.ConnectionStatus.Online)
            {
                Application.OpenURL(Simulator.Web.Config.CloudUrl);
            }
        }

        public void SetLoaderUIState(LoaderUIStateType state)
        {
            LoaderUIState = state;
            switch (LoaderUIState)
            {
                case LoaderUIStateType.START:
                    break;
                case LoaderUIStateType.PROGRESS:
                    statusButtonText.text = "Loading...";
                    statusText.text = "Loading...";
                    break;
                case LoaderUIStateType.READY:
                    statusButtonText.text = "API ready!";
                    statusText.text = "API ready!";
                    break;
            }
        }

        public void OnUnlinkButtonClicked()
        {
            statusMenuRoot.SetActive(false);
            if (ConnectionManager.Status == ConnectionManager.ConnectionStatus.Online)
            {
                ConnectionManager.instance.Disconnect();
            }
            Config.RegenerateSimID();
        }

        public void EnterScenarioEditor()
        {
            var nonBlockingTask = Loader.Instance.EnterScenarioEditor();
        }
    }
}
