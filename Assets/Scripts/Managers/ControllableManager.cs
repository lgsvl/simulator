/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Api;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Network.Core.Identification;
using Simulator.Network.Shared;

namespace Simulator.Controllable
{
    public class ControllableManager : MonoBehaviour
    {
        public IControllable SpawnControllable(GameObject prefab, string uid, Vector3 pos, Quaternion rot, Vector3 velocity, Vector3 angVelocity)
        {
            var api = ApiManager.Instance;
            var obj = Instantiate(prefab, pos, rot);
            obj.transform.SetParent(transform);
            obj.transform.position = pos;
            obj.transform.rotation = rot;

            var rb = obj.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = velocity;
                rb.angularVelocity = angVelocity;
            }

            IControllable controllable = obj.GetComponent<IControllable>();
            if (controllable == null)
            {
                Destroy(obj);
                Debug.LogError($"Prefab missing IControllable component, spawning {prefab.name} aborted");
                return null;
            }

            controllable.UID = uid;
            controllable.Spawned = true;

            api.Controllables.Add(uid, controllable);
            api.ControllablesUID.Add(controllable, uid);

            if (Loader.Instance.Network.IsClusterSimulation)
            {
                //Add required components for cluster simulation
                ClusterSimulationUtilities.AddDistributedComponents(obj);
            }

            return controllable;
        }

        public void RemoveControllable(string id, IControllable controllable)
        {
            var api = ApiManager.Instance;
            if (api.Controllables.ContainsKey(id))
                api.Controllables.Remove(id);
            if (api.ControllablesUID.ContainsKey(controllable))
                api.ControllablesUID.Remove(controllable);

            if (controllable.Spawned)
                Destroy(controllable.gameObject);
        }

        public void Reset()
        {
            var api = ApiManager.Instance;
            var allControllables = FindObjectsOfType<MonoBehaviour>().OfType<IControllable>();
            foreach (var controllable in allControllables)
            {
                if (controllable.Spawned)
                {
                    Destroy(controllable.gameObject);
                }
                else
                {
                    var uid = System.Guid.NewGuid().ToString();
                    api.Controllables.Add(uid, controllable);
                    api.ControllablesUID.Add(controllable, uid);
                }
            }
        }
    }

    public struct ControlAction
    {
        public string Action;
        public string Value;
    }

    public interface IControllable : IGloballyUniquelyIdentified
    {
        Transform transform { get; }
        GameObject gameObject { get; }

        bool Spawned { get; set; }
        string UID { get; set; }
        string ControlType { get; set; }  // Control type of a controllable object (i.e., signal)
        string CurrentState { get; set; }  // Current state of a controllable object (i.e., green)
        string[] ValidStates { get; }  // Valid states (i.e., green, yellow, red)
        string[] ValidActions { get; }  // Valid actions (i.e., trigger, wait)

        // Control policy defines rules for control actions
        string DefaultControlPolicy { get; set; }  // Default control policy
        string CurrentControlPolicy { get; set; }  // Control policy that's currently active

        /// <summary>Control a controllable object with a new control policy</summary>
        /// <param name="controlPolicy">A new control policy to control this object</param>
        /// <param name="errorMsg">Error message for invalid control policy</param>
        void Control(List<ControlAction> controlActions);
    }
}
