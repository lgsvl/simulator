/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
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
    public class SourceVariant
    {
        /// <summary>
        /// Is this source variant prepared
        /// </summary>
        private bool isPrepared;

        /// <summary>
        /// Source variant name
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Description of this variant
        /// </summary>
        public virtual string Description { get; protected set; }

        /// <summary>
        /// Prefab used to visualize a scenario element variant
        /// </summary>
        public virtual GameObject Prefab { get; protected set; }

        /// <summary>
        /// Is this source variant currently being prepared
        /// </summary>
        public bool IsBusy { get; protected set; }

        /// <summary>
        /// Progress of the preparation in percents
        /// </summary>
        public float PreparationProgress { get; protected set; }

        /// <summary>
        /// Event invoked when this variant became prepared 
        /// </summary>
        public event Action Prepared;

        /// <summary>
        /// Is this source variant prepared
        /// </summary>
        public bool IsPrepared
        {
            get => isPrepared;
            protected set
            {
                if (isPrepared == value)
                    return;
                isPrepared = value;
                PreparationProgress = isPrepared ? 100.0f : 0.0f;
                if (isPrepared)
                    Prepared?.Invoke();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SourceVariant() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Source variant name</param>
        /// <param name="description">Description of this variant</param>
        /// <param name="prefab">Prefab used to visualize a scenario element variant</param>
        public SourceVariant(string name = null, string description = null, GameObject prefab = null)
        {
            Name = name;
            Description = description;
            Prefab = prefab;
            IsPrepared = prefab != null;
        }

        /// <summary>
        /// Prepares the variant with all the assets
        /// </summary>
        /// <returns>Task</returns>
        public virtual Task Prepare(IProgress<SourceVariant> progress = null)
        {
            return Task.CompletedTask;
        }
    }
}