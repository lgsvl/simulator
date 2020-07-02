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
    /// Interface that handles rotating element  invoked by <see cref="InputManager"/>
    /// </summary>
    public interface IRotateHandler
    {
        /// <summary>
        /// Method called when the rotating was started by <see cref="InputManager"/>
        /// </summary>
        /// <param name="viewportPosition">Viewport position for rotation event</param>
        void RotationStarted(Vector2 viewportPosition);

        /// <summary>
        /// Method called when the rotation viewport position has changed
        /// </summary>
        /// <param name="viewportPosition">Viewport position for rotation event</param>
        void RotationChanged(Vector2 viewportPosition);

        /// <summary>
        /// Method called when the rotation was finished by <see cref="InputManager"/>
        /// </summary>
        /// <param name="viewportPosition">Viewport position for rotation event</param>
        void RotationFinished(Vector2 viewportPosition);

        /// <summary>
        /// Method called when the rotation was canceled by <see cref="InputManager"/>
        /// </summary>
        /// <param name="viewportPosition">Viewport position for rotation event</param>
        void RotationCancelled(Vector2 viewportPosition);
    }
}