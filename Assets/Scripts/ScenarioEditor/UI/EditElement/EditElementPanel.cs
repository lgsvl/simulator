/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors
{
    using System.Collections.Generic;
    using Elements;
    using Inspector;
    using Managers;
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

        /// <summary>
        /// Transform parent for the panel content
        /// </summary>
        [SerializeField]
        private Transform contentParent;

        /// <summary>
        /// Info that is displayed when there is no scenario element selected
        /// </summary>
        [SerializeField]
        private GameObject noElementInfo;
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
                if (prefab == null)
                    continue;
                var panel = Instantiate(prefab, contentParent);
                panel.Initialize();
                panels.Add(panel);
            }

            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            for (var i = 0; i < panels.Count; i++)
                panels[i].Deinitialize();
            panels.Clear();
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
                scenarioManager.SelectedOtherElement -= OnSelectedOtherElement;
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

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="previousElement">Scenario element that has been deselected</param>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement previousElement, ScenarioElement selectedElement)
        {
            noElementInfo.SetActive(selectedElement == null);
        }
    }
}