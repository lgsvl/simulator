/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Inspector
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Single menu item in the inspector bar for switching between content panels
    /// </summary>
    public class InspectorMenuItem : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Parent <see cref="inspectorMenu"/> where the show command will be passed
        /// </summary>
        [SerializeField]
        private InspectorMenu inspectorMenu;

        /// <summary>
        /// Text object displaying the menu item name
        /// </summary>
        [SerializeField]
        private Text nameText;
#pragma warning restore 0649

        /// <summary>
        /// Corresponding content panel that will be shown when this menu item is pressed
        /// </summary>
        private IInspectorContentPanel panel;

        /// <summary>
        /// Setups the menu item according to passed panel data
        /// </summary>
        /// <param name="panel">Content panel that will be corresponding to this menu item</param>
        public void Setup(IInspectorContentPanel panel)
        {
            this.panel = panel;
            nameText.text = panel.MenuItemTitle;
        }

        /// <summary>
        /// Shows the corresponding content panel in the parent inspector menu
        /// </summary>
        public void ShowPanel()
        {
            inspectorMenu.ShowPanel(panel);
        }
    }
}