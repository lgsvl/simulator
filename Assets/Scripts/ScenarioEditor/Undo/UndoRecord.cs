/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo
{
    /// <summary>
    /// Undo record that contains an revertable VSE action
    /// </summary>
    public abstract class UndoRecord
    {
        /// <summary>
        /// Undo the action bound to this record
        /// </summary>
        public abstract void Undo();

        /// <summary>
        /// Dispose all objects as this action can not be undone anymore
        /// </summary>
        public abstract void Dispose();
    }
}