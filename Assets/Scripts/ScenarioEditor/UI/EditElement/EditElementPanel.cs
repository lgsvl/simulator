/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
    using System.Collections.Generic;
    using Inspector;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// UI panel which allows editing currently selected scenario element
    /// </summary>
    public class EditElementPanel : MonoBehaviour, IInspectorContentPanel
    {
        /// <inheritdoc/>
        public string MenuItemTitle => "Edit";
        
        /// <summary>
        /// Available parameter edit panels
        /// </summary>
        private List<IParameterEditPanel> panels = new List<IParameterEditPanel>();

        /// <inheritdoc/>
        void IInspectorContentPanel.Initialize()
        {
            var availablePanels = gameObject.GetComponentsInChildren<IParameterEditPanel>(true);
            for (var i = 0; i < availablePanels.Length; i++)
            {
                var availablePanel = availablePanels[i];
                availablePanel.Initialize();
                panels.Add(availablePanel);
            }
        }
        
        /// <inheritdoc/>
        void IInspectorContentPanel.Deinitialize()
        {
            for (var i = 0; i < panels.Count; i++)
                panels[i].Deinitialize();
            panels.Clear();
        }

        /// <inheritdoc/>
        void IInspectorContentPanel.Show()
        {
            gameObject.SetActive(true);
            UIUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <inheritdoc/>
        void IInspectorContentPanel.Hide()
        {
            gameObject.SetActive(false);
        }
    }
}