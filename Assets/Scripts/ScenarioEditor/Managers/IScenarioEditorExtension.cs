/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for all the scenario managers that extends the visual scenario editor
    /// </summary>
    public interface IScenarioEditorExtension
    {
        /// <summary>
        /// Is the scenario manager initialized
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Initialization method
        /// </summary>
        Task Initialize();

        /// <summary>
        /// Deinitialization method
        /// </summary>
        void Deinitialize();
    }
}
