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
    using Utilities;

    /// <summary>
    /// UI panel which allows editing a selected scenario waypoint
    /// </summary>
    public class ScenarioDestinationPointWaypointEditPanel : ParameterEditPanel, IAddElementsHandler
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Panel with all UI objects for editing speed
        /// </summary>
        [SerializeField]
        private GameObject speedPanel;
        
        /// <summary>
        /// Input field with units for editing speed
        /// </summary>
        [SerializeField]
        private FloatInputWithUnits speedInput;
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
        private ScenarioDestinationPointWaypoint waypointInstance;

        /// <summary>
        /// Reference to currently selected agent
        /// </summary>
        private ScenarioDestinationPoint selectedDestinationPoint;

        /// <summary>
        /// Waypoints extension of currently selected destination point
        /// </summary>
        private DestinationPointWaypointsPath selectedWaypointsPath;

        /// <summary>
        /// Reference to currently selected waypoint
        /// </summary>
        private ScenarioDestinationPointWaypoint selectedWaypoint;

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            var selectedElement = ScenarioManager.Instance.SelectedElement;
            speedInput.Initialize(ScenarioPersistenceKeys.SpeedUnitKey, ChangeWaypointSpeed, selectedElement);
            isInitialized = true;
            OnSelectedOtherElement(null, selectedElement);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            if (!isInitialized)
                return;
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

            // Force input apply on deselect
            if (selectedWaypoint != null)
                SubmitChangedInputs();
            selectedWaypoint = selectedElement as ScenarioDestinationPointWaypoint;
            var parentAgent = selectedWaypoint != null ? (ScenarioAgent) selectedWaypoint.ParentElement : null;
            selectedDestinationPoint = parentAgent != null
                ? parentAgent.GetExtension<AgentDestinationPoint>()?.DestinationPoint
                : null;
            selectedWaypointsPath = selectedDestinationPoint != null ? selectedDestinationPoint.PlaybackPath : null;
            
            // Disable panel if it is not supported
            if (selectedDestinationPoint == null || selectedWaypointsPath == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                speedPanel.SetActive(selectedWaypoint != null);
                if (selectedWaypoint != null)
                {
                    speedInput.CurrentContext = selectedWaypoint;
                    speedInput.ExternalValueChange(selectedWaypoint.DestinationSpeed, selectedWaypoint, false);
                }
                UnityUtilities.LayoutRebuild(transform as RectTransform);
            }
        }

        /// <summary>
        /// Invokes adding new waypoints
        /// </summary>
        public void Add()
        {
            if (selectedDestinationPoint != null)
                ScenarioManager.Instance.GetExtension<InputManager>().StartAddingElements(this);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingStarted(Vector3 addPosition)
        {
            if (selectedDestinationPoint == null)
            {
                Debug.LogWarning("Cannot add waypoints if no agent or waypoint is selected.");
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);
                return;
            }

            isAddingWaypoints = true;

            waypointInstance = ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>()
                .GetWaypointInstance<ScenarioDestinationPointWaypoint>();
            if (waypointInstance == null)
            {
                Debug.LogWarning("Cannot add waypoints. Add waypoint component to the prefab.");
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);
                ScenarioManager.Instance.prefabsPools.ReturnInstance(waypointInstance.gameObject);
                return;
            }

            waypointInstance.transform.position = addPosition;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (selectedDestinationPoint.ParentAgent.Type)
            {
                case AgentType.Ego:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }

            selectedWaypointsPath.AddWaypoint(waypointInstance, selectedWaypoint);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingMoved(Vector3 addPosition)
        {
            waypointInstance.transform.position = addPosition;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (selectedDestinationPoint.ParentAgent.Type)
            {
                case AgentType.Ego:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }

            selectedWaypointsPath.WaypointPositionChanged(waypointInstance);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddElement(Vector3 addPosition)
        {
            var previousWaypoint = waypointInstance;
            waypointInstance = ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>()
                    .GetWaypointInstance<ScenarioDestinationPointWaypoint>();
            waypointInstance.transform.position = addPosition;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (selectedDestinationPoint.ParentAgent.Type)
            {
                case AgentType.Ego:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }

            selectedWaypointsPath.AddWaypoint(waypointInstance, previousWaypoint);
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
    }
}