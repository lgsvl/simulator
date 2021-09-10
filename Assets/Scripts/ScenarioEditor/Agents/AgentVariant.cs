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
    /// Data describing a single agent variant of the scenario agent type
    /// </summary>
    public class AgentVariant : SourceVariant
    {
        /// <summary>
        /// The source of the scenario agent type, this variant is a part of this source
        /// </summary>
        protected ScenarioAgentSource source;

        /// <summary>
        /// Name of this agent variant
        /// </summary>
        protected string name;
        
        /// <summary>
        /// Description of this agent variant
        /// </summary>
        protected string description;

        /// <summary>
        /// Prefab used to visualize this agent variant
        /// </summary>
        protected GameObject prefab;

        /// <inheritdoc/>
        public override string Name => name;
        
        /// <inheritdoc/>
        public override string Description => description;

        /// <inheritdoc/>
        public override GameObject Prefab => prefab;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The source of the scenario agent type, this variant is a part of this source</param>
        /// <param name="name">Name of this agent variant</param>
        /// <param name="prefab">Prefab used to visualize this agent variant</param>
        /// <param name="description">Description with agent variant details</param>
        public AgentVariant(ScenarioAgentSource source, string name, GameObject prefab, string description)
        {
            this.source = source;
            this.name = name;
            this.prefab = prefab;
            this.description = description;
            IsPrepared = prefab != null;
        }

        /// <inheritdoc/>
        public override Task Prepare(IProgress<SourceVariant> progress = null)
        {
            progress?.Report(this);
            return Task.CompletedTask;
        }
    }
}