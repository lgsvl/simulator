/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
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