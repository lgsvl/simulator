/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapEdit.Buttons
{
    using Elements;
    using Managers;

    /// <summary>
    /// Feature allowing to remove a map element
    /// </summary>
    public class ElementMapRemove : ElementMapEdit
    {
        /// <inheritdoc/>
        public override bool CanEditElement(ScenarioElement element)
        {
            return element.CanBeRemoved;
        }

        /// <inheritdoc/>
        public override void Edit()
        {
            ScenarioManager.Instance.SelectedElement = null;
            ScenarioManager.Instance.IsScenarioDirty = true;
            CurrentElement.Remove();
        }
    }
}