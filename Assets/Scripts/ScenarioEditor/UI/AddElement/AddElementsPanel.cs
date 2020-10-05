/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.AddElement
{
    using System.Collections;
    using Controllables;
    using Inspector;
    using Managers;
    using ScenarioEditor.Utilities;
    using UnityEngine;

    /// <summary>
    /// UI panel which allows adding new agents to the scenario
    /// </summary>
    public class AddElementsPanel : InspectorContentPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Agent source panel prefab, which represents a single agent type for adding
        /// </summary>
        [SerializeField]
        private SourcePanel sourcePanelPrefab;
        
        /// <summary>
        /// Transform parent for the panel content
        /// </summary>
        [SerializeField]
        private Transform contentParent;
#pragma warning restore 0649

        /// <inheritdoc/>
        public override void Initialize()
        {
            SourcePanel newPanel;
            //Agents panels
            var agentsManager = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>();
            var sources = agentsManager.Sources;
            for (var i = 0; i < sources.Count; i++)
            {
                newPanel = Instantiate(sourcePanelPrefab, contentParent);
                newPanel.Initialize(sources[i]);
            }

            //Controllables panels
            newPanel = Instantiate(sourcePanelPrefab, contentParent);
            var controllablesManager = ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>();
            newPanel.Initialize(controllablesManager.source);
            UnityUtilities.LayoutRebuild(contentParent as RectTransform);
        }
        
        /// <inheritdoc/>
        public override void Deinitialize()
        {
            
        }

        /// <inheritdoc/>
        public override void Show()
        {
            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(contentParent as RectTransform);
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}