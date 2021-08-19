/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Inspector
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Network.Core;
    using UnityEngine;
    using UnityEngine.Serialization;
    using Utilities;

    /// <summary>
    /// Visual scenario editor inspector menu that can switch between different panels
    /// </summary>
    public class Inspector : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Game object where all the instantiated menu items will be stored
        /// </summary>
        [SerializeField]
        private GameObject menu;
        
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

        /// <summary>
        /// Time required for the whole slide animation (in seconds)
        /// </summary>
        [SerializeField]
        private float slideAnimationDuration = 1.0f;
        
        /// <summary>
        /// Panel hiding the inspector
        /// </summary>
        [SerializeField]
        private GameObject hidePanel;
        
        /// <summary>
        /// Panel showing the inspector
        /// </summary>
        [SerializeField]
        private GameObject showPanel;
        
        /// <summary>
        /// Height occluder scrollbar for limiting the objects visibility
        /// </summary>
        [SerializeField]
        private HeightOccluderScrollbar heightOccluderScrollbar;
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
        /// Height occluder scrollbar for limiting the objects visibility
        /// </summary>
        public HeightOccluderScrollbar HeightOccluderScroll => heightOccluderScrollbar;

        /// <summary>
        /// Current progress of showing the inspector, 1.0f hidden, 0.0f shown
        /// </summary>
        private float slideProgress;

        /// <summary>
        /// Target progress of showing the inspector, 1.0f will be hidden, 0.0f will be shown
        /// </summary>
        private float slideTarget;

        /// <summary>
        /// Current slide animation
        /// </summary>
        private IEnumerator slideAnimation;

        /// <summary>
        /// Event invoked when the inspector changes active menu item
        /// </summary>
        public event Action<InspectorMenuItem> MenuItemChanged;

        /// <summary>
        /// Initialization method
        /// </summary>
        public void Initialize()
        {
            HeightOccluderScroll.Initialize();
            for (var i = 0; i < menuItemsPrefabs.Count; i++)
            {
                var menuItem = Instantiate(menuItemsPrefabs[i], menu.transform);
                menuItems.Add(menuItem);
                menuItem.Initialize(this);
                if (i == 0) menuItem.ShowPanel();
                else menuItem.HidePanel();
            }

            activeMenuItem = menuItems.Count > 0 ? menuItems[0] : null;
            MenuItemChanged?.Invoke(activeMenuItem);
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            for (var i = 0; i < menuItems.Count; i++)
                menuItems[i].Deinitialize();
            menuItems.Clear();
            heightOccluderScrollbar.Deinitialize();
        }

        /// <summary>
        /// Shows selected inspector content panel while hiding previously selected one
        /// </summary>
        /// <param name="menuItem">Menu item that was selected</param>
        public void MenuItemSelected(InspectorMenuItem menuItem)
        {
            if (!menuItems.Contains(menuItem))
            {
                Log.Warning("Cannot show inspector panel which is not in the inspector content hierarchy.");
                return;
            }

            // Check if selected menu is currently active
            if (activeMenuItem == menuItem)
                return;

            if (activeMenuItem != null)
                activeMenuItem.HidePanel();

            menuItem.ShowPanel();
            activeMenuItem = menuItem;
            MenuItemChanged?.Invoke(activeMenuItem);
        }

        /// <summary>
        /// Toggles the inspector visibility, shows if hidden and hides if shown
        /// </summary>
        public void ToggleVisibility()
        {
            var requestHide = Mathf.Approximately(slideTarget, 0.0f);
            slideTarget = requestHide ? 1.0f : 0.0f;
            hidePanel.SetActive(!requestHide);
            showPanel.SetActive(requestHide);
            if (slideAnimation == null)
            {
                slideAnimation = SlideAnimation();
                StartCoroutine(slideAnimation);
            }
        }

        /// <summary>
        /// Animation for showing or hiding the inspector
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        /// <exception cref="ArgumentException">Inspector slide animation requires RectTransform component.</exception>
        private IEnumerator SlideAnimation()
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform==null)
                throw new ArgumentException("Inspector slide animation requires RectTransform component.");
            var wait = new WaitForEndOfFrame();
            var position = rectTransform.anchoredPosition;
            var size = rectTransform.sizeDelta;
            while (!Mathf.Approximately(slideProgress, slideTarget))
            {
                var direction = slideTarget > slideProgress ? 1.0f : -1.0f;
                slideProgress += direction * Time.unscaledDeltaTime / slideAnimationDuration;
                slideProgress = Mathf.Clamp(slideProgress, 0.0f, 1.0f);
                position.x = size.x * slideProgress;
                rectTransform.anchoredPosition = position;
                yield return wait;
            }

            slideProgress = slideTarget;
            position.x = size.x * slideProgress;
            rectTransform.anchoredPosition = position;

            slideAnimation = null;
        }
    }
}