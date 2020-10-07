/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Agents;
    using Controllables;
    using Input;
    using Simulator.Utilities;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Manager for caching and handling all the scenario controllables
    /// </summary>
    public class ScenarioControllablesManager : IScenarioEditorExtension
    {
        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Source of the controllable elements
        /// </summary>
        public ScenarioControllablesSource source = new ScenarioControllablesSource();

        /// <summary>
        /// All instantiated scenario controllables
        /// </summary>
        public List<ScenarioControllable> Controllables { get; } = new List<ScenarioControllable>();

        /// <summary>
        /// Event invoked when a new controllable is registered
        /// </summary>
        public event Action<ScenarioControllable> ControllableRegistered;
        
        /// <summary>
        /// Event invoked when controllable is unregistered
        /// </summary>
        public event Action<ScenarioControllable> ControllableUnregistered;

        /// <summary>
        /// Initialization method
        /// </summary>
        public async Task Initialize()
        {
            if (IsInitialized)
                return;
            await ScenarioManager.Instance.WaitForExtension<InputManager>();
            await source.Initialize();
            ScenarioManager.Instance.ScenarioReset += InstanceOnScenarioReset;
            IsInitialized = true;
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            InstanceOnScenarioReset();
            source.Deinitialize();
            Controllables.Clear();
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
        }

        /// <summary>
        /// Method invoked when current scenario is being reset
        /// </summary>
        private void InstanceOnScenarioReset()
        {
            for (var i = Controllables.Count - 1; i >= 0; i--)
            {
                var agent = Controllables[i];
                agent.RemoveFromMap();
                agent.Dispose();
            }
        }

        /// <summary>
        /// Registers the controllable in the manager
        /// </summary>
        /// <param name="controllable">Controllable to register</param>
        public void RegisterControllable(ScenarioControllable controllable)
        {
            Controllables.Add(controllable);
            ControllableRegistered?.Invoke(controllable);
        }

        /// <summary>
        /// Unregisters the controllable in the manager
        /// </summary>
        /// <param name="controllable">Controllable to register</param>
        public void UnregisterControllable(ScenarioControllable controllable)
        {
            Controllables.Remove(controllable);
            ControllableUnregistered?.Invoke(controllable);
        }
    }
}