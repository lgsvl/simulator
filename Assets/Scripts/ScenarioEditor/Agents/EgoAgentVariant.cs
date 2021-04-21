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
        /// Flag that indicates if the default are already cached
        /// </summary>
        private bool cachedDefaults;

        /// <summary>
        /// Should the default collider be added to the variant instances
        /// </summary>
        private bool requiresDefaultCollider;

        /// <summary>
        /// Should the default renderer be added to the variant instances
        /// </summary>
        private bool requiresDefaultRenderer;

        /// <summary>
        /// Bounds that are used to create a default collider or mesh renderer
        /// </summary>
        private Bounds defaultBounds;
        
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

        /// <summary>
        /// Calculates the default values and caches them
        /// </summary>
        /// <param name="defaultRendererPrefab">Renderer prefab that will be instantiated if instance has no mesh renderers</param>
        private void CacheDefaults(GameObject defaultRendererPrefab)
        {
            if (cachedDefaults)
                return;
            
            var colliders = Prefab != null ? Prefab.GetComponentsInChildren<Collider>() : null;
            var hasMeshRenderer = Prefab != null && Prefab.GetComponentInParent<MeshRenderer>() != null;
            defaultBounds = new Bounds();
            if (colliders != null && colliders.Length == 0)
            {
                //Add a default collider if there is no collider
                if (hasMeshRenderer)
                {
                    var renderers = Prefab.GetComponentsInParent<MeshRenderer>();
                    foreach (var renderer in renderers)
                        defaultBounds.Encapsulate(renderer.bounds);
                    requiresDefaultRenderer = false;
                    requiresDefaultCollider = true;
                }
                else
                {
                    var renderers = defaultRendererPrefab.GetComponentsInParent<MeshRenderer>();
                    foreach (var renderer in renderers)
                        defaultBounds.Encapsulate(renderer.bounds);
                    requiresDefaultRenderer = true;
                    requiresDefaultCollider = defaultRendererPrefab.GetComponentInChildren<Collider>() == null;
                }
            }
            else
            {
                if (colliders != null)
                {
                    foreach (var collider in colliders)
                    {
                        defaultBounds.Encapsulate(collider.bounds);
                    }
                }

                requiresDefaultRenderer = !hasMeshRenderer;
                requiresDefaultCollider = true;
            }

            cachedDefaults = true;
        }

        /// <summary>
        /// Adds the required components to the instance
        /// </summary>
        /// <param name="instance">Instance that will get required components</param>
        /// <param name="defaultRendererPrefab">Renderer prefab that will be instantiated if instance has no mesh renderers</param>
        public void AddRequiredComponents(GameObject instance, GameObject defaultRendererPrefab)
        {
            CacheDefaults(defaultRendererPrefab);

            if (requiresDefaultRenderer && instance.GetComponentInChildren<MeshRenderer>() == null)
            {
                Object.Instantiate(defaultRendererPrefab, instance.transform);
            }
            
            if (requiresDefaultCollider && instance.GetComponentInChildren<Collider>() == null)
            {
                var collider = instance.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.center = defaultBounds.center;
                collider.size = defaultBounds.size;
            }
        }
    }
}