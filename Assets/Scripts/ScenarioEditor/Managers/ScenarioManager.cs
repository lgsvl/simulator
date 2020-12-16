/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Elements;
    using Input;
    using Simulator.Utilities;
    using UI.ColorPicker;
    using UI.FileEdit;
    using UI.Inspector;
    using UI.MapSelecting;
    using UI.Utilities;
    using Undo;
    using Undo.Records;
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
        /// Prefabs that will be instantiated inside this editor object
        /// </summary>
        [SerializeField]
        private List<GameObject> extensions;
#pragma warning restore 0649

        /// <summary>
        /// Is the manager initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Available scenario editor extensions for the visual scenario editor
        /// </summary>
        private readonly Dictionary<Type, IScenarioEditorExtension> scenarioEditorExtensions =
            new Dictionary<Type, IScenarioEditorExtension>();

        /// <summary>
        /// Is there a single popup visible in the scenario editor
        /// </summary>
        private bool viewsPopup;

        /// <summary>
        /// Currently selected scenario element
        /// </summary>
        private ScenarioElement selectedElement;

        /// <summary>
        /// Inspector menu used in the scenario editor
        /// </summary>
        public Inspector inspector;
        
        /// <summary>
        /// Pooling mechanism for prefabs in the visual scenario editor
        /// </summary>
        public PrefabsPools prefabsPools;

        /// <summary>
        /// Panel which allows selecting a custom color
        /// </summary>
        public ColorPicker colorPicker;

        /// <summary>
        /// Shared <see cref="SelectFileDialog"/> to be used in the scenario editor, dialog can handle only one request at same time
        /// </summary>
        public SelectFileDialog selectFileDialog;

        /// <summary>
        /// Popup which requires user interaction to confirm an operation
        /// </summary>
        public ConfirmationPopup confirmationPopup;

        /// <summary>
        /// The loading panel reference
        /// </summary>
        public LoadingPanel loadingPanel;

        /// <summary>
        /// Log panel for displaying message to the user for a limited time
        /// </summary>
        public LogPanel logPanel;

        /// <summary>
        /// Camera used to render the scenario world
        /// </summary>
        public Camera ScenarioCamera => scenarioCamera;

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
                if (selectedElement!=null)
                    selectedElement.Deselected();
                selectedElement = value;
                if (selectedElement != null)
                    selectedElement.Selected();
                SelectedOtherElement?.Invoke(selectedElement);
            }
        }

        /// <summary>
        /// Scenario element that is copied and will be cloned on demand
        /// </summary>
        public ScenarioElement CopiedElement { get; private set; }

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
                    GetExtension<InputManager>().InputSemaphore.Lock();
                else
                    GetExtension<InputManager>().InputSemaphore.Unlock();
            }
        }

        /// <summary>
        /// Is scenario dirty, true if there are some unsaved changes
        /// </summary>
        public bool IsScenarioDirty { get; set; }

        /// <summary>
        /// Is the manager initialized
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Event invoked when the scenario is being reset
        /// </summary>
        public event Action ScenarioReset;

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
            Deinitialize();
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        private async Task Initialize()
        {
            if (IsInitialized)
                return;
            isInitialized = true;
            var loadingProcess = loadingPanel.AddProgress();

            //Initialize all the scenario editor extensions
            var managersTypes = ReflectionCache.FindTypes(type =>
                typeof(IScenarioEditorExtension).IsAssignableFrom(type) && !type.IsAbstract);
            loadingProcess.Update("Loading the Visual Scenario Editor.");
            var tasks = new Task[managersTypes.Count];
            var i = 0;
            foreach (var extensionPrefab in extensions)
            {
                var addon = Instantiate(extensionPrefab, transform);
                var scenarioManager = addon.GetComponent<IScenarioEditorExtension>();
                if (scenarioManager != null)
                {
                    var type = scenarioManager.GetType();
                    managersTypes.Remove(type);
                    tasks[i++] = scenarioManager.Initialize();
                    scenarioEditorExtensions.Add(type, scenarioManager);
                }
            }

            foreach (var scenarioManagerType in managersTypes)
            {
                if (scenarioManagerType.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    var go = new GameObject(scenarioManagerType.Name, scenarioManagerType);
                    go.transform.SetParent(transform);
                    var scenarioManager = go.GetComponent(scenarioManagerType) as IScenarioEditorExtension;
                    tasks[i++] = scenarioManager?.Initialize();
                    scenarioEditorExtensions.Add(scenarioManagerType, scenarioManager);
                }
                else
                {
                    var scenarioManager = Activator.CreateInstance(scenarioManagerType) as IScenarioEditorExtension;
                    tasks[i++] = scenarioManager?.Initialize();
                    scenarioEditorExtensions.Add(scenarioManagerType, scenarioManager);
                }
            }
            await Task.WhenAll(tasks);
            
            //Initialize map
            var mapManager = GetExtension<ScenarioMapManager>();
            mapManager.MapChanged += OnMapLoaded;
            await mapManager.LoadMapAsync();
            inspector.Initialize();
            loadingProcess.Update("Visual Scenario Editor has been loaded.");
            loadingProcess.NotifyCompletion();
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        private void Deinitialize()
        {
            if (!IsInitialized)
                return;
            selectedElement = null;
            if (inspector!=null)
                inspector.Deinitialize();
            foreach (var scenarioManager in scenarioEditorExtensions)
                scenarioManager.Value.Deinitialize();
            GetExtension<ScenarioMapManager>().MapChanged -= OnMapLoaded;
            isInitialized = false;
        }

        /// <summary>
        /// Checks if the scenario editor extension of given type is ready
        /// </summary>
        /// <typeparam name="T">Type of the scenario editor extension</typeparam>
        /// <returns>Is the scenario editor extension of given type ready</returns>
        public bool IsExtensionReady<T>() where T : class, IScenarioEditorExtension
        {
            return scenarioEditorExtensions.TryGetValue(typeof(T), out var scenarioManager) &&
                   scenarioManager.IsInitialized;
        }
        
        /// <summary>
        /// Waits until extension is ready to be used
        /// </summary>
        /// <typeparam name="T">Type of the scenario editor extension</typeparam>
        /// <returns>Asynchronous task waiting for the extension to be ready</returns>
        public async Task WaitForExtension<T>() where T : class, IScenarioEditorExtension
        {
            while (!IsExtensionReady<T>())
                await Task.Delay(25);
        }

        /// <summary>
        /// Returns a scenario editor extension of the requested type created for this editor
        /// </summary>
        /// <typeparam name="T">Type of the scenario editor extension</typeparam>
        /// <returns>Scenario editor extension of the requested type created for this editor</returns>
        public T GetExtension<T>() where T : class, IScenarioEditorExtension
        {
            return scenarioEditorExtensions[typeof(T)] as T;
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
            ScenarioReset?.Invoke();
            IsScenarioDirty = false;
            GetExtension<ScenarioUndoManager>().ClearRecords();
        }

        /// <summary>
        /// Creates an element clone with current state so it can be duplicated later
        /// </summary>
        public void CopyElement(ScenarioElement element)
        {
            if (CopiedElement != null)
                CopiedElement.Dispose();
            CopiedElement = element;
            CopiedElement = PlaceElementCopy(element.transform.position, false);
            CopiedElement.RemoveFromMap();
            CopiedElement.gameObject.SetActive(false);
        }

        /// <summary>
        /// Places an element copy on the map
        /// </summary>
        /// <param name="position">Position where the copy should be placed</param>
        /// <param name="registerUndo">Should this operation be registered in the undo manager as add element record</param>
        public ScenarioElement PlaceElementCopy(Vector3 position, bool registerUndo = true)
        {
            if (CopiedElement == null)
                return null;
            var copy = prefabsPools.Clone(CopiedElement.gameObject);
            copy.SetActive(true);
            var scenarioElementCopy = copy.GetComponent<ScenarioElement>();
            if (scenarioElementCopy!=null && registerUndo)
                GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoAddElement(scenarioElementCopy));
            copy.transform.position = position;
            //Reposition all scenario elements on ground, do not snap while repositioning
            var scenarioElements = copy.GetComponentsInChildren<ScenarioElement>(true);
            var mapManager = GetExtension<ScenarioMapManager>();
            var snap = mapManager.LaneSnapping.SnappingEnabled;
            mapManager.LaneSnapping.SnappingEnabled = false;
            foreach (var scenarioElement in scenarioElements)
                scenarioElement.RepositionOnGround();
            mapManager.LaneSnapping.SnappingEnabled = snap;
            IsScenarioDirty = true;
            return scenarioElementCopy;
        }

        /// <summary>
        /// Method called when new map has been loaded, resets the camera position and hides loading panel
        /// </summary>
        /// <param name="mapMetaData">The loaded map meta data</param>
        public void OnMapLoaded(ScenarioMapManager.MapMetaData mapMetaData)
        {
            var spawnInfo = FindObjectOfType<SpawnInfo>();
            ScenarioCamera.transform.position = (spawnInfo == null ? Vector3.zero : spawnInfo.transform.position)+new Vector3(0.0f, 30.0f, 0.0f);
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