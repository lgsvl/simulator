/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Data describing a single agent variant of the ego agent type that is available from the cloud
    /// </summary>
    public class EgoAgentVariant : CloudAgentVariant
    {
        /// <summary>
        /// Meta-data for a configuration of the sensors in ego agent
        /// </summary>
        public class SensorsConfiguration
        {
            /// <summary>
            /// Id of this configuration
            /// </summary>
            public string Id { get; set; }
            
            /// <summary>
            /// Visible name of this configuration
            /// </summary>
            public string Name { get; set; }
        }

        /// <summary>
        /// All available sensors configurations for this ego agent variant
        /// </summary>
        public List<SensorsConfiguration> SensorsConfigurations { get; set; } = new List<SensorsConfiguration>();
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The source of the scenario agent type, this variant is a part of this source</param>
        /// <param name="name">Name of this agent variant</param>
        /// <param name="prefab">Prefab used to visualize this agent variant</param>
        /// <param name="description">Description with agent variant details</param>
        /// <param name="guid">Guid of the vehicle</param>
        /// <param name="assetGuid">Guid of the asset loaded within this vehicle</param>
        public EgoAgentVariant(ScenarioAgentSource source, string name, GameObject prefab, string description,
            string guid, string assetGuid) : base(source, name, prefab, description, guid, assetGuid)
        {
        }
    }
}