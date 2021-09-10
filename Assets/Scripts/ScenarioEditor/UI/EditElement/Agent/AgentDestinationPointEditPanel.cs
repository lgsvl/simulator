/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Agent
{
    using System;
    using System.Collections.Generic;
    using Agents;
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Elements.Waypoints;
    using Input;
    using Managers;
    using ScenarioEditor.Utilities;
    using Simulator.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Edit panel for the agent's destination point
    /// </summary>
    public class AgentDestinationPointEditPanel : ParameterEditPanel, IAddElementsHandler
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Toggle for the agent's destination point
        /// </summary>
        [SerializeField]
        private Toggle destinationPointToggle;

        /// <summary>
        /// Toggle for the destination point's playback path visibility
        /// </summary>
        [SerializeField]
        private Toggle playbackPathToggle;

        /// <summary>
        /// Dropdown that allows selecting the waypoints path type
        /// </summary>
        [SerializeField]
        private Dropdown pathTypeDropdown;

        /// <summary>
        /// Game objects shown for editing an active destination point
        /// </summary>
        [SerializeField]
        private List<GameObject> shownWhenActive;

        /// <summary>
        /// Game objects for editing an playback path
        /// </summary>
        [SerializeField]
        private List<GameObject> shownWithPlaybackPath;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Is currently adding new waypoints
        /// </summary>
        private bool isAddingWaypoints;

        /// <summary>
        /// Destination point extension that is edited by this panel
        /// </summary>
        private AgentDestinationPoint destinationPointExtension;

        /// <summary>
        /// Currently edited scenario agent reference
        /// </summary>
        private ScenarioAgent selectedAgent;

        /// <summary>
        /// New waypoint instance that is currently being added to the scenario
        /// </summary>lo
        private ScenarioWaypoint newWaypointInstance;

        /// <summary>
        /// Precached list of available waypoints path type enums
        /// </summary>
        private readonly List<WaypointsPathType> pathTypeEnums = new List<WaypointsPathType>();

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            pathTypeDropdown.ClearOptions();
            pathTypeEnums.Clear();
            var enumValues = Enum.GetValues(typeof(WaypointsPathType));
            var options = new List<string>();
            foreach (var enumValue in enumValues)
            {
                var pathType = (WaypointsPathType) enumValue;
                pathTypeEnums.Add(pathType);
                options.Add(pathType.ToString());
            }

            pathTypeDropdown.AddOptions(options);

            isInitialized = true;
            OnSelectedOtherElement(null, ScenarioManager.Instance.SelectedElement);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            if (!isInitialized)
                return;
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
                scenarioManager.SelectedOtherElement -= OnSelectedOtherElement;
            isInitialized = false;
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="previousElement">Scenario element that has been deselected</param>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement previousElement, ScenarioElement selectedElement)
        {
            //Detach from current agent events
            if (destinationPointExtension != null && selectedAgent == previousElement)
            {
                //Hide destination point if something else than the destination point is selected
                if (!(selectedElement is ScenarioDestinationPoint ||
                      selectedElement is ScenarioDestinationPointWaypoint) &&
                    destinationPointExtension.DestinationPoint != null)
                    destinationPointExtension.DestinationPoint.SetVisibility(false);
                destinationPointExtension = null;
            }

            selectedAgent = selectedElement as ScenarioAgent;
            //Attach to selected agent events
            if (selectedAgent != null)
            {
                destinationPointExtension = selectedAgent.GetExtension<AgentDestinationPoint>();
                if (destinationPointExtension == null)
                    Hide();
                else
                {
                    Show();
                }
            }
            else
            {
                Hide();
            }
        }


        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected agent
        /// </summary>
        public void Show()
        {
            var supportsDestinationPoint = destinationPointExtension.DestinationPoint != null;
            if (supportsDestinationPoint)
            {
                var active = destinationPointExtension.DestinationPoint.IsActive;
                destinationPointToggle.SetIsOnWithoutNotify(active);
                pathTypeDropdown.SetValueWithoutNotify(
                    pathTypeEnums.IndexOf(destinationPointExtension.DestinationPoint.PlaybackPath.PathType));
                foreach (var element in shownWhenActive)
                {
                    element.SetActive(active);
                }

                foreach (var element in shownWithPlaybackPath)
                {
                    element.SetActive(active && destinationPointExtension.DestinationPoint.IsPlaybackPathVisible);
                }

                destinationPointExtension.DestinationPoint.SetVisibility(active);
                UnityUtilities.LayoutRebuild(transform as RectTransform);
            }

            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Hides the panel and clears current agent
        /// </summary>
        public void Hide()
        {
            if (isAddingWaypoints)
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets destination point as active or inactive if it is supported by selected agent
        /// </summary>
        /// <param name="active">Should the destination point be active</param>
        public void SetDestinationPoint(bool active)
        {
            if (destinationPointExtension == null) return;
            var extension = destinationPointExtension;
            var undoCallback = new Action<bool>((undoValue) => { SetDestinationPoint(extension, undoValue); });
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new GenericUndo<bool>(extension.DestinationPoint.IsActive,
                    "Undo toggling a destination point",
                    undoCallback));
            SetDestinationPoint(extension, active);
        }

        /// <summary>
        /// Sets destination point as active or inactive if it is supported by selected agent
        /// </summary>
        /// <param name="extension">Changed destination point extension</param>
        /// <param name="active">Should the destination point be active</param>
        private void SetDestinationPoint(AgentDestinationPoint extension, bool active)
        {
            extension.DestinationPoint.SetActive(active);
            var isSelected = extension == destinationPointExtension;
            extension.DestinationPoint.SetVisibility(isSelected && active);
            if (isSelected)
                destinationPointToggle.SetIsOnWithoutNotify(active);
            foreach (var element in shownWhenActive)
            {
                element.SetActive(isSelected && active);
            }

            SetPlaybackPathVisibility(extension.DestinationPoint.IsPlaybackPathVisible);

            if (isSelected)
                UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Sets playback path visibility
        /// </summary>
        /// <param name="visible">Should the playback path be visible</param>
        public void SetPlaybackPathVisibility(bool visible)
        {
            if (destinationPointExtension == null) return;
            var extension = destinationPointExtension;
            var undoCallback = new Action<bool>((undoValue) => { SetPlaybackPathVisibility(extension, undoValue); });
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new GenericUndo<bool>(extension.DestinationPoint.IsPlaybackPathVisible,
                    "Undo toggling a playback path visibility",
                    undoCallback));
            SetPlaybackPathVisibility(extension, visible);
        }

        /// <summary>
        /// Sets playback path visibility
        /// </summary>
        /// <param name="extension">Changed destination point extension</param>
        /// <param name="visible">Should the playback path be visible</param>
        private void SetPlaybackPathVisibility(AgentDestinationPoint extension, bool visible)
        {
            var isSelected = extension == destinationPointExtension;

            extension.DestinationPoint.SetPlaybackPathVisible(visible);
            extension.DestinationPoint.PlaybackPath.SetActive(visible);
            if (isSelected)
                playbackPathToggle.SetIsOnWithoutNotify(visible);
            foreach (var element in shownWithPlaybackPath)
            {
                element.SetActive(isSelected && visible);
            }

            if (isSelected)
                UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Moves the scenario camera to the destination point
        /// </summary>
        public void MoveCameraToDestinationPoint()
        {
            var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            inputManager.FocusOnScenarioElement(destinationPointExtension.DestinationPoint);
        }

        /// <summary>
        /// Changes the waypoints path type
        /// </summary>
        /// <param name="dropdownOption">Selected dropdown option</param>
        public void ChangePathType(int dropdownOption)
        {
            var path = destinationPointExtension.DestinationPoint.PlaybackPath;
            var previousPathType = path.PathType;
            var undoCallback = new Action<WaypointsPathType>(prev =>
            {
                path.ChangePathType(previousPathType);
                pathTypeDropdown.SetValueWithoutNotify(pathTypeEnums.IndexOf(path.PathType));
            });
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new GenericUndo<WaypointsPathType>(previousPathType, "Reverting path type selection",
                    undoCallback));
            path.ChangePathType(pathTypeEnums[dropdownOption]);
        }

        /// <summary>
        /// Invokes adding new playback waypoints
        /// </summary>
        public void AddPlaybackWaypoints()
        {
            if (selectedAgent == null)
                return;
            ScenarioManager.Instance.GetExtension<InputManager>().StartAddingElements(this);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingStarted(Vector3 addPosition)
        {
            var path = destinationPointExtension.DestinationPoint.PlaybackPath;
            newWaypointInstance = path.GetWaypointInstance();
            if (newWaypointInstance == null)
            {
                Debug.LogWarning(
                    $"Cannot add waypoints. Add {nameof(ScenarioAgentWaypoint)} component to the prefab.");
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);
                return;
            }

            newWaypointInstance.ForceMove(addPosition);
            path.AddWaypoint(newWaypointInstance, null);
            isAddingWaypoints = true;
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingMoved(Vector3 addPosition)
        {
            newWaypointInstance.ForceMove(addPosition);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddElement(Vector3 addPosition)
        {
            var path = destinationPointExtension.DestinationPoint.PlaybackPath;
            ScenarioManager.Instance.IsScenarioDirty = true;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoAddElement(newWaypointInstance));
            newWaypointInstance = path.GetWaypointInstance();
            newWaypointInstance.ForceMove(addPosition);
            path.AddWaypoint(newWaypointInstance, null);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingCancelled(Vector3 addPosition)
        {
            if (newWaypointInstance.CanBeRemoved)
            {
                newWaypointInstance.RemoveFromMap();
                newWaypointInstance.Dispose();
            }

            newWaypointInstance = null;
            isAddingWaypoints = false;
        }
    }
}