/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Agents;
    using Elements;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;

    /// <summary>
    /// UI panel which allows editing a scenario agent parameters
    /// </summary>
    public class AgentEditPanel : MonoBehaviour, IParameterEditPanel, IAddElementsHandler
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
        private Dropdown agentSelectDropdown;
        
        /// <summary>
        /// Game object components that will be disabled for ego agent
        /// </summary>
        [SerializeField]
        private List<GameObject> objectsDisabledForEgo = new List<GameObject>();
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
        void IParameterEditPanel.Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
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
            selectedAgent = selectedElement as ScenarioAgent;
            if (selectedAgent == null)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected agent
        /// </summary>
        public void Show()
        {
            //TODO Cache only when the parent EditElementPanel is active
            if (agentSource != selectedAgent.Source)
            {
                agentSource = selectedAgent.Source;
                agentSelectDropdown.options.Clear();
                agentSelectDropdown.AddOptions(
                    agentSource.AgentVariants.Select(variant => variant.name).ToList());
            }

            //Disable some game objects if ego agent is selected
            for (int i = 0; i < objectsDisabledForEgo.Count; i++)
                objectsDisabledForEgo[i].SetActive(agentSource.AgentTypeId != 1);

            var variantId = agentSource.AgentVariants.IndexOf(selectedAgent.Variant);
            agentSelectDropdown.SetValueWithoutNotify(variantId);
            gameObject.SetActive(true);
            UIUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Hides the panel and clears current agent
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            selectedAgent = null;
        }

        /// <summary>
        /// Method changing the variant of the currently selected scenario agent
        /// </summary>
        /// <param name="variantId">Variant identifier in the source</param>
        public void AgentSelectDropdownChanged(int variantId)
        {
            var nonBlockingTask = ChangeVariant(agentSource.AgentVariants[variantId]);
        }

        /// <summary>
        /// Changes variant of the selected vehicle, downloads assets if required
        /// </summary>
        /// <param name="variant">Variant that will be applied to the vehicle</param>
        /// <returns>Task</returns>
        private async Task ChangeVariant(AgentVariant variant)
        {
            if (variant is CloudAgentVariant cloudVariant && cloudVariant.prefab == null)
            {
                ScenarioManager.Instance.ShowLoadingPanel();
                await cloudVariant.DownloadAsset();
                ScenarioManager.Instance.HideLoadingPanel();
            }
            selectedAgent.ChangeVariant(variant);
        }

        /// <summary>
        /// Invokes adding new waypoints
        /// </summary>
        public void AddWaypoints()
        {
            if (selectedAgent == null)
                return;
            addedElementType = AgentElementType.Waypoints;
            if (!ScenarioManager.Instance.inputManager.StartAddingElements(this))
                addedElementType = AgentElementType.None;
        }
        /// <inheritdoc/>
        void IAddElementsHandler.AddingStarted(Vector3 addPosition)
        {
            switch (addedElementType)
            {
                case AgentElementType.Waypoints:
                    var mapWaypointPrefab = ScenarioManager.Instance.waypointsManager.waypointPrefab;
                    newElementInstance = ScenarioManager.Instance.prefabsPools.GetInstance(mapWaypointPrefab)
                        .GetComponent<ScenarioWaypoint>();
                    if (newElementInstance == null)
                    {
                        Debug.LogWarning(
                            $"Cannot add waypoints. Add {nameof(ScenarioWaypoint)} component to the prefab.");
                        return;
                    }

                    newElementInstance.Reposition(addPosition);
                    selectedAgent.AddWaypoint(newElementInstance as ScenarioWaypoint);
                    break;
            }
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingMoved(Vector3 addPosition)
        {
            newElementInstance.Reposition(addPosition);
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddElement(Vector3 addPosition)
        {
            switch (addedElementType)
            {
                case AgentElementType.Waypoints:
                    ScenarioManager.Instance.IsScenarioDirty = true;
                    var mapWaypointPrefab = ScenarioManager.Instance.waypointsManager.waypointPrefab;
                    newElementInstance = ScenarioManager.Instance.prefabsPools.GetInstance(mapWaypointPrefab)
                        .GetComponent<ScenarioWaypoint>();
                    newElementInstance.Reposition(addPosition);
                    selectedAgent.AddWaypoint(newElementInstance as ScenarioWaypoint);
                    break;
            }
        }

        /// <inheritdoc/>
        void IAddElementsHandler.AddingCancelled(Vector3 addPosition)
        {
            if (newElementInstance.CanBeRemoved)
                newElementInstance.Remove();
            newElementInstance = null;

            addedElementType = AgentElementType.None;
        }
    }
}