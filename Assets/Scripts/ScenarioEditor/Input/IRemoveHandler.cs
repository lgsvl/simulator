/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Input
{
    /// <summary>
    /// Interface that handles removing an scenario element
    /// </summary>
    public interface IRemoveHandler
    {
        /// <summary>
        /// Method called to entirely remove element from the scenario
        /// </summary>
        void Remove();
    }
}