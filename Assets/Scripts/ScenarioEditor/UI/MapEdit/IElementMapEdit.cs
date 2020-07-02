/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapEdit
{
    using System;
    using Elements;

    /// <summary>
    /// Single feature that allows to edit map element
    /// </summary>
    public interface IElementMapEdit
    {
        /// <summary>
        /// Title of this feature
        /// </summary>
        string Title { get; }
        
        /// <summary>
        /// Target scenario elements that can be edited with this feature
        /// </summary>
        Type[] TargetTypes { get; }

        /// <summary>
        /// Currently edited element reference
        /// </summary>
        ScenarioElement CurrentElement { get; set; }

        /// <summary>
        /// Method that starts editing current element with this feature
        /// </summary>
        void Edit();
    }
}