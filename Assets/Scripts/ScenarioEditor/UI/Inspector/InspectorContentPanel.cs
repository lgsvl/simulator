/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Inspector
{
    using UnityEngine;

    /// <summary>
    /// Interface that has to be implemented in order to consider panel as a inspector content panel
    /// </summary>
    public abstract class InspectorContentPanel : MonoBehaviour
    {
        /// <summary>
        /// Menu item title that will be displayed in the inspector bar
        /// </summary>
        public string MenuItemTitle { get; }

        /// <summary>
        /// Initializes content panel without showing it
        /// </summary>
        public abstract void Initialize();
        
        /// <summary>
        /// Deinitializes content panel
        /// </summary>
        public abstract void Deinitialize();

        /// <summary>
        /// Method that shows inspector content panel when it is requested
        /// </summary>
        public abstract void Show();

        /// <summary>
        /// Method that hides inspector content panel when it is requested
        /// </summary>
        public abstract void Hide();
    }
}