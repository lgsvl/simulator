/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapEdit
{
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
        /// Currently edited element reference
        /// </summary>
        ScenarioElement CurrentElement { get; set; }

        /// <summary>
        /// Checks if this map edit panel can edit selected scenario element
        /// </summary>
        bool CanEditElement(ScenarioElement element);

        /// <summary>
        /// Method that starts editing current element with this feature
        /// </summary>
        void Edit();
    }
}