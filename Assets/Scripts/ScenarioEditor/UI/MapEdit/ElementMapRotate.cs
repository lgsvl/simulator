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
    using Input;
    using Managers;

    /// <summary>
    /// Feature allowing to rotate a map element
    /// </summary>
    public class ElementMapRotate : IElementMapEdit
    {
        /// <inheritdoc/>
        public string Title { get; } = "Rotate";
        
        /// <inheritdoc/>
        public Type[] TargetTypes { get; } = {typeof(IRotateHandler)};
        
        /// <inheritdoc/>
        public ScenarioElement CurrentElement { get; set; }

        /// <inheritdoc/>
        public void Edit()
        {
            if (CurrentElement == null)
                throw new ArgumentException("Current agent has to be set by external script before editing.");
            ScenarioManager.Instance.inputManager.StartRotatingElement(CurrentElement as IRotateHandler);
        }
    }
}