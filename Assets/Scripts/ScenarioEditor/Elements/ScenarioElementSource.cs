/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using System.Collections.Generic;
    using Managers;
    using ScenarioEditor.Agents;
    using UnityEngine;

    /// <summary>
    /// Abstract scenario element source 
    /// </summary>
    public abstract class ScenarioElementSource : MonoBehaviour
    {
        /// <summary>
        /// Name of the agent type this source handles
        /// </summary>
        public abstract string ElementTypeName { get; }
        
        /// <summary>
        /// List of available variants in this element sourc
        /// </summary>
        public abstract List<SourceVariant> Variants { get; }

        /// <summary>
        /// Method that instantiates and initializes a prefab of the selected variant
        /// </summary>
        /// <param name="variant">Scenario element variant which model should be instantiated</param>
        /// <returns>Scenario element variant model</returns>
        public virtual GameObject GetModelInstance(SourceVariant variant)
        {
            var instance = ScenarioManager.Instance.prefabsPools.GetInstance(variant.Prefab);
            return instance;
        }
        
        /// <summary>
        /// Returns the model instance to the models pool
        /// </summary>
        /// <param name="instance">Scenario element variant instance which should be returned to the pool</param>
        public void ReturnModelInstance(GameObject instance)
        {
            ScenarioManager.Instance.prefabsPools.ReturnInstance(instance);
        } 

        /// <summary>
        /// Method invokes when this source is selected in the UI
        /// </summary>
        /// <param name="variant">Scenario element variant that is selected</param>
        public abstract void OnVariantSelected(SourceVariant variant);
    }
}