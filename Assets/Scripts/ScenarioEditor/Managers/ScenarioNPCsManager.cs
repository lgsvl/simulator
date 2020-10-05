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
    using Simulator.Utilities;
    using UnityEngine;

    /// <summary>
    /// Manager that cache data for the NPC agents
    /// </summary>
    public class ScenarioNPCsManager : IScenarioEditorExtension
    {
        /// <summary>
        /// Types of all the NPC behaviours available in the Simulator
        /// </summary>
        public List<Type> AvailableBehaviourTypes { get; private set; }

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc/>
        public Task Initialize()
        {
            if (IsInitialized)
                return Task.CompletedTask;
            AvailableBehaviourTypes = ReflectionCache.FindTypes(type => type.IsSubclassOf(typeof(NPCBehaviourBase)) && !type.IsAbstract);
            IsInitialized = true;
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
        }
    }
}
