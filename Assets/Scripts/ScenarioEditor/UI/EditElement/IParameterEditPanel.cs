/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
    /// <summary>
    /// Edit panel for changing parameters of the edited scenario element
    /// </summary>
    public interface IParameterEditPanel
    {
        /// <summary>
        /// Initializes edit panel
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Deinitializes edit panel
        /// </summary>
        void Deinitialize();
    }
}