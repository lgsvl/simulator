/**
 * Copyright (c) 2020 LG Electronics, Inc.
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
    using Data.Deserializer;
    using Data.Serializer;
    using Input;
    using Inspector;
    using Managers;
    using MapSelecting;
    using SimpleJSON;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;
    using Toggle = UnityEngine.UI.Toggle;

    /// <summary>
    /// UI panel for options, loading and saving scenario, managing scenario editor
    /// </summary>
    public class FileEditPanel : MonoBehaviour, IInspectorContentPanel
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
#pragma warning restore 0649

        /// <inheritdoc/>
        public string MenuItemTitle => "File";

        /// <inheritdoc/>
        void IInspectorContentPanel.Initialize()
        {
            var inputManager = ScenarioManager.Instance.inputManager;
            UpdateCameraModeText();
            snapToLanesToggle.SetIsOnWithoutNotify(ScenarioManager.Instance.MapManager.LaneSnapping.SnappingEnabled);
            invertedXRotationToggle.SetIsOnWithoutNotify(inputManager.InvertedXRotation);
            invertedYRotationToggle.SetIsOnWithoutNotify(inputManager.InvertedYRotation);
        }
        
        /// <inheritdoc/>
        void IInspectorContentPanel.Deinitialize()
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
            ScenarioManager.Instance.ShowLoadingPanel();
            LoadPath.Value = path;
            var json = JSONNode.Parse(File.ReadAllText(path));
            if (json != null && json.IsObject)
                await JsonScenarioDeserializer.DeserializeScenario(json);
            ScenarioManager.Instance.HideLoadingPanel();
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
            var scenario = JsonScenarioSerializer.SerializeScenario();
            File.WriteAllText(path, scenario.ScenarioData.ToString());
            ScenarioManager.Instance.IsScenarioDirty = false;
            ScenarioManager.Instance.logPanel.EnqueueInfo($"Scenario has been saved to the file: '{path}'.");
        }

        /// <summary>
        /// Opens <see cref="SelectFileDialog"/> and exports the scenario to Python API script
        /// </summary>
        public void ExportPythonApi()
        {
            ScenarioManager.Instance.selectFileDialog.Show(ExportPythonApi, true, ExportPythonPath.Value,
                "Export Scenario To Python Script", "Export To File", new[] {"py"});
        }

        /// <summary>
        /// Exports the scenario to Python API script in the path
        /// </summary>
        /// <param name="path">Path to the Python API script where scenario will be exported</param>
        private void ExportPythonApi(string path)
        {
            path = Path.ChangeExtension(path, ".py");
            ExportPythonPath.Value = path;
            var scenario = PythonScenarioSerializer.SerializeScenario();
            File.WriteAllText(path, scenario.ScenarioData);
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
                    ScenarioManager.Instance.ShowLoadingPanel();
                    //Delay exiting editor so the loading panel can initialize
                    StartCoroutine(DelayedExitEditor());
                };

                ScenarioManager.Instance.confirmationPopup.Show(popupData);
            }
            else
            {
                ScenarioManager.Instance.ShowLoadingPanel();
                //Delay exiting editor so the loading panel can initialize
                StartCoroutine(DelayedExitEditor());
            }
        }

        /// <summary>
        /// Invokes visual scenario editor exit after a single frame update
        /// </summary>
        /// <returns>IEnumerator</returns>
        private IEnumerator DelayedExitEditor()
        {
            yield return null;
            Loader.ExitScenarioEditor();
        }

        /// <inheritdoc/>
        void IInspectorContentPanel.Show()
        {
            gameObject.SetActive(true);
        }

        /// <inheritdoc/>
        void IInspectorContentPanel.Hide()
        {
            gameObject.SetActive(false);
        }


        /// <summary>
        /// Updates the camera mode change button's label according to current camera mode
        /// </summary>
        private void UpdateCameraModeText()
        {
            var inputManager = ScenarioManager.Instance.inputManager;
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
            var inputManager = ScenarioManager.Instance.inputManager;
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
            ScenarioManager.Instance.MapManager.LaneSnapping.SnappingEnabled = value;
        }

        /// <summary>
        /// Changes the rotation X inversion setting
        /// </summary>
        /// <param name="value">Current value for the rotation X inversion setting</param>
        public void ChangeRotationXInversion(bool value)
        {
            ScenarioManager.Instance.inputManager.InvertedXRotation = value;
        }

        /// <summary>
        /// Changes the rotation Y inversion setting
        /// </summary>
        /// <param name="value">Current value for the rotation Y inversion setting</param>
        public void ChangeRotationYInversion(bool value)
        {
            ScenarioManager.Instance.inputManager.InvertedYRotation = value;
        }
    }
}