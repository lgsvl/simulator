/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using System.Collections.Generic;
    using Agents;
    using Managers;
    using UnityEngine;

    /// <remarks>
    /// Scenario trigger data
    /// </remarks>
    public class ScenarioTrigger : MonoBehaviour
    {
        /// <summary>
        /// Parent agent which includes this trigger
        /// </summary>
        public ScenarioAgent ParentAgent { get; set; }
        
        /// <summary>
        /// Waypoint that is linked to this trigger
        /// </summary>
        public ScenarioWaypoint LinkedWaypoint { get; set; }

        /// <summary>
        /// Effectors that will be invoked on this trigger
        /// </summary>
        public WaypointTrigger Trigger { get; set; } = new WaypointTrigger();
        
        /// <summary>
        /// Dictionary effector objects that helps with visualizing the effector settings
        /// </summary>
        private Dictionary<string, GameObject> effectorObjects = new Dictionary<string, GameObject>();

        /// <summary>
        /// Initializes created trigger and it's effectors
        /// </summary>
        public void Initialize()
        {
            //TODO add trigger effectors
        }

        /// <summary>
        /// Deinitializes trigger and 
        /// </summary>
        public void Deinitalize()
        {
            foreach (var effectorObject in effectorObjects)
                ScenarioManager.Instance.prefabsPools.ReturnInstance(effectorObject.Value);
            Trigger.Effectors.Clear();
            effectorObjects.Clear();
        }

        /// <summary>
        /// Gets an effector object from the trigger or creates new one
        /// </summary>
        /// <param name="objectName">The unique name for the object</param>
        /// <param name="prefab"></param>
        /// <returns>Effector object inside the trigger</returns>
        public GameObject GetOrAddEffectorObject(string objectName, GameObject prefab)
        {
            if (effectorObjects.TryGetValue(objectName, out var effectorObject))
                return effectorObject;
            effectorObject = ScenarioManager.Instance.prefabsPools.GetInstance(prefab);
            effectorObject.gameObject.name = objectName;
            effectorObject.transform.SetParent(transform);
            effectorObjects.Add(objectName, effectorObject);
            return effectorObject;
        }

        /// <summary>
        /// Removes the effector object from the trigger
        /// </summary>
        /// <param name="objectName">Effector object name</param>
        public void RemoveEffectorObject(string objectName)
        {
            if (!effectorObjects.TryGetValue(objectName, out var effectorObject)) return;
            ScenarioManager.Instance.prefabsPools.ReturnInstance(effectorObject);
            effectorObjects.Remove(objectName);
        }
    }
}