/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using UnityEngine;

    /// <summary>
    /// Scenario source variant for creating 
    /// </summary>
    public abstract class SourceVariant
    {
        /// <summary>
        /// Source variant name
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// Prefab used to visualize a scenario element variant
        /// </summary>
        public abstract GameObject Prefab { get; }
        
        /// <summary>
        /// Texture used to visualize this scenario element variant in UI
        /// </summary>
        public abstract Texture2D IconTexture { get; }
    }
}