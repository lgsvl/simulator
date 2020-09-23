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
    using Undo;
    using Undo.Records;

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
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoRemoveElement(CurrentElement));
            CurrentElement.RemoveFromMap();
        }
    }
}