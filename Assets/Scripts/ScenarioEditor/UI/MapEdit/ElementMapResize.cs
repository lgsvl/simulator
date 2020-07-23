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
    /// Feature allowing to resize a map element
    /// </summary>
    public class ElementMapResize : IElementMapEdit
    {
        /// <inheritdoc/>
        public string Title { get; } = "Resize";
        
        /// <inheritdoc/>
        public ScenarioElement CurrentElement { get; set; }

        /// <inheritdoc/>
        public bool CanEditElement(ScenarioElement element)
        {
            return element.CanBeResized;
        }
        
        /// <inheritdoc/>
        public void Edit()
        {
            if (CurrentElement == null)
                throw new ArgumentException("Current agent has to be set by external script before editing.");
            CurrentElement.StartDragResize();
        }
    }
}