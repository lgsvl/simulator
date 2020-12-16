/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using System;
    using Managers;

    /// <summary>
    /// Record that allows setting a generic undo
    /// </summary>
    public class GenericUndo<T> : UndoRecord
    {
        /// <summary>
        /// Value that was before the registered action, will be passed in the callbacks
        /// </summary>
        private readonly T previousValue;

        /// <summary>
        /// Message displayed in the log when undo is applied
        /// </summary>
        private readonly string undoMessage;
        
        /// <summary>
        /// Event invoked when the undo is applied
        /// </summary>
        private readonly Action<T> undoCallback;
        
        /// <summary>
        /// Event invoked to dispose this undo
        /// </summary>
        private readonly Action<T> disposeCallback;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="previousValue">Value that was before the registered action, will be passed in the callbacks</param>
        /// <param name="undoMessage">Message displayed in the log when undo is applied</param>
        /// <param name="undoCallback">Event invoked when the undo is applied</param>
        /// <param name="disposeCallback">Event invoked to dispose this undo</param>
        public GenericUndo(T previousValue, string undoMessage, Action<T> undoCallback, Action<T> disposeCallback = null)
        {
            this.previousValue = previousValue;
            this.undoMessage = undoMessage;
            this.undoCallback = undoCallback;
            this.disposeCallback = disposeCallback;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            undoCallback?.Invoke(previousValue);
            ScenarioManager.Instance.logPanel.EnqueueInfo($"Generic undo applied with the message: {undoMessage}.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            disposeCallback?.Invoke(previousValue);
        }
    }
}