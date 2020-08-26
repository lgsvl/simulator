/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Inspector
{
    using System.Collections.Generic;
    using Network.Core;
    using UnityEngine;

    /// <summary>
    /// Visual scenario editor inspector menu that can switch between different panels
    /// </summary>
    public class InspectorMenu : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Game object where all the instantiated content panels will be stored
        /// </summary>
        [SerializeField]
        private GameObject content;

        /// <summary>
        /// Prefabs of the menu items that will be used in the inspector
        /// </summary>
        [SerializeField]
        private List<InspectorMenuItem> menuItemsPrefabs = new List<InspectorMenuItem>();
#pragma warning restore 0649

        /// <summary>
        /// Currently active inspector panel
        /// </summary>
        private InspectorMenuItem activeMenuItem;
        
        /// <summary>
        /// Available menu items in this inspector
        /// </summary>
        private List<InspectorMenuItem> menuItems = new List<InspectorMenuItem>();

        /// <summary>
        /// Game object where all the instantiated content panels will be stored
        /// </summary>
        public GameObject Content => content;

        /// <summary>
        /// Unity Start method
        /// </summary>
        public void Start()
        {
            for (var i = 0; i < menuItemsPrefabs.Count; i++)
            {
                var menuItem = Instantiate(menuItemsPrefabs[i], transform);
                menuItems.Add(menuItem);
                menuItem.Initialize(this);
                if (i == 0) menuItem.ShowPanel();
                else menuItem.HidePanel();
            }

            activeMenuItem = menuItems.Count > 0 ? menuItems[0] : null;
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        public void OnDestroy()
        {
            for (var i = 0; i < menuItems.Count; i++)
                menuItems[i].Deinitialize();
            menuItems.Clear();
        }

        /// <summary>
        /// Shows selected inspector content panel while hiding previously selected one
        /// </summary>
        /// <param name="panel"></param>
        public void MenuItemSelected(InspectorMenuItem menuItem)
        {
            if (!menuItems.Contains(menuItem))
            {
                Log.Warning("Cannot show inspector panel which is not in the inspector content hierarchy.");
                return;
            }

            activeMenuItem?.HidePanel();
            menuItem.ShowPanel();
            activeMenuItem = menuItem;
        }
    }
}