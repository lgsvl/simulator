/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using Elements;
    using Input;

    /// <summary>
    /// Interface that handles continuous marking scenario elements invoked by <see cref="InputManager"/>
    /// </summary>
    public interface IMarkElementsHandler
    {
        /// <summary>
        /// Method called when the marking was started by <see cref="InputManager"/>
        /// </summary>
        void MarkingStarted();

        /// <summary>
        /// Method called when <see cref="InputManager"/> requests to mark given element
        /// </summary>
        /// <param name="element">Scenario element requested to mark</param>
        void MarkElement(ScenarioElement element);

        /// <summary>
        /// Method called when the marking was canceled by <see cref="InputManager"/>
        /// </summary>
        void MarkingCancelled();
    }
}