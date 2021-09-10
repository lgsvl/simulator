/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.FileEdit
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Threading.Tasks;
    using Input;
    using Inspector;
    using Managers;
    using MapSelecting;
    using ScenarioEditor.Utilities;
    using SimpleJSON;
    using Undo;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;
    using Toggle = UnityEngine.UI.Toggle;

    /// <summary>
    /// UI panel for options, loading and saving scenario, managing scenario editor
    /// </summary>
    public class FileEditPanel : InspectorContentPanel
    {
        /// <summary>
        /// Common persistence data key for all the paths
        /// </summary>
        private const string PathsKey = "Simulator/ScenarioEditor/FileEdit/";

        /// <summary>
        /// Persistence path pointing the last selected load directory
        /// </summary>
        private static readonly PersistencePath LoadPath = new PersistencePath(PathsKey + "Load");

        /// <summary>
        /// Persistence path pointing the last selected save directory
        /// </summary>
        private static readonly PersistencePath SavePath = new PersistencePath(PathsKey + "Save");

        /// <summary>
        /// Persistence path pointing the last selected export Python directory
        /// </summary>
        private static readonly PersistencePath ExportPythonPath = new PersistencePath(PathsKey + "ExportPython");

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Label for displaying current camera mode
        /// </summary>
        [SerializeField]
        private Text cameraModeLabel;

        /// <summary>
        /// Toggle for switching snapping elements to lanes
        /// </summary>
        [SerializeField]
        private Toggle snapToLanesToggle;

        /// <summary>
        /// Toggle for switching X rotation inversion
        /// </summary>
        [SerializeField]
        private Toggle invertedXRotationToggle;

        /// <summary>
        /// Toggle for switching Y rotation inversion
        /// </summary>
        [SerializeField]
        private Toggle invertedYRotationToggle;

        /// <summary>
        /// Toggle for enabling and disabling the height occluder scrollbar
        /// </summary>
        [SerializeField]
        private Toggle heightOccluderScrollbarToggle;
#pragma warning restore 0649

        /// <inheritdoc/>
        public override void Initialize()
        {
            var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            UpdateCameraModeText();
            snapToLanesToggle.SetIsOnWithoutNotify(ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnappingEnabled);
            invertedXRotationToggle.SetIsOnWithoutNotify(inputManager.InvertedXRotation);
            invertedYRotationToggle.SetIsOnWithoutNotify(inputManager.InvertedYRotation);
            var inspector = ScenarioManager.Instance.inspector;
            heightOccluderScrollbarToggle.SetIsOnWithoutNotify(inspector.HeightOccluderScroll.IsEnabled);
        }
        
        /// <inheritdoc/>
        public override void Deinitialize()
        {
            
        }

        /// <summary>
        /// Opens <see cref="SelectFileDialog"/> and loads scenario from selected json
        /// </summary>
        public void LoadScenario()
        {
            ScenarioManager.Instance.RequestResetScenario(() =>
            {
                ScenarioManager.Instance.selectFileDialog.Show((path) =>
                    {
                        var nonBlockingTask = LoadScenarioFromJson(path);
                    }, false, LoadPath.Value,
                    "Load Scenario From Json", "Load From File", new[] {"json"});
            }, null);
        }

        /// <summary>
        /// Loads scenario from json available in the path
        /// </summary>
        /// <param name="path">Path to the json file that contains a scenario</param>
        private async Task LoadScenarioFromJson(string path)
        {
            var loadingProcess = ScenarioManager.Instance.loadingPanel.AddProgress();
            loadingProcess.Update("Loading the scenario.");
            LoadPath.Value = path;
            var json = JSONNode.Parse(File.ReadAllText(path));
            if (json != null && json.IsObject)
                await ScenarioManager.Instance.DeserializeScenario(json);
            loadingProcess.Update("Scenario has been loaded.");
            loadingProcess.NotifyCompletion();
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().ClearRecords();
        }

        /// <summary>
        /// Opens <see cref="SelectFileDialog"/> and saves the scenario to selected json
        /// </summary>
        public void SaveScenario()
        {
            ScenarioManager.Instance.selectFileDialog.Show(SaveScenarioToJson, true, SavePath.Value,
                "Save Scenario To Json", "Save To File", new[] {"json"});
        }

        /// <summary>
        /// Saves the scenario to json in the path
        /// </summary>
        /// <param name="path">Path to the json file where scenario will be saved</param>
        private void SaveScenarioToJson(string path)
        {
            path = Path.ChangeExtension(path, ".json");
            SavePath.Value = path;
            var scenario = ScenarioManager.Instance.SerializeScenario();
            File.WriteAllText(path, scenario.ScenarioData.ToString());
            ScenarioManager.Instance.IsScenarioDirty = false;
            ScenarioManager.Instance.logPanel.EnqueueInfo($"Scenario has been saved to the file: '{path}'.");
        }

        /// <summary>
        /// Resets current scenario removing all the scenario elements
        /// </summary>
        public void ResetScenario()
        {
            ScenarioManager.Instance.RequestResetScenario(null, null);
        }

        /// <summary>
        /// Exits the visual scenario editor back to the Loader screen
        /// </summary>
        public void ExitEditor()
        {
            if (ScenarioManager.Instance.IsScenarioDirty)
            {
                var popupData = new ConfirmationPopup.PopupData()
                {
                    Text = "There are unsaved changes in the scenario, do you wish to discard them?"
                };
                popupData.ConfirmCallback += () =>
                {
                    var loadingProcess = ScenarioManager.Instance.loadingPanel.AddProgress();
                    loadingProcess.Update("Deinitializing before exiting the visual scenario editor.");
                    //Delay exiting editor so the loading panel can initialize
                    StartCoroutine(DelayedExitEditor(loadingProcess));
                };

                ScenarioManager.Instance.confirmationPopup.Show(popupData);
            }
            else
            {
                var loadingProcess = ScenarioManager.Instance.loadingPanel.AddProgress();
                loadingProcess.Update("Deinitializing before exiting the visual scenario editor.");
                //Delay exiting editor so the loading panel can initialize
                StartCoroutine(DelayedExitEditor(loadingProcess));
            }
        }

        /// <summary>
        /// Invokes visual scenario editor exit after a single frame update
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator DelayedExitEditor(LoadingPanel.LoadingProcess loadingProcess)
        {
            yield return null;
            loadingProcess.Update("Exiting the visual scenario editor.");
            Loader.Instance.ExitScenarioEditor();
            //Do not turn off loading process - loading panel will be destroyed within the scene
            //loadingProcess.Update("Exited Visual scenario editor.", true);
        }

        /// <inheritdoc/>
        public override void Show()
        {
            gameObject.SetActive(true);
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            gameObject.SetActive(false);
        }


        /// <summary>
        /// Updates the camera mode change button's label according to current camera mode
        /// </summary>
        private void UpdateCameraModeText()
        {
            var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            switch (inputManager.CameraMode)
            {
                case InputManager.CameraModeType.TopDown:
                    cameraModeLabel.text = "Top-down camera";
                    break;
                case InputManager.CameraModeType.Leaned45:
                    cameraModeLabel.text = "Leaned 45° camera";
                    break;
                case InputManager.CameraModeType.Free:
                    cameraModeLabel.text = "Free camera";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Switches current camera mode to the next one
        /// </summary>
        public void ChangeCameraMode()
        {
            var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            inputManager.CameraMode =
                (InputManager.CameraModeType) (((int) inputManager.CameraMode + 1) %
                    ((int) InputManager.CameraModeType.Free + 1));
            UpdateCameraModeText();
        }

        /// <summary>
        /// Changes the snapping to lane setting
        /// </summary>
        /// <param name="value">Current value for the snapping to lane setting</param>
        public void ChangeSnappingToLane(bool value)
        {
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnappingEnabled = value;
        }

        /// <summary>
        /// Changes the rotation X inversion setting
        /// </summary>
        /// <param name="value">Current value for the rotation X inversion setting</param>
        public void ChangeRotationXInversion(bool value)
        {
            ScenarioManager.Instance.GetExtension<InputManager>().InvertedXRotation = value;
        }

        /// <summary>
        /// Changes the rotation Y inversion setting
        /// </summary>
        /// <param name="value">Current value for the rotation Y inversion setting</param>
        public void ChangeRotationYInversion(bool value)
        {
            ScenarioManager.Instance.GetExtension<InputManager>().InvertedYRotation = value;
        }

        /// <summary>
        /// Changes the height occluder scrollbar state
        /// </summary>
        /// <param name="value">Current value for height occluder scrollbar state</param>
        public void ChangeHeightOccluderScrollbarState(bool value)
        {
            ScenarioManager.Instance.inspector.HeightOccluderScroll.IsEnabled = value;
        }
    }
}