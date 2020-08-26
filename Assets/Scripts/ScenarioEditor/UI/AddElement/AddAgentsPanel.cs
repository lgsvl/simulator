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
    public class AddAgentsPanel : InspectorContentPanel
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
        public override void Initialize()
        {
            var sources = ScenarioManager.Instance.agentsManager.Sources;
            for (var i = 0; i < sources.Count; i++)
            {
                var newPanel = Instantiate(agentSourcePanelPrefab, transform);
                newPanel.Initialize(sources[i]);
            }
        }
        
        /// <inheritdoc/>
        public override void Deinitialize()
        {
            
        }

        /// <inheritdoc/>
        public override void Show()
        {
            gameObject.SetActive(true);
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}