/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors
{
    using System.Collections.Generic;
    using Inspector;
    using ScenarioEditor.Utilities;
    using UnityEngine;

    /// <summary>
    /// UI panel which allows editing currently selected scenario element
    /// </summary>
    public class EditElementPanel : InspectorContentPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Dropdown for the agent variant selection
        /// </summary>
        [SerializeField]
        private List<ParameterEditPanel> panelsPrefabs = new List<ParameterEditPanel>();
#pragma warning restore 0649

        /// <summary>
        /// Available parameter edit panels
        /// </summary>
        private List<ParameterEditPanel> panels = new List<ParameterEditPanel>();

        /// <inheritdoc/>
        public override void Initialize()
        {
            for (var i = 0; i < panelsPrefabs.Count; i++)
            {
                var prefab = panelsPrefabs[i];
                var panel = Instantiate(prefab, transform);
                panel.Initialize();
                panels.Add(panel);
            }
        }
        
        /// <inheritdoc/>
        public override void Deinitialize()
        {
            for (var i = 0; i < panels.Count; i++)
                panels[i].Deinitialize();
            panels.Clear();
        }

        /// <inheritdoc/>
        public override void Show()
        {
            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}