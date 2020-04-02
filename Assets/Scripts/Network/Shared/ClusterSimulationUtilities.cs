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
        /// Currently adds: <br/><see cref="DistributedRigidbody"/> to every <see cref="Rigidbody"/> component
        /// </summary>
        /// <param name="target">GameObject where required distributed components will be added</param>
        /// <returns>True if at least one component was added, false otherwise</returns>
        public static bool AddDistributedComponents(GameObject target)
        {
            var distributedObjectAdded = false;
            var rigidbodies = target.GetComponentsInChildren<Rigidbody>();
            if (rigidbodies.Length != 0)
            {
                if (target.GetComponent<DistributedObject>() == null)
                {
                    var distributedObject = target.AddComponent<DistributedObject>();
                    distributedObject.CallInitialize();
                }

                distributedObjectAdded = true;
            }
            foreach (var rigidbody in rigidbodies)
            {
                if (target.GetComponent<DistributedRigidbody>() == null)
                    rigidbody.gameObject.AddComponent<DistributedRigidbody>();
            }
            return distributedObjectAdded;
        }
    }
}