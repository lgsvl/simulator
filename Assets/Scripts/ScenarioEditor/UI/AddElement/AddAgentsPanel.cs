/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.AddElement
{
    using Inspector;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// UI panel which allows adding new agents to the scenario
    /// </summary>
    public class AddAgentsPanel : MonoBehaviour, IInspectorContentPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Agent source panel prefab, which represents a single agent type for adding
        /// </summary>
        [SerializeField]
        private AgentSourcePanel agentSourcePanelPrefab;
#pragma warning restore 0649

        /// <inheritdoc/>
        public string MenuItemTitle => "Add";

        /// <inheritdoc/>
        void IInspectorContentPanel.Initialize()
        {
            var sources = ScenarioManager.Instance.agentsManager.Sources;
            for (var i = 0; i < sources.Count; i++)
            {
                var newPanel = Instantiate(agentSourcePanelPrefab, transform);
                newPanel.Initialize(sources[i]);
            }
        }
        
        /// <inheritdoc/>
        void IInspectorContentPanel.Deinitialize()
        {
            
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
    }
}