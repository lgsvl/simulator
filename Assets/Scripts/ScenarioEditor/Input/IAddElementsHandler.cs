/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using Input;
    using UnityEngine;

    /// <summary>
    /// Interface that handles continuous element adding invoked by <see cref="InputManager"/>
    /// </summary>
    public interface IAddElementsHandler
    {
        /// <summary>
        /// Method called when the adding was started by <see cref="InputManager"/>
        /// </summary>
        /// <param name="addPosition">World position for add event</param>
        void AddingStarted(Vector3 addPosition);

        /// <summary>
        /// Method called when the position for adding was moved
        /// </summary>
        /// <param name="addPosition">World position for add event</param>
        void AddingMoved(Vector3 addPosition);

        /// <summary>
        /// Method called when <see cref="InputManager"/> requests element add
        /// </summary>
        /// <param name="addPosition">World position for add event</param>
        void AddElement(Vector3 addPosition);

        /// <summary>
        /// Method called when the adding was canceled by <see cref="InputManager"/>
        /// </summary>
        /// <param name="addPosition">World position for add event</param>
        void AddingCancelled(Vector3 addPosition);
    }
}