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
    using Input;
    using Managers;
    using ScenarioEditor.Utilities;
    using Simulator.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Edit panel for the agent's waypoints
    /// </summary>
    public class AgentWaypointsEditPanel : ParameterEditPanel, IAddElementsHandler
    {
        /// <summary>
        /// Toggle for switching loop options
        /// </summary>
        [SerializeField]
        private Toggle loopToggle;

        /// <summary>
        /// Dropdown that allows selecting the waypoints path type
        /// </summary>
        [SerializeField]
        private Dropdown pathTypeDropdown;

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Is currently adding new waypoints
        /// </summary>
        private bool isAddingWaypoints;

        /// <summary>
        /// Waypoints that is edited by this panel
        /// </summary>
        private AgentWaypoints agentWaypoints;

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
            OnSelectedOtherElement(ScenarioManager.Instance.SelectedElement);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            if (!isInitialized)
                return;
            pathTypeDropdown.ClearOptions();
            pathTypeEnums.Clear();
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
                scenarioManager.SelectedOtherElement -= OnSelectedOtherElement;
            isInitialized = false;
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement selectedElement)
        {
            if (agentWaypoints != null)
            {
                agentWaypoints.IsActiveChanged -= AgentWaypointsOnIsActiveChanged;
            }

            selectedAgent = selectedElement as ScenarioAgent;
            //Attach to selected agent events
            if (selectedAgent != null)
            {
                agentWaypoints = selectedAgent.GetExtension<AgentWaypoints>();
                if (agentWaypoints == null)
                    Hide();
                else
                {
                    agentWaypoints.IsActiveChanged += AgentWaypointsOnIsActiveChanged;
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
            gameObject.SetActive(true);
            pathTypeDropdown.SetValueWithoutNotify(pathTypeEnums.IndexOf(agentWaypoints.PathType));
            loopToggle.SetIsOnWithoutNotify(agentWaypoints.Loop);
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
        /// Method invoked when the parent waypoints extension is active property has changed
        /// </summary>
        /// <param name="isActive">Is parent waypoints extension active</param>
        private void AgentWaypointsOnIsActiveChanged(bool isActive)
        {
            if (isActive)
                Show();
            else
                Hide();
        }

        /// <summary>
        /// Invokes adding new waypoints
        /// </summary>
        public void AddWaypoints()
        {
            if (selectedAgent == null)
                return;
            ScenarioManager.Instance.GetExtension<InputManager>().StartAddingElements(this);
        }

        /// <summary>
        /// Changes the waypoints path type
        /// </summary>
        /// <param name="dropdownOption">Selected dropdown option</param>
        public void ChangePathType(int dropdownOption)
        {
            var previousPathType = agentWaypoints.PathType;
            var undoCallback = new Action<WaypointsPathType>(prev =>
            {
                agentWaypoints.ChangePathType(previousPathType);
                pathTypeDropdown.SetValueWithoutNotify(pathTypeEnums.IndexOf(agentWaypoints.PathType));
            });
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new GenericUndo<WaypointsPathType>(previousPathType, "Reverting path type selection",
                    undoCallback));
            agentWaypoints.ChangePathType(pathTypeEnums[dropdownOption]);
        }

        /// <summary>
        /// Changes the waypoint loop option
        /// </summary>
        /// <param name="value">Loop option value</param>
        public void ChangeLoopValue(bool value)
        {
            if (agentWaypoints.Loop == value)
                return;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoToggle(loopToggle, agentWaypoints.Loop,
                    v => agentWaypoints.Loop = v));
            agentWaypoints.Loop = value;
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingStarted(Vector3 addPosition)
        {
            var mapWaypointPrefab =
                ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().waypointPrefab;
            newWaypointInstance = ScenarioManager.Instance.prefabsPools
                .GetInstance(mapWaypointPrefab).GetComponent<ScenarioWaypoint>();
            if (newWaypointInstance == null)
            {
                Debug.LogWarning(
                    $"Cannot add waypoints. Add {nameof(ScenarioWaypoint)} component to the prefab.");
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);
                return;
            }

            newWaypointInstance.ForceMove(addPosition);
            agentWaypoints.AddWaypoint(newWaypointInstance, true);
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
            ScenarioManager.Instance.IsScenarioDirty = true;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoAddElement(newWaypointInstance));
            var mapWaypointPrefab =
                ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().waypointPrefab;
            newWaypointInstance = ScenarioManager.Instance.prefabsPools
                .GetInstance(mapWaypointPrefab).GetComponent<ScenarioWaypoint>();
            newWaypointInstance.ForceMove(addPosition);
            agentWaypoints.AddWaypoint(newWaypointInstance, true);
        }

        /// <inheritdoc/>
        public void AddingCancelled(Vector3 addPosition)
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