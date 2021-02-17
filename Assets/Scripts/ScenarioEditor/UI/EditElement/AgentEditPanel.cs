/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Agents;
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Input;
    using Managers;
    using ScenarioEditor.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// UI panel which allows editing a scenario agent parameters
    /// </summary>
    public class AgentEditPanel : ParameterEditPanel, IAddElementsHandler
    {
        /// <summary>
        /// Type of the element that is currently being added to the agent
        /// </summary>
        private enum AgentElementType
        {
            /// <summary>
            /// Adding mode was not enabled
            /// </summary>
            None,

            /// <summary>
            /// Adding new waypoints to the agent
            /// </summary>
            Waypoints,
        }

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Dropdown for the agent variant selection
        /// </summary>
        [SerializeField]
        private Dropdown variantDropdown;

        /// <summary>
        /// Dropdown for the agent behaviour selection
        /// </summary>
        [SerializeField]
        private Dropdown behaviourDropdown;

        /// <summary>
        /// Dropdown for the agent sensors configuration selection
        /// </summary>
        [SerializeField]
        private Dropdown sensorsConfigurationDropdown;

        /// <summary>
        /// Panel which contains UI for editing the agent waypoints
        /// </summary>
        [SerializeField]
        private GameObject waypointsPanel;

        /// <summary>
        /// Panel which contains UI for editing the agent color
        /// </summary>
        [SerializeField]
        private GameObject colorPanel;

        /// <summary>
        /// Image that represents color of the agent
        /// </summary>
        [SerializeField]
        private Image colorImage;

        /// <summary>
        /// Panel for editing agent's destination point
        /// </summary>
        [SerializeField]
        private GameObject destinationPointPanel;

        /// <summary>
        /// Panel for editing an active destination point
        /// </summary>
        [SerializeField]
        private GameObject activeDestinationPointPanel;

        /// <summary>
        /// Toggle for the agent's destination point
        /// </summary>
        [SerializeField]
        private Toggle destinationPointToggle;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Type of the element that is currently being added to the agent
        /// </summary>
        private AgentElementType addedElementType;

        /// <summary>
        /// New element instance that is currently being added to the scenario
        /// </summary>lo
        private ScenarioElement newElementInstance;

        /// <summary>
        /// Cached agent source which variants are currently available in the dropdown
        /// </summary>
        private ScenarioAgentSource agentSource;

        /// <summary>
        /// Currently edited scenario agent reference
        /// </summary>
        private ScenarioAgent selectedAgent;

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            isInitialized = true;
            OnSelectedOtherElement(ScenarioManager.Instance.SelectedElement);
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
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement selectedElement)
        {
            //Detach from current agent events
            if (selectedAgent != null)
            {
                selectedAgent.VariantChanged -= SelectedAgentOnVariantChanged;
                selectedAgent.BehaviourChanged -= SelectedAgentOnBehaviourChanged;
                selectedAgent.SensorsConfigurationIdChanged -= SelectedAgentOnSensorsConfigurationIdChanged;
                selectedAgent.ColorChanged -= SelectedAgentOnColorChanged;

                //Hide destination point if agent is deselected and something else is selected
                if (selectedAgent.DestinationPoint != null && selectedElement != selectedAgent.DestinationPoint)
                    selectedAgent.DestinationPoint.SetVisibility(false);
            }

            selectedAgent = selectedElement as ScenarioAgent;
            //Attach to selected agent events
            if (selectedAgent != null)
            {
                Show();
                selectedAgent.VariantChanged += SelectedAgentOnVariantChanged;
                selectedAgent.BehaviourChanged += SelectedAgentOnBehaviourChanged;
                selectedAgent.SensorsConfigurationIdChanged += SelectedAgentOnSensorsConfigurationIdChanged;
                selectedAgent.ColorChanged += SelectedAgentOnColorChanged;
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// Method invoked when selected agent changes the variant
        /// </summary>
        /// <param name="newVariant">Agent new variant</param>
        private void SelectedAgentOnVariantChanged(SourceVariant newVariant)
        {
            var variantId = agentSource.Variants.IndexOf(newVariant);
            variantDropdown.SetValueWithoutNotify(variantId);
            SetupSensorsConfigurationDropdown();
        }

        /// <summary>
        /// Method invoked when selected agent changes the behaviour
        /// </summary>
        /// <param name="newBehaviour">Agent new behaviour</param>
        private void SelectedAgentOnBehaviourChanged(string newBehaviour)
        {
            var behaviourId = agentSource.Behaviours.IndexOf(newBehaviour);
            behaviourDropdown.SetValueWithoutNotify(behaviourId);
            //Disable waypoints panel if waypoints are not supported
            waypointsPanel.SetActive(agentSource.AgentSupportWaypoints(selectedAgent));
        }

        /// <summary>
        /// Method invoked when selected agent changes the sensors configuration id
        /// </summary>
        /// <param name="newId">Agent new sensors configuration id</param>
        private void SelectedAgentOnSensorsConfigurationIdChanged(string newId)
        {
            if (!(selectedAgent.Variant is EgoAgentVariant egoAgentVariant))
                return;
            var configNo = egoAgentVariant.SensorsConfigurations.FindIndex(c => c.Id == newId);
            sensorsConfigurationDropdown.SetValueWithoutNotify(configNo);
        }

        /// <summary>
        /// Method invoked when selected agent changes the color
        /// </summary>
        /// <param name="newColor">Agent new color</param>
        private void SelectedAgentOnColorChanged(Color newColor)
        {
            if (colorImage != null)
                colorImage.color = newColor;
        }

        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected agent
        /// </summary>
        public void Show()
        {
            //Setup variants
            agentSource = selectedAgent.Source;
            variantDropdown.options.Clear();
            variantDropdown.AddOptions(
                agentSource.Variants.Where(variant => variant.IsPrepared).Select(variant => variant.Name).ToList());
            //Setup behaviour
            if (agentSource.Behaviours != null && agentSource.Behaviours.Count > 0)
            {
                behaviourDropdown.options.Clear();
                behaviourDropdown.AddOptions(agentSource.Behaviours);
                behaviourDropdown.gameObject.SetActive(true);
            }
            else
            {
                behaviourDropdown.gameObject.SetActive(false);
            }

            //Setup sensors config
            SetupSensorsConfigurationDropdown();

            //Disable waypoints panel if waypoints are not supported
            waypointsPanel.SetActive(agentSource.AgentSupportWaypoints(selectedAgent));

            var variantId = agentSource.Variants.IndexOf(selectedAgent.Variant);
            variantDropdown.SetValueWithoutNotify(variantId);
            if (agentSource.Behaviours != null)
            {
                var behaviourId = string.IsNullOrEmpty(selectedAgent.Behaviour)
                    ? 0
                    : agentSource.Behaviours.IndexOf(selectedAgent.Behaviour);
                behaviourDropdown.SetValueWithoutNotify(behaviourId);
                behaviourDropdown.RefreshShownValue();
            }

            colorPanel.SetActive(selectedAgent.SupportColors);
            if (selectedAgent.SupportColors)
                colorImage.color = selectedAgent.AgentColor;
            var supportsDestinationPoint = selectedAgent.DestinationPoint != null;
            if (supportsDestinationPoint)
            {
                var active = selectedAgent.DestinationPoint.IsActive;
                destinationPointToggle.SetIsOnWithoutNotify(active);
                activeDestinationPointPanel.SetActive(active);
                selectedAgent.DestinationPoint.SetVisibility(active);
            }

            destinationPointPanel.SetActive(supportsDestinationPoint);

            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Hides the panel and clears current agent
        /// </summary>
        public void Hide()
        {
            if (addedElementType != AgentElementType.None)
                ScenarioManager.Instance.GetExtension<InputManager>().CancelAddingElements(this);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Setups the sensors configuration dropdown basing on the currently selected agent
        /// </summary>
        private void SetupSensorsConfigurationDropdown()
        {
            if (selectedAgent.Variant is EgoAgentVariant egoAgentVariant && egoAgentVariant.SensorsConfigurations.Count>0)
            {
                sensorsConfigurationDropdown.options.Clear();
                var selectedNo = 0;
                for (var i = 0; i < egoAgentVariant.SensorsConfigurations.Count; i++)
                {
                    var configuration = egoAgentVariant.SensorsConfigurations[i];
                    sensorsConfigurationDropdown.options.Add(new Dropdown.OptionData(configuration.Name));
                    if (configuration.Id == selectedAgent.SensorsConfigurationId)
                        selectedNo = i;
                }
                sensorsConfigurationDropdown.SetValueWithoutNotify(selectedNo);
                sensorsConfigurationDropdown.RefreshShownValue();
                sensorsConfigurationDropdown.gameObject.SetActive(true);
            }
            else
            {
                sensorsConfigurationDropdown.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Method changing the variant of the currently selected scenario agent
        /// </summary>
        /// <param name="variantId">Variant identifier in the source</param>
        public void VariantDropdownChanged(int variantId)
        {
            ChangeVariant(agentSource.Variants[variantId]);
        }

        /// <summary>
        /// Changes variant of the selected vehicle
        /// </summary>
        /// <param name="variant">Variant that will be applied to the vehicle</param>
        /// <returns>Task</returns>
        private void ChangeVariant(SourceVariant variant)
        {
            ScenarioManager.Instance.colorPicker.Hide();
            selectedAgent.ChangeVariant(variant);
        }

        /// <summary>
        /// Method changing the behaviour of the currently selected scenario agent
        /// </summary>
        /// <param name="behaviourId">Behaviour identifier in the source</param>
        public void BehaviourDropdownChanged(int behaviourId)
        {
            selectedAgent.ChangeBehaviour(agentSource.Behaviours[behaviourId]);
        }

        /// <summary>
        /// Method changing the sensors configuration of the currently selected scenario agent
        /// </summary>
        /// <param name="configNo">Order number of the selected sensors configuration</param>
        public void SensorsConfigurationDropdownChanged(int configNo)
        {
            if (selectedAgent.Variant is EgoAgentVariant egoAgentVariant)
                selectedAgent.ChangeSensorsConfigurationId(egoAgentVariant.SensorsConfigurations[configNo].Id);
        }

        /// <summary>
        /// Invokes adding new waypoints
        /// </summary>
        public void AddWaypoints()
        {
            if (selectedAgent == null)
                return;
            addedElementType = AgentElementType.Waypoints;
            if (!ScenarioManager.Instance.GetExtension<InputManager>().StartAddingElements(this))
                addedElementType = AgentElementType.None;
        }

        /// <summary>
        /// Shows the color picker to change the selected agent color
        /// </summary>
        public void EditColor()
        {
            if (!selectedAgent.SupportColors)
                return;
            var colorPicker = ScenarioManager.Instance.colorPicker;
            var previousColor = selectedAgent.AgentColor;
            colorPicker.Show(selectedAgent.AgentColor,
                color => selectedAgent.AgentColor = color,
                () => ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new UndoChangeColor(selectedAgent, previousColor)));
        }

        /// <summary>
        /// Sets destination point as active or inactive if it is supported by selected agent
        /// </summary>
        /// <param name="active">Should the destination point be active</param>
        public void ToggleSetDestinationPoint(bool active)
        {
            if (selectedAgent.DestinationPoint == null) return;
            var agent = selectedAgent;
            var undoCallback = new Action<bool>((undoValue) => { SetDestinationPoint(agent, undoValue); });
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoToggle(destinationPointToggle, selectedAgent.DestinationPoint.IsActive,
                    undoCallback));
            SetDestinationPoint(selectedAgent, active);
        }

        /// <summary>
        /// Sets destination point as active or inactive if it is supported by selected agent
        /// </summary>
        /// <param name="agent">Agent owning changed destination point</param>
        /// <param name="active">Should the destination point be active</param>
        private void SetDestinationPoint(ScenarioAgent agent, bool active)
        {
            agent.DestinationPoint.SetActive(active);
            var isSelected = agent == selectedAgent;
            agent.DestinationPoint.SetVisibility(isSelected && active);
            activeDestinationPointPanel.SetActive(isSelected && active);
        }

        /// <summary>
        /// Moves the scenario camera to the destination point
        /// </summary>
        public void MoveCameraToDestinationPoint()
        {
            var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var raycastHitsInCenter =
                inputManager.RaycastAll(inputManager.ScenarioCamera.ViewportPointToRay(new Vector3(0.35f, 0.5f, 0.5f)));
            if (raycastHitsInCenter.Length == 0)
                return;
            var furthestHit = inputManager.GetFurthestHit(raycastHitsInCenter, raycastHitsInCenter.Length, true);
            if (!furthestHit.HasValue)
                return;
            var cameraTransform = inputManager.ScenarioCamera.transform;
            var destinationPointPosition = selectedAgent.DestinationPoint.transform.position;
            var offset = furthestHit.Value.point - cameraTransform.position;
            inputManager.ForceCameraReposition(destinationPointPosition - offset, cameraTransform.rotation.eulerAngles);
            ScenarioManager.Instance.SelectedElement = selectedAgent.DestinationPoint;
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingStarted(Vector3 addPosition)
        {
            switch (addedElementType)
            {
                case AgentElementType.Waypoints:
                    var mapWaypointPrefab =
                        ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().waypointPrefab;
                    newElementInstance = ScenarioManager.Instance.prefabsPools
                        .GetInstance(mapWaypointPrefab).GetComponent<ScenarioWaypoint>();
                    if (newElementInstance == null)
                    {
                        Debug.LogWarning(
                            $"Cannot add waypoints. Add {nameof(ScenarioWaypoint)} component to the prefab.");
                        return;
                    }

                    newElementInstance.ForceMove(addPosition);
                    selectedAgent.AddWaypoint(newElementInstance as ScenarioWaypoint, true);
                    break;
            }
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingMoved(Vector3 addPosition)
        {
            newElementInstance.ForceMove(addPosition);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddElement(Vector3 addPosition)
        {
            switch (addedElementType)
            {
                case AgentElementType.Waypoints:
                    ScenarioManager.Instance.IsScenarioDirty = true;
                    ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                        .RegisterRecord(new UndoAddElement(newElementInstance));
                    var mapWaypointPrefab =
                        ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().waypointPrefab;
                    newElementInstance = ScenarioManager.Instance.prefabsPools
                        .GetInstance(mapWaypointPrefab).GetComponent<ScenarioWaypoint>();
                    newElementInstance.ForceMove(addPosition);
                    selectedAgent.AddWaypoint(newElementInstance as ScenarioWaypoint, true);
                    break;
            }
        }

        /// <inheritdoc/>
        public void AddingCancelled(Vector3 addPosition)
        {
            if (newElementInstance.CanBeRemoved)
            {
                newElementInstance.RemoveFromMap();
                newElementInstance.Dispose();
            }

            newElementInstance = null;

            addedElementType = AgentElementType.None;
        }
    }
}