/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.AddElement
{
    using System;
    using Elements;
    using ScenarioEditor.Utilities;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Scenario element source panel visualize a scenario element source for adding new elements
    /// </summary>
    public class SourcePanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Title text of this panel
        /// </summary>
        [SerializeField]
        private Text title;

        /// <summary>
        /// Transform containing all the source element panels
        /// </summary>
        [SerializeField]
        private Transform elementsPanel;

        /// <summary>
        /// Prefab for a single source element panel
        /// </summary>
        [SerializeField]
        private SourceElementPanel elementPanelPrefab;
#pragma warning restore 0649

        /// <summary>
        /// Cached scenario element source class which is used for adding new elements from this panel
        /// </summary>
        private ScenarioElementSource source;

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="source">Scenario element source class which will be used for adding new elements from this panel</param>
        public void Initialize(ScenarioElementSource source)
        {
            this.source = source ?? throw new ArgumentException("Cannot initialize source panel with null source.");
            title.text = source.ElementTypeName;

            var contentSizeFitter = GetComponent<ContentSizeFitter>();
            var verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.enabled = false;
            contentSizeFitter.enabled = false;
            
            foreach (var sourceVariant in source.Variants)
            {
                var variantPanel = Instantiate(elementPanelPrefab, elementsPanel);
                variantPanel.Initialize(source, sourceVariant);
            }
            
            //Rebuild the UI layout
            contentSizeFitter.enabled = true;
            verticalLayoutGroup.enabled = true;
        }
    }
}