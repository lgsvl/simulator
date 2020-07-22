/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
    using System;
    using System.Collections.Generic;
    using Agents;
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
        /// <summary>
        /// Unit type used for editing speed
        /// </summary>
        public enum SpeedUnitType
        {
            MetersPerSecond = 0,
            KilometersPerHour = 1,
            MilesPerHour = 2
        }

        /// <summary>
        /// Persistence data key for the selected speed unit path
        /// </summary>
        private static string SpeedUnitPath = "Simulator/ScenarioEditor/WaypointEditPanel/SelectedSpeedUnit";

        /// <summary>
        /// Currently selected speed unit
        /// </summary>
        private static SpeedUnitType CurrentSpeedUnit = SpeedUnitType.KilometersPerHour;

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
        /// Input field for editing speed
        /// </summary>
        [SerializeField]
        private InputField speedInput;

        /// <summary>
        /// Input field for editing wait time
        /// </summary>
        [SerializeField]
        private InputField waitTimeInput;

        /// <summary>
        /// Dropdown for changing the speed unit
        /// </summary>
        [SerializeField]
        private Dropdown speedUnitDropdown;
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
            CurrentSpeedUnit = (SpeedUnitType) PlayerPrefs.GetInt(SpeedUnitPath, 0);
            speedUnitDropdown.options.Clear();
            var options = new List<string>();
            options.Add("m/s");
            options.Add("kph");
            options.Add("mph");
            speedUnitDropdown.AddOptions(options);
            isInitialized = true;
            OnSelectedOtherElement(ScenarioManager.Instance.SelectedElement);
        }
        
        /// <inheritdoc/>
        void IParameterEditPanel.Deinitialize()
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
                    speedUnitDropdown.SetValueWithoutNotify((int) CurrentSpeedUnit);
                    SpeedUnitDropdownChanged((int) CurrentSpeedUnit);
                    waitTimeInput.text = selectedWaypoint.WaitTime.ToString("F");
                }

                UIUtilities.LayoutRebuild(transform as RectTransform);
            }
        }

        /// <summary>
        /// Method changing the variant of the currently selected scenario agent
        /// </summary>
        /// <param name="unitId">Speed unit type id</param>
        public void SpeedUnitDropdownChanged(int unitId)
        {
            CurrentSpeedUnit = (SpeedUnitType) unitId;
            PlayerPrefs.SetInt(SpeedUnitPath, unitId);
            speedInput.text = ConvertFromMps(selectedWaypoint.Speed, CurrentSpeedUnit).ToString("F");
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
            selectedAgent.AddWaypoint(waypointInstance, selectedWaypoint);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingMoved(Vector3 addPosition)
        {
            waypointInstance.transform.position = addPosition;
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
        /// Converts the source unit type speed value to meters per second
        /// </summary>
        /// <param name="value">Speed value in the source unit type</param>
        /// <param name="sourceUnit">Source speed unit type</param>
        /// <returns>Converted meters per second</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid source speed unit type</exception>
        private float ConvertToMps(float value, SpeedUnitType sourceUnit)
        {
            switch (sourceUnit)
            {
                case SpeedUnitType.MetersPerSecond:
                    return value;
                case SpeedUnitType.KilometersPerHour:
                    return value * 1000.0f / 3600.0f;
                case SpeedUnitType.MilesPerHour:
                    return value * 1609.344f / 3600.0f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sourceUnit), sourceUnit, null);
            }
        }

        /// <summary>
        /// Converts the meters per second to target speed unit type
        /// </summary>
        /// <param name="mpsValue">Speed in meters per second</param>
        /// <param name="targetUnit">Target speed unit type</param>
        /// <returns>Converted speed to target unit type</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid target speed unit type</exception>
        private float ConvertFromMps(float mpsValue, SpeedUnitType targetUnit)
        {
            switch (targetUnit)
            {
                case SpeedUnitType.MetersPerSecond:
                    return mpsValue;
                case SpeedUnitType.KilometersPerHour:
                    return mpsValue * 3600.0f / 1000.0f;
                case SpeedUnitType.MilesPerHour:
                    return mpsValue * 3600.0f / 1609.344f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetUnit), targetUnit, null);
            }
        }

        /// <summary>
        /// Changes the currently selected waypoint speed
        /// </summary>
        /// <param name="speedString">Speed value in string</param>
        private void ChangeWaypointSpeed(string speedString)
        {
            if (selectedWaypoint != null && float.TryParse(speedString, out var speed))
                ChangeWaypointSpeed(ConvertToMps(speed, CurrentSpeedUnit));
        }

        /// <summary>
        /// Changes the currently selected waypoint speed
        /// </summary>
        /// <param name="mpsSpeed">Speed value in meters per second</param>
        private void ChangeWaypointSpeed(float mpsSpeed)
        {
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