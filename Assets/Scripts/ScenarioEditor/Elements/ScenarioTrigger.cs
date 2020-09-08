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
        /// Effectors object that is added as addon to this trigger
        /// </summary>
        public class ScenarioEffector
        {
            /// <summary>
            /// Parent trigger that contains this effector
            /// </summary>
            private ScenarioTrigger parentTrigger;

            /// <summary>
            /// Trigger effector that is bound with the objects
            /// </summary>
            private TriggerEffector effector;

            /// <summary>
            /// Object that holds objects of this effector
            /// </summary>
            private GameObject gameObject;
            
            /// <summary>
            /// Dictionary effector objects that helps with visualizing the effector settings
            /// </summary>
            private readonly Dictionary<string, GameObject> objects = new Dictionary<string, GameObject>();

            /// <summary>
            /// Initialization of the effector
            /// </summary>
            /// <param name="parent">Parent trigger that contains this effector</param>
            public void Initialize(ScenarioTrigger parent, TriggerEffector effector)
            {
                parentTrigger = parent;
                this.effector = effector;
                gameObject = new GameObject(effector.TypeName);
                gameObject.transform.SetParent(parentTrigger.transform);
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
                gameObject.transform.localPosition = Vector3.zero;
            }

            /// <summary>
            /// Effector deinitialization
            /// </summary>
            public void Deinitialize()
            {
                foreach (var effectorObject in objects)
                    ScenarioManager.Instance.prefabsPools.ReturnInstance(effectorObject.Value);
                objects.Clear();
                Destroy(gameObject);
            }

            /// <summary>
            /// Shows all the objects in this effector
            /// </summary>
            public void Show()
            {
                gameObject.SetActive(true);
            }

            /// <summary>
            /// Hides all the objects in this effector
            /// </summary>
            public void Hide()
            {
                gameObject.SetActive(false);
            }
            
            /// <summary>
            /// Gets an effector object from the trigger or creates new one
            /// </summary>
            /// <param name="objectName">The unique name for the object</param>
            /// <param name="prefab"></param>
            /// <returns>Effector object inside the trigger</returns>
            public GameObject GetOrAddEffectorObject(string objectName, GameObject prefab)
            {
                if (objects.TryGetValue(objectName, out var effectorObject))
                    return effectorObject;
                effectorObject = ScenarioManager.Instance.prefabsPools.GetInstance(prefab);
                effectorObject.gameObject.name = objectName;
                effectorObject.transform.SetParent(gameObject.transform);
                objects.Add(objectName, effectorObject);
                return effectorObject;
            }
            
            /// <summary>
            /// Removes the effector object from the trigger
            /// </summary>
            /// <param name="objectName">Effector object name</param>
            public void RemoveEffectorObject(string objectName)
            {
                if (!objects.TryGetValue(objectName, out var effectorObject)) return;
                ScenarioManager.Instance.prefabsPools.ReturnInstance(effectorObject);
                objects.Remove(objectName);
            }
        }
        
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
        /// Effectors objects that are added as addons to this trigger
        /// </summary>
        public Dictionary<TriggerEffector, ScenarioEffector> effectors = new Dictionary<TriggerEffector, ScenarioEffector>();

        /// <summary>
        /// Initializes created trigger and it's effectors
        /// </summary>
        public void Initialize()
        {
            //TODO add trigger effectors
        }

        /// <summary>
        /// Deinitializes trigger clearing all the effectors
        /// </summary>
        public void Deinitalize()
        {
            foreach (var effector in effectors)
            {
                effector.Value.Deinitialize();
            }
            Trigger.Effectors.Clear();
        }

        /// <summary>
        /// Gets an effector object from the trigger or creates new one
        /// </summary>
        /// <param name="effector">Effector that uses this object</param>
        /// <param name="objectName">The unique name for the object</param>
        /// <param name="prefab"></param>
        /// <returns>Effector object inside the trigger</returns>
        public GameObject GetOrAddEffectorObject(TriggerEffector effector, string objectName, GameObject prefab)
        {
            if (effectors.TryGetValue(effector, out var scenarioEffector))
                return scenarioEffector.GetOrAddEffectorObject(objectName, prefab);
            scenarioEffector = new ScenarioEffector();
            scenarioEffector.Initialize(this, effector);
            effectors.Add(effector, scenarioEffector);
            return scenarioEffector.GetOrAddEffectorObject(objectName, prefab);
        }

        /// <summary>
        /// Removes the effector object from the trigger
        /// </summary>
        /// <param name="effector">Effector that uses this object</param>
        /// <param name="objectName">Effector object name</param>
        public void RemoveEffectorObject(TriggerEffector effector, string objectName)
        {
            if (!effectors.TryGetValue(effector, out var scenarioEffector)) return;
            scenarioEffector.RemoveEffectorObject(objectName);
        }

        /// <summary>
        /// Tries to get the scenario effector data
        /// </summary>
        /// <param name="effector">Effector for which data is requested</param>
        /// <returns>Scenario effector data, null if it is not available</returns>
        public ScenarioEffector TryGetEffector(TriggerEffector effector)
        {
            effectors.TryGetValue(effector, out var scenarioEffector);
            return scenarioEffector;
        }
    }
}