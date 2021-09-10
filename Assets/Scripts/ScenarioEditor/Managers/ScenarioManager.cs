/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Data;
    using Elements;
    using Input;
    using SimpleJSON;
    using Simulator.Utilities;
    using UI.ColorPicker;
    using UI.FileEdit;
    using UI.Inspector;
    using UI.MapSelecting;
    using UI.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Utilities;
    using Web;

    /// <summary>
    /// Scenario editor manager connecting all other components
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        /// <summary>
        /// Possible state of the manager initialization
        /// </summary>
        public enum InitializationState
        {
            /// <summary>
            /// Manager is deinitialized and cannot be used
            /// </summary>
            Deinitialized = 0,

            /// <summary>
            /// Manager is during the initialization process
            /// </summary>
            Initializing = 1,

            /// <summary>
            /// Manager is initialized and ready to be used
            /// </summary>
            Initialized = 2,

            /// <summary>
            /// Manage is during the deinitialization process
            /// </summary>
            Deinitializing = 3
        }

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
        /// Prefabs that will be instantiated inside this editor object
        /// </summary>
        [SerializeField]
        private List<GameObject> extensions;

        /// <summary>
        /// Camera used to render the scenario world
        /// </summary>
        [SerializeField]
        private Camera scenarioCamera;

        /// <summary>
        /// Lights object that is instantiated on the loaded map
        /// </summary>
        [SerializeField]
        private GameObject lightsPrefab;
#pragma warning restore 0649

        /// <summary>
        /// Available scenario editor extensions for the visual scenario editor
        /// </summary>
        private readonly Dictionary<Type, IScenarioEditorExtension> scenarioEditorExtensions =
            new Dictionary<Type, IScenarioEditorExtension>();

        /// <summary>
        /// List of all assets being downloaded
        /// </summary>
        private readonly List<string> assetsBeingDownloaded = new List<string>();

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
                if (selectedElement != null)
                    selectedElement.Deselected();
                var previousElement = selectedElement;
                selectedElement = value;
                if (selectedElement != null)
                    selectedElement.Selected();
                SelectedOtherElement?.Invoke(previousElement, selectedElement);
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
        public InitializationState State { get; private set; }

        /// <summary>
        /// Event invoked when the scenario manager finishes the initialization
        /// </summary>
        public event Action Initialized;

        /// <summary>
        /// Event invoked when the scenario is being reset
        /// </summary>
        public event Action ScenarioReset;

        /// <summary>
        /// Event invoked when the new scenario element is activated in scenario
        /// </summary>
        public event Action<ScenarioElement> ScenarioElementActivated;

        /// <summary>
        /// Event invoked when the new scenario element is deactivated in scenario
        /// </summary>
        public event Action<ScenarioElement> ScenarioElementDeactivated;

        /// <summary>
        /// Event invoked when the selected scenario element changes
        /// First parameter is deselected scenario element
        /// Second parameter is new selected scenario element
        /// </summary>
        public event Action<ScenarioElement, ScenarioElement> SelectedOtherElement;

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
            if (State != InitializationState.Deinitialized)
                return;
            State = InitializationState.Initializing;
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
                    Debug.Log($"Loading the VSE extension: {scenarioManager.GetType().Name}.");
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
                    Debug.Log($"Loading the VSE extension: {scenarioManagerType.Name}.");
                    var go = new GameObject(scenarioManagerType.Name, scenarioManagerType);
                    go.transform.SetParent(transform);
                    var scenarioManager = go.GetComponent(scenarioManagerType) as IScenarioEditorExtension;
                    tasks[i++] = scenarioManager?.Initialize();
                    scenarioEditorExtensions.Add(scenarioManagerType, scenarioManager);
                }
                else
                {
                    Debug.Log($"Loading the VSE extension: {scenarioManagerType.Name}.");
                    var scenarioManager = Activator.CreateInstance(scenarioManagerType) as IScenarioEditorExtension;
                    tasks[i++] = scenarioManager?.Initialize();
                    scenarioEditorExtensions.Add(scenarioManagerType, scenarioManager);
                }
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                StopInitialization();
                return;
            }

            //Initialize map
            var mapManager = GetExtension<ScenarioMapManager>();
            mapManager.MapChanged += OnMapLoaded;
            try
            {
                await mapManager.LoadMapAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while loading map in the VSE: {ex.Message}.");
                StopInitialization();
                return;
            }

            //Initialize the inspector
            try
            {
                inspector.Initialize();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while loading the VSE inspector: {ex.Message}.");
                StopInitialization();
                return;
            }

            loadingProcess.Update("Visual Scenario Editor has been loaded.");
            loadingProcess.NotifyCompletion();
            State = InitializationState.Initialized;
            Initialized?.Invoke();
        }

        /// <summary>
        /// Stops the scenario manager initialization and exits the scenario editor
        /// </summary>
        private void StopInitialization()
        {
            if (State == InitializationState.Initializing)
                Deinitialize();
            Loader.Instance.ExitScenarioEditor();
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        private void Deinitialize()
        {
            if (State == InitializationState.Deinitialized || State == InitializationState.Deinitializing)
                return;
            State = InitializationState.Deinitializing;
            selectedElement = null;
            if (inspector != null)
                inspector.Deinitialize();
            foreach (var scenarioManager in scenarioEditorExtensions)
                scenarioManager.Value.Deinitialize();
            GetExtension<ScenarioMapManager>().MapChanged -= OnMapLoaded;
            foreach (var assetBeingDownloaded in assetsBeingDownloaded)
            {
                DownloadManager.StopAssetDownload(assetBeingDownloaded);
            }

            assetsBeingDownloaded.Clear();
            State = InitializationState.Deinitialized;
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
            var type = typeof(T);
            if (scenarioEditorExtensions.TryGetValue(type, out var extension))
                return extension as T;
            return null;
        }

        /// <summary>
        /// Returns list of scenario editor extensions that are assignable to given type
        /// </summary>
        /// <typeparam name="T">Requested scenario editor extension implemented type</typeparam>
        /// <returns>List of scenario editor extensions that are assignable to given type</returns>
        public List<T> GetExtensions<T>()
        {
            var requestedExtensions = new List<T>();
            foreach (var editorExtension in scenarioEditorExtensions)
            {
                if (typeof(T).IsAssignableFrom(editorExtension.Key))
                    requestedExtensions.Add((T) editorExtension.Value);
            }

            return requestedExtensions;
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
            if (scenarioElementCopy != null && registerUndo)
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
        /// <param name="scene">The loaded map scene</param>
        public void OnMapLoaded(ScenarioMapManager.MapMetaData mapMetaData)
        {
            var spawnInfo = FindObjectOfType<SpawnInfo>();
            ScenarioCamera.transform.position = (spawnInfo == null ? Vector3.zero : spawnInfo.transform.position) +
                                                new Vector3(0.0f, 30.0f, 0.0f);
            var lights = Instantiate(lightsPrefab);
            lights.SetActive(true);
        }

        /// <summary>
        /// Invoked by <see cref="ScenarioElement"/> Start method notifying scenario about activated element
        /// </summary>
        /// <param name="scenarioElement">Scenario element that was just activated</param>
        public void ReportActivatedElement(ScenarioElement scenarioElement)
        {
            ScenarioElementActivated?.Invoke(scenarioElement);
        }

        /// <summary>
        /// Invoked by <see cref="ScenarioElement"/> Start method notifying scenario about deactivated element
        /// </summary>
        /// <param name="scenarioElement">Scenario element that was just deactivated</param>
        public void ReportDeactivatedElement(ScenarioElement scenarioElement)
        {
            ScenarioElementDeactivated?.Invoke(scenarioElement);
        }

        /// <summary>
        /// Serializes current scenario state into a json scenario
        /// </summary>
        /// <returns>Json scenario with serialized scenario</returns>
        public JsonScenario SerializeScenario()
        {
            var scenarioData = new JSONObject();
            scenarioData.Add("version", new JSONString("0.01"));

            var serializedExtensions = ScenarioManager.Instance.GetExtensions<ISerializedExtension>();
            foreach (var serializedExtension in serializedExtensions)
            {
                try
                {
                    var serializationResult = serializedExtension.Serialize(scenarioData);
                    if (!serializationResult)
                    {
                        logPanel.EnqueueError(
                            $"Error occured while serializing {serializedExtension.GetType().Name} extension.");
                        return null;
                    }
                }
                catch (Exception exception)
                {
                    logPanel.EnqueueError(
                        $"Error occured while serializing {serializedExtension.GetType().Name} extension. Error message: {exception.Message}.");
                    return null;
                }
            }

            return new JsonScenario(scenarioData);
        }

        /// <summary>
        /// Adds visual scenario editor metadata to the json scenario
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        private static void SerializeMetadata(JSONNode data)
        {
            var vseMetadata = new JSONObject();
            data.Add("vseMetadata", vseMetadata);
            var cameraSettings = new JSONObject();
            vseMetadata.Add("cameraSettings", cameraSettings);
            var camera = ScenarioManager.Instance.ScenarioCamera;
            var position = new JSONObject().WriteVector3(camera.transform.position);
            cameraSettings.Add("position", position);
            var rotation = new JSONObject().WriteVector3(camera.transform.rotation.eulerAngles);
            cameraSettings.Add("rotation", rotation);
        }

        /// <summary>
        /// Deserializes and loads scenario from the given json data
        /// </summary>
        /// <param name="data">Json data with the scenario</param>
        public async Task<bool> DeserializeScenario(JSONNode data)
        {
            var serializedExtensions = ScenarioManager.Instance.GetExtensions<ISerializedExtension>();
            foreach (var serializedExtension in serializedExtensions)
            {
                try
                {
                    var deserializationResult = await serializedExtension.Deserialize(data);
                    if (!deserializationResult)
                    {
                        logPanel.EnqueueError(
                            $"Error occured while deserializing {serializedExtension.GetType().Name} extension.");
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    logPanel.EnqueueError(
                        $"Error occured while deserializing {serializedExtension.GetType().Name} extension. Error message: {exception.Message.TrimEnd('.')}.");
                    return false;
                }
            }

            DeserializeMetadata(data);
            return true;
        }

        /// <summary>
        /// Deserializes scenario meta data from the json data
        /// </summary>
        /// <param name="data">Json object with the metadata</param>
        private static void DeserializeMetadata(JSONNode data)
        {
            var vseMetadata = data["vseMetadata"];
            if (vseMetadata == null)
                vseMetadata = data["vse_metadata"];
            if (vseMetadata == null)
                return;
            var cameraSettings = vseMetadata["cameraSettings"];
            if (cameraSettings == null)
                cameraSettings = vseMetadata["camera_settings"];
            if (cameraSettings == null)
                return;
            var position = cameraSettings["position"];
            var camera = ScenarioManager.Instance.ScenarioCamera;
            var rotation = cameraSettings.HasKey("rotation")
                ? cameraSettings["rotation"].ReadVector3()
                : camera.transform.rotation.eulerAngles;
            Instance.GetExtension<InputManager>().ForceCameraReposition(position, rotation);
        }

        /// <summary>
        /// Checks if asset with given guid is being downloaded
        /// </summary>
        /// <param name="assetGuid">Checked asset guid</param>
        /// <returns>True if asset is being downloaded, false otherwise</returns>
        public bool DownloadsAsset(string assetGuid)
        {
            return assetsBeingDownloaded.Contains(assetGuid);
        }

        /// <summary>
        /// Reports asset guid that starts downloading
        /// </summary>
        /// <param name="assetGuid">Guid of the asset that starts downloading</param>
        public void ReportAssetDownload(string assetGuid)
        {
            if (!DownloadsAsset(assetGuid))
                assetsBeingDownloaded.Add(assetGuid);
        }

        /// <summary>
        /// Reports that asset of given guid finishes downloading
        /// </summary>
        /// <param name="assetGuid">Guid of the asset that finishes downloading</param>
        public void ReportAssetFinishedDownload(string assetGuid)
        {
            assetsBeingDownloaded.Remove(assetGuid);
        }
    }
}