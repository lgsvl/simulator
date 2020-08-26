/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors
{
    using UnityEngine;

    /// <summary>
    /// Edit panel for changing parameters of the edited scenario element
    /// </summary>
    public abstract class ParameterEditPanel : MonoBehaviour
    {
        /// <summary>
        /// Initializes edit panel
        /// </summary>
        public abstract void Initialize();
        
        /// <summary>
        /// Deinitializes edit panel
        /// </summary>
        public abstract void Deinitialize();
    }
}