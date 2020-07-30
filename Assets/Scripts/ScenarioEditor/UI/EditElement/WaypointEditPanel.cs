/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
    using Agents;
    using Data.Serializer;
    using Elements;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;

    /// <summary>
    /// UI panel which allows editing a selected scenario waypoint
    /// </summary>
    public class WaypointEditPanel : MonoBehaviour, IParameterEditPanel, IAddElementsHandler
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Panel with all UI objects for editing speed
        /// </summary>
        [SerializeField]
        private GameObject speedPanel;

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
        private ScenarioWaypoint waypointInstance;

        /// <summary>
        /// Reference to currently selected agent
        /// </summary>
        private ScenarioAgent selectedAgent;

        /// <summary>
        /// Reference to currently selected waypoint
        /// </summary>
        private ScenarioWaypoint selectedWaypoint;

        /// <inheritdoc/>
        void IParameterEditPanel.Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            speedInput.Initialize(ScenarioPersistenceKeys.SpeedUnitKey, ChangeWaypointSpeed);
            isInitialized = true;
            OnSelectedOtherElement(ScenarioManager.Instance.SelectedElement);
        }

        /// <inheritdoc/>
        void IParameterEditPanel.Deinitialize()
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
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement selectedElement)
        {
            if (isAddingWaypoints)
                ScenarioManager.Instance.inputManager.CancelAddingElements(this);

            selectedWaypoint = selectedElement as ScenarioWaypoint;
            selectedAgent = selectedWaypoint != null ? selectedWaypoint.ParentAgent : null;
            //Disable waypoints for ego vehicles
            if (selectedAgent == null || selectedAgent.Source.AgentTypeId == 1)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                speedPanel.SetActive(selectedWaypoint != null);
                waitTimePanel.SetActive(selectedWaypoint != null);
                if (selectedWaypoint != null)
                {
                    speedInput.ExternalValueChange(selectedWaypoint.Speed);
                    waitTimeInput.text = selectedWaypoint.WaitTime.ToString("F");
                }

                UIUtilities.LayoutRebuild(transform as RectTransform);
            }
        }

        /// <summary>
        /// Invokes adding new waypoints
        /// </summary>
        public void Add()
        {
            if (selectedAgent != null)
                ScenarioManager.Instance.inputManager.StartAddingElements(this);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingStarted(Vector3 addPosition)
        {
            if (selectedAgent == null)
            {
                Debug.LogWarning("Cannot add waypoints if no agent or waypoint is selected.");
                ScenarioManager.Instance.inputManager.CancelAddingElements(this);
                return;
            }

            isAddingWaypoints = true;

            var mapWaypointPrefab = ScenarioManager.Instance.waypointsManager.waypointPrefab;
            waypointInstance = ScenarioManager.Instance.prefabsPools.GetInstance(mapWaypointPrefab)
                .GetComponent<ScenarioWaypoint>();
            if (waypointInstance == null)
            {
                Debug.LogWarning("Cannot add waypoints. Add waypoint component to the prefab.");
                ScenarioManager.Instance.inputManager.CancelAddingElements(this);
                ScenarioManager.Instance.prefabsPools.ReturnInstance(waypointInstance.gameObject);
                return;
            }

            waypointInstance.transform.position = addPosition;
            switch (selectedAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }

            selectedAgent.AddWaypoint(waypointInstance, selectedWaypoint);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingMoved(Vector3 addPosition)
        {
            waypointInstance.transform.position = addPosition;
            switch (selectedAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }
            
            selectedAgent.WaypointPositionChanged(waypointInstance);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddElement(Vector3 addPosition)
        {
            var previousWaypoint = waypointInstance;
            var mapWaypointPrefab = ScenarioManager.Instance.waypointsManager.waypointPrefab;
            waypointInstance = ScenarioManager.Instance.prefabsPools.GetInstance(mapWaypointPrefab)
                .GetComponent<ScenarioWaypoint>();
            waypointInstance.transform.position = addPosition;
            switch (selectedAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        waypointInstance.transform);
                    break;
                case AgentType.Pedestrian:
                    ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        waypointInstance.transform);
                    break;
            }
            selectedAgent.AddWaypoint(waypointInstance, previousWaypoint);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingCancelled(Vector3 addPosition)
        {
            if (waypointInstance != null)
                waypointInstance.Remove();
            waypointInstance = null;
            isAddingWaypoints = false;
        }

        /// <summary>
        /// Changes the currently selected waypoint speed
        /// </summary>
        /// <param name="mpsSpeed">Speed value in meters per second</param>
        private void ChangeWaypointSpeed(float mpsSpeed)
        {
            if (selectedWaypoint == null)
                return;
            ScenarioManager.Instance.IsScenarioDirty = true;
            selectedWaypoint.Speed = mpsSpeed;
        }

        /// <summary>
        /// Changes the currently selected waypoint wait time
        /// </summary>
        /// <param name="waitTimeString">Wait time value in the string format</param>
        public void ChangeWaypointWaitTime(string waitTimeString)
        {
            if (selectedWaypoint != null && float.TryParse(waitTimeString, out var value))
            {
                ScenarioManager.Instance.IsScenarioDirty = true;
                selectedWaypoint.WaitTime = value;
            }
        }
    }
}