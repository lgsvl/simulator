/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapEdit.Buttons
{
    using System;
    using Elements;

    /// <summary>
    /// Feature allowing to resize a map element
    /// </summary>
    public class ElementMapResize : ElementMapEdit
    {
        /// <inheritdoc/>
        public override bool CanEditElement(ScenarioElement element)
        {
            return element.CanBeResized;
        }
        
        /// <inheritdoc/>
        public override void Edit()
        {
            if (CurrentElement == null)
                throw new ArgumentException("Current agent has to be set by external script before editing.");
            CurrentElement.StartDragResize();
        }
    }
}