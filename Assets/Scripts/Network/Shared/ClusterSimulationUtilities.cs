/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Shared
{
    using Core.Components;
    using UnityEngine;

    /// <summary>
    /// Set of utility methods for cluster simulation
    /// </summary>
    public static class ClusterSimulationUtilities
    {
        /// <summary>
        /// Adds required distributed components to the GameObject and it's children <br/>
        /// Currently adds:
        /// <br/><see cref="DistributedRigidbody"/> to every <see cref="Rigidbody"/> component
        /// <br/><see cref="DistributedTransform"/> to every <see cref="ArticulationBody"/> component
        /// </summary>
        /// <param name="target">GameObject where required distributed components will be added</param>
        /// <returns>True if at least one component was added, false otherwise</returns>
        public static bool AddDistributedComponents(GameObject target)
        {
            var anyComponentAdded = false;
            var rigidbodies = target.GetComponentsInChildren<Rigidbody>();
            var articulationBodies = target.GetComponentsInChildren<ArticulationBody>();
            if (rigidbodies.Length != 0 || articulationBodies.Length != 0)
            {
                if (target.GetComponent<DistributedObject>() == null)
                {
                    var distributedObject = target.AddComponent<DistributedObject>();
                    distributedObject.CallInitialize();
                }

                anyComponentAdded = true;
            }

            foreach (var rigidbody in rigidbodies)
            {
                if (rigidbody.GetComponent<DistributedRigidbody>() == null)
                    rigidbody.gameObject.AddComponent<DistributedRigidbody>();
            }

            foreach (var articulationBody in articulationBodies)
            {
                if (articulationBody.GetComponent<DistributedTransform>() == null &&
                    articulationBody.GetComponent<DistributedRigidbody>() == null)
                    articulationBody.gameObject.AddComponent<DistributedTransform>();
            }

            return anyComponentAdded;
        }
    }
}