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
        /// Prefab of the inspector content panel that will be shown when this menu item is pressed
        /// </summary>
        [SerializeField]
        private InspectorContentPanel panelPrefab;
#pragma warning restore 0649
        
        /// <summary>
        /// Parent <see cref="inspector"/> where the show command will be passed
        /// </summary>
        private Inspector inspector;
        
        /// <summary>
        /// Corresponding content panel that will be shown when this menu item is pressed
        /// </summary>
        private InspectorContentPanel panel;

        /// <summary>
        /// Initializes the menu item
        /// </summary>
        /// <param name="inspector">Parent inspector menu of this item</param>
        public void Initialize(Inspector inspector)
        {
            this.inspector = inspector;
            panel = Instantiate(panelPrefab, inspector.Content.transform);
            panel.Initialize();
        }

        /// <summary>
        /// Deinitializes the menu item
        /// </summary>
        public void Deinitialize()
        {
            panel.Deinitialize();
            Destroy(panel);
        }

        /// <summary>
        /// Shows the panel bound to this menu item
        /// </summary>
        public void ShowPanel()
        {
            panel.Show();
        }

        /// <summary>
        /// Hides the panel bound to this menu item
        /// </summary>
        public void HidePanel()
        {
            panel.Hide();
        }

        /// <summary>
        /// Shows the corresponding content panel in the parent inspector menu
        /// </summary>
        public void Pressed()
        {
            inspector.MenuItemSelected(this);
        }

        private void Start()
        {
            if (name == "FilePanelButton")
            {
                GetComponent<Button>().Select();
            }
        }
    }
}