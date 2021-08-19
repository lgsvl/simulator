/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Waypoints
{
    using Agents;
    using Data.Serializer;
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Elements.Waypoints;
    using Input;
    using Managers;
    using ScenarioEditor.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Utilities;

    /// <summary>
    /// UI panel which allows editing a selected scenario waypoint
    /// </summary>
    public class ScenarioAgentWaypointEditPanel : ParameterEditPanel, IAddElementsHandler
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Panel with all UI objects for editing speed
        /// </summary>
        [SerializeField]
        private GameObject speedPanel;
        
        /// <summary>
        /// Panel with all UI objects for editing acceleration
        /// </summary>
        [SerializeField]
        private GameObject accelerationPanel;

        /// <summary>
        /// Panel with all UI objects for editing wait time
        /// </summary>
        [SerializeField]
        private GameObject waitTimePanel;

        /// <summary>
        /// Input field for editing wait time
        /// </summary>
        [SerializeField]
        private InputField waitTimeInput;

        /// <summary>
        /// Input field with units for editing speed
        /// </summary>
        [SerializeField]
        private FloatInputWithUnits speedInput;

        /// <summary>
        /// Input field with units for editing acceleration
        /// </summary>
        [SerializeField]
        private FloatInputWithUnits accelerationInput;

        /// <summary>
        /// Panel for editing waypoint triggers
        /// </summary>
        [SerializeField]
        private TriggerEditPanel triggerEditPanel;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Is this panel currently adding new waypoints to the scenario
        /// </summary>
        private bool isAddingWaypoints;

        /// <summary>
        /// Waypoint instance that is currently being added to the scenario
        /// </summary>
        private ScenarioAgentWaypoint waypointInstance;

        /// <summary>
        /// Reference to currently selected agent
        /// </summary>
        private ScenarioAgent selectedAgent;

        /// <summary>
        /// Waypoints extension of currently selected agent
        /// </summary>
        private AgentWaypointsPath selectedAgentWaypointsPath;

        /// <summary>
        /// Reference to currently selected waypoint
        /// </summary>
        private ScenarioAgentWaypoint selectedWaypoint;

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        private void OnDisable()
        {
            waitTimeInput.OnDeselect(new BaseEventData(EventSystem.current));
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            var selectedElement = ScenarioManager.Instance.SelectedElement;
            speedInput.Initialize(ScenarioPersistenceKeys.SpeedUnitKey, ChangeWaypointSpeed, selectedElement);
            accelerationInput.Initialize(ScenarioPersistenceKeys.AccelerationUnitKey, ChangeWaypointAcceleration, selectedElement);
            triggerEditPanel.Initialize();
            isInitialized = true;
            OnSelectedOtherElement(null, selectedElement);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            if (!isInitialized)
                return;
            triggerEditPanel.Deinitialize();
            accelerationInput.Deinitialize();
            speedInput.Deinitialize();
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
                scenarioManager.SelectedOtherElement -= OnSelectedOtherElement;
            isInitialized = false;
        }

        /// <summary>
        /// Submits changed input field value
        /// </summary>
        private void SubmitChangedInputs()
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (speedInput.UnityInputField.gameObject == selected)
                speedInput.OnValueInputApply();
            if (accelerationInput.UnityInputField.gameObject == selected)
                accelerationInput.OnValueInputApply();
            if (waitTimeInput.gameObject == selected)
                OnWaypointWaitTimeInputChange(waitTimeInput.text);
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="previousElement">Scenario element that has been deselected</param>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement previousElement, ScenarioElement selectedElement)
        {
            if (isAddingWaypoints)
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);

            //Force input apply on deselect
            if (selectedWaypoint != null)
                SubmitChangedInputs();
            selectedWaypoint = selectedElement as ScenarioAgentWaypoint;
            selectedAgent = selectedWaypoint != null ? (ScenarioAgent) selectedWaypoint.ParentElement : null;
            selectedAgentWaypointsPath =
                selectedAgent == null ? null : selectedAgent.GetExtension<AgentWaypointsPath>();
            //Disable waypoints for ego vehicles
            if (selectedAgent == null || selectedAgentWaypointsPath == null ||
                !selectedAgent.Source.AgentSupportWaypoints(selectedAgent))
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                speedPanel.SetActive(selectedWaypoint != null);
                accelerationPanel.SetActive(selectedWaypoint != null);
                waitTimePanel.SetActive(selectedWaypoint != null);
                if (selectedWaypoint != null)
                {
                    speedInput.CurrentContext = selectedWaypoint;
                    speedInput.ExternalValueChange(selectedWaypoint.DestinationSpeed, selectedWaypoint, false);
                    accelerationInput.CurrentContext = selectedWaypoint;
                    accelerationInput.ExternalValueChange(selectedWaypoint.Acceleration, selectedWaypoint, false);
                    waitTimeInput.text = selectedWaypoint.WaitTime.ToString("F");
                }

                triggerEditPanel.OnSelectedNewTrigger(selectedWaypoint.LinkedTrigger);
                UnityUtilities.LayoutRebuild(transform as RectTransform);
            }
        }

        /// <summary>
        /// Invokes adding new waypoints
        /// </summary>
        public void Add()
        {
            if (selectedAgent != null)
                ScenarioManager.Instance.GetExtension<InputManager>().StartAddingElements(this);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingStarted(Vector3 addPosition)
        {
            if (selectedAgent == null)
            {
                Debug.LogWarning("Cannot add waypoints if no agent or waypoint is selected.");
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);
                return;
            }

            isAddingWaypoints = true;

            waypointInstance = ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>()
                .GetWaypointInstance<ScenarioAgentWaypoint>();
            if (waypointInstance == null)
            {
                Debug.LogWarning("Cannot add waypoints. Add waypoint component to the prefab.");
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);
                ScenarioManager.Instance.prefabsPools.ReturnInstance(waypointInstance.gameObject);
                return;
            }

            waypointInstance.transform.position = addPosition;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (selectedAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }

            selectedAgentWaypointsPath.AddWaypoint(waypointInstance, selectedWaypoint);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingMoved(Vector3 addPosition)
        {
            waypointInstance.transform.position = addPosition;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (selectedAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }

            selectedAgentWaypointsPath.WaypointPositionChanged(waypointInstance);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddElement(Vector3 addPosition)
        {
            var previousWaypoint = waypointInstance;
            waypointInstance = ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>()
                    .GetWaypointInstance<ScenarioAgentWaypoint>();
            waypointInstance.transform.position = addPosition;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (selectedAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }

            selectedAgentWaypointsPath.AddWaypoint(waypointInstance, previousWaypoint);
            ScenarioManager.Instance.IsScenarioDirty = true;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoAddElement(previousWaypoint));
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingCancelled(Vector3 addPosition)
        {
            if (waypointInstance != null)
            {
                waypointInstance.RemoveFromMap();
                waypointInstance.Dispose();
            }

            waypointInstance = null;
            isAddingWaypoints = false;
        }

        /// <summary>
        /// Changes the currently selected waypoint speed
        /// </summary>
        /// <param name="changedElement">Scenario element which speed has been changed</param>
        /// <param name="mpsSpeed">Speed value in meters per second</param>
        private void ChangeWaypointSpeed(ScenarioElement changedElement, float mpsSpeed)
        {
            if (!(changedElement is ScenarioAgentWaypoint changedWaypoint))
                return;
            ScenarioManager.Instance.IsScenarioDirty = true;
            changedWaypoint.DestinationSpeed = mpsSpeed;
        }

        /// <summary>
        /// Changes the currently selected waypoint acceleration
        /// </summary>
        /// <param name="changedElement">Scenario element which acceleration has been changed</param>
        /// <param name="mps2Acceleration">Acceleration value in meters per square second</param>
        private void ChangeWaypointAcceleration(ScenarioElement changedElement, float mps2Acceleration)
        {
            if (!(changedElement is ScenarioAgentWaypoint changedWaypoint))
                return;
            ScenarioManager.Instance.IsScenarioDirty = true;
            changedWaypoint.Acceleration = mps2Acceleration;
        }

        /// <summary>
        /// Changes the currently selected waypoint wait time and registers an undo record 
        /// </summary>
        /// <param name="waitTimeString">Wait time value in the string format</param>
        public void OnWaypointWaitTimeInputChange(string waitTimeString)
        {
            if (selectedWaypoint == null || !float.TryParse(waitTimeString, out var waitTime)) return;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoInputField(
                waitTimeInput, selectedWaypoint.WaitTime.ToString("F"), ChangeWaypointWaitTime));
            ChangeWaypointWaitTime(waitTime);
        }

        /// <summary>
        /// Changes the currently selected waypoint wait time
        /// </summary>
        /// <param name="waitTimeString">Wait time value in the string format</param>
        private void ChangeWaypointWaitTime(string waitTimeString)
        {
            if (selectedWaypoint == null || !float.TryParse(waitTimeString, out var waitTime)) return;
            ChangeWaypointWaitTime(waitTime);
        }

        /// <summary>
        /// Changes the currently selected waypoint wait time
        /// </summary>
        /// <param name="waitTime">Wait time value</param>
        private void ChangeWaypointWaitTime(float waitTime)
        {
            ScenarioManager.Instance.IsScenarioDirty = true;
            selectedWaypoint.WaitTime = waitTime;
        }
    }
}