/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// Scenario source variant for creating 
    /// </summary>
    public abstract class SourceVariant
    {
        /// <summary>
        /// Is this source variant prepared
        /// </summary>
        private bool isPrepared;
        
        /// <summary>
        /// Source variant name
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// Prefab used to visualize a scenario element variant
        /// </summary>
        public abstract GameObject Prefab { get;}
        
        /// <summary>
        /// Is this source variant prepared
        /// </summary>
        public bool IsPrepared {
            get => isPrepared;
            protected set
            {
                if (isPrepared == value)
                    return;
                isPrepared = value;
                if (isPrepared)
                    Prepared?.Invoke();
            }
        }
        
        /// <summary>
        /// Is this source variant currently being prepared
        /// </summary>
        public bool IsBusy { get; protected set; }

        /// <summary>
        /// Prepares the variant with all the assets
        /// </summary>
        /// <returns>Task</returns>
        public abstract Task Prepare();

        /// <summary>
        /// Event invoked when this variant became prepared 
        /// </summary>
        public event Action Prepared;
    }
}