/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Input
{
    /// <summary>
    /// Interface that handles dragging element invoked by <see cref="InputManager"/>
    /// </summary>
    public interface IDragHandler
    {
        /// <summary>
        /// Method called when the dragging was started by <see cref="InputManager"/>
        /// </summary>
        void DragStarted();

        /// <summary>
        /// Method called when the dragging position has moved
        /// </summary>
        void DragMoved();

        /// <summary>
        /// Method called when the drag was finished by <see cref="InputManager"/>
        /// </summary>
        void DragFinished();

        /// <summary>
        /// Method called when the drag was canceled by <see cref="InputManager"/>
        /// </summary>
        void DragCancelled();
    }
}