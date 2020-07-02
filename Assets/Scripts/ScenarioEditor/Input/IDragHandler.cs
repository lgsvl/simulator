/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Input
{
    using UnityEngine;

    /// <summary>
    /// Interface that handles dragging element invoked by <see cref="InputManager"/>
    /// </summary>
    public interface IDragHandler
    {
        /// <summary>
        /// Method called when the dragging was started by <see cref="InputManager"/>
        /// </summary>
        /// <param name="dragPosition">World position for drag event</param>
        void DragStarted(Vector3 dragPosition);

        /// <summary>
        /// Method called when the dragging position has moved
        /// </summary>
        /// <param name="dragPosition">World position for drag event</param>
        void DragMoved(Vector3 dragPosition);

        /// <summary>
        /// Method called when the drag was finished by <see cref="InputManager"/>
        /// </summary>
        /// <param name="dragPosition">World position for drag event</param>
        void DragFinished(Vector3 dragPosition);

        /// <summary>
        /// Method called when the drag was canceled by <see cref="InputManager"/>
        /// </summary>
        /// <param name="dragPosition">World position for drag event</param>
        void DragCancelled(Vector3 dragPosition);
    }
}