/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using Elements;
    using Input;
    using Network.Core;
    using Network.Core.Threading;
    using UI.FileEdit;
    using UI.MapSelecting;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Scenario editor manager connecting all other components
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the scenario manager
        /// </summary>
        private static ScenarioManager instance;

        /// <summary>
        /// Singleton instance of the scenario manager
        /// </summary>
        public static ScenarioManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<ScenarioManager>();
                return instance;
            }
            private set
            {
                if (instance == value)
                    return;
                if (instance != null && value != null)
                    throw new ArgumentException($"Instance of {instance.GetType().Name} is already set.");
                instance = value;
            }
        }

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Camera used to render the scenario world
        /// </summary>
        [SerializeField]
        private Camera scenarioCamera;

        /// <summary>
        /// The loading panel game object
        /// </summary>
        [SerializeField]
        private GameObject loadingPanel;
#pragma warning restore 0649

        /// <summary>
        /// Is the manager initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Semaphore that holds the loading screen
        /// </summary>
        private LockingSemaphore loadingSemaphore = new LockingSemaphore();

        /// <summary>
        /// Is there a single popup visible in the scenario editor
        /// </summary>
        private bool viewsPopup;

        /// <summary>
        /// Initial position of the camera, applied when the map changes
        /// </summary>
        private Vector3 cameraInitialPosition;

        /// <summary>
        /// Currently selected scenario element
        /// </summary>
        private ScenarioElement selectedElement;

        /// <summary>
        /// <see cref="InputManager"/> used in the scenario manager
        /// </summary>
        public InputManager inputManager;

        /// <summary>
        /// <see cref="ObjectsShotCapture"/> allows taking a screen shot of the object as a texture
        /// </summary>
        public ObjectsShotCapture objectsShotCapture;

        /// <summary>
        /// <see cref="PrefabsPools"/> used for pooling game objects in the scenario editor
        /// </summary>
        public PrefabsPools prefabsPools;

        /// <summary>
        /// Shared <see cref="SelectFileDialog"/> to be used in the scenario editor, dialog can handle only one request at same time
        /// </summary>
        public SelectFileDialog selectFileDialog;

        /// <summary>
        /// Popup which requires user interaction to confirm an operation
        /// </summary>
        public ConfirmationPopup confirmationPopup;

        /// <summary>
        /// Log panel for displaying message to the user for a limited time
        /// </summary>
        public LogPanel logPanel;

        /// <summary>
        /// Manager for caching and handling all the scenario agents and their sources
        /// </summary>
        public ScenarioAgentsManager agentsManager;

        /// <summary>
        /// Manager for caching and handling all the scenario waypoints
        /// </summary>
        public ScenarioWaypointsManager waypointsManager;

        /// <summary>
        /// Camera used to render the scenario world
        /// </summary>
        public Camera ScenarioCamera => scenarioCamera;

        /// <summary>
        /// Scenario editor map manager for checking current map and loading another
        /// </summary>
        public ScenarioMapManager MapManager { get; } = new ScenarioMapManager();

        /// <summary>
        /// Currently selected scenario element
        /// </summary>
        public ScenarioElement SelectedElement
        {
            get => selectedElement;
            set
            {
                if (selectedElement == value)
                    return;
                selectedElement = value;
                if (selectedElement != null)
                    selectedElement.Selected();
                SelectedOtherElement?.Invoke(selectedElement);
            }
        }

        /// <summary>
        /// Is there a single popup visible in the scenario editor
        /// </summary>
        public bool ViewsPopup
        {
            get => viewsPopup;
            set
            {
                if (viewsPopup == value)
                    return;
                viewsPopup = value;
                if (viewsPopup)
                    inputManager.InputSemaphore.Lock();
                else
                    inputManager.InputSemaphore.Unlock();
            }
        }

        /// <summary>
        /// Is scenario dirty, true if there are some unsaved changes
        /// </summary>
        public bool IsScenarioDirty { get; set; }

        /// <summary>
        /// Event invoked when the new scenario element is created and activated in scenario
        /// </summary>
        public event Action<ScenarioElement> NewScenarioElement;

        /// <summary>
        /// Event invoked when the selected scenario element changes
        /// </summary>
        public event Action<ScenarioElement> SelectedOtherElement;

        /// <summary>
        /// Unity Start method
        /// </summary>
        /// <exception cref="ArgumentException">Invalid ScenarioManager game object setup</exception>
        private void Start()
        {
            if (scenarioCamera == null)
                throw new ArgumentException("Scenario camera reference is required in the ScenarioManager.");
            var cameraTransform = scenarioCamera.transform;
            cameraInitialPosition = cameraTransform.position;
            if (Instance == null || Instance == this)
            {
                var nonBlockingTask = Initialize();
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            if (Instance != this) return;

            Deinitialize();
            Instance = null;
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        public async Task Initialize()
        {
            if (isInitialized)
                return;
            ShowLoadingPanel();
            MapManager.MapChanged += OnMapLoaded;
            var mapLoading = MapManager.LoadMapAsync();
            var agentsLoading = agentsManager.Initialize();
            waypointsManager.Initialize();
            await Task.WhenAll(mapLoading, agentsLoading);
            Time.timeScale = 0.0f;
            await FixLights();
            isInitialized = true;
            HideLoadingPanel();
        }

        /// <summary>
        /// Fixes lights on the map scene
        /// </summary>
        /// <returns></returns>
        private async Task FixLights()
        {
            //Enabling camera three times with those delays forces Unity to recalculate lights
            objectsShotCapture.ShotObject(gameObject);
            await Task.Delay(100);
            objectsShotCapture.ShotObject(gameObject);
            await Task.Delay(100);
            objectsShotCapture.ShotObject(gameObject);
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (!isInitialized)
                return;
            MapManager.MapChanged -= OnMapLoaded;
            MapManager.UnloadMapAsync();
            waypointsManager.Deinitialize();
            agentsManager.Deinitialize();
            Time.timeScale = 1.0f;
            isInitialized = false;
        }

        /// <summary>
        /// Requests scenario reset, operation can be canceled by the user
        /// </summary>
        /// <param name="successCallback">Callback invoked when scenario reset successes</param>
        /// <param name="failCallback">Callback invoked when scenario reset fails</param>
        public void RequestResetScenario(Action successCallback, Action failCallback)
        {
            if (!IsScenarioDirty)
            {
                ResetScenario();
                successCallback?.Invoke();
                return;
            }

            var popupData = new ConfirmationPopup.PopupData()
            {
                Text = "There are unsaved changes in the scenario, do you wish to discard them?"
            };
            popupData.ConfirmCallback += () =>
            {
                ResetScenario();
                successCallback?.Invoke();
            };
            popupData.CancelCallback += failCallback;

            confirmationPopup.Show(popupData);
        }

        /// <summary>
        /// Resets whole scenario removing all added elements
        /// </summary>
        private void ResetScenario()
        {
            SelectedElement = null;
            var agents = agentsManager.Agents;
            for (var i = agents.Count - 1; i >= 0; i--)
            {
                var agent = agents[i];
                agent.Remove();
            }

            var waypoints = waypointsManager.Waypoints;
            for (var i = waypoints.Count - 1; i >= 0; i--)
            {
                var waypoint = waypoints[i];
                waypoint.Remove();
            }

            Instance.IsScenarioDirty = false;
        }

        /// <summary>
        /// Shows loading panel
        /// </summary>
        public void ShowLoadingPanel()
        {
            loadingSemaphore.Lock();
            loadingPanel.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides loading panel
        /// </summary>
        public void HideLoadingPanel()
        {
            loadingSemaphore.Unlock();
            if (loadingSemaphore.IsUnlocked)
                loadingPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Method called when new map has been loaded, resets the camera position and hides loading panel
        /// </summary>
        /// <param name="mapName">New loaded map name</param>
        public void OnMapLoaded(string mapName)
        {
            var cameraTransform = ScenarioCamera.transform;
            cameraTransform.position = cameraInitialPosition;
        }

        /// <summary>
        /// Invoked by <see cref="ScenarioElement"/> Start method notifying scenario about new element
        /// </summary>
        /// <param name="scenarioElement">Scenario element that was just activated</param>
        public void NewElementActivated(ScenarioElement scenarioElement)
        {
            NewScenarioElement?.Invoke(scenarioElement);
        }
    }
}