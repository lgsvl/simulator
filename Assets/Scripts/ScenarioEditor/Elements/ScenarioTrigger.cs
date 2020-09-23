/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using System;
    using System.Collections.Generic;
    using Agents;
    using Agents.Triggers;
    using Managers;
    using UnityEngine;
    using Utilities;

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
            private readonly Dictionary<string, ScenarioEffectorObject> objects =
                new Dictionary<string, ScenarioEffectorObject>();

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
                    ScenarioManager.Instance.GetExtension<PrefabsPools>()
                        .ReturnInstance(effectorObject.Value.gameObject);
                objects.Clear();
                Destroy(gameObject);
            }

            /// <summary>
            /// Method called after this element is created by copying
            /// </summary>
            /// <param name="origin">Origin element from which copy was created</param>
            public void CopyProperties(ScenarioEffector origin)
            {
                foreach (var originObject in origin.objects)
                {
                    var effectorObject = AddEffectorObject(originObject.Key, originObject.Value.gameObject);
                    effectorObject.CopyProperties(originObject.Value);
                }
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
            /// Gets an effector object from the trigger
            /// </summary>
            /// <param name="objectName">The unique name for the object</param>
            /// <returns>Effector object inside the trigger</returns>
            public ScenarioEffectorObject GetEffectorObject(string objectName)
            {
                return objects.TryGetValue(objectName, out var effectorObject) ? effectorObject : null;
            }

            /// <summary>
            /// Creates an effector object from the trigger
            /// </summary>
            /// <param name="objectName">The unique name for the object</param>
            /// <param name="prefab">Prefab that will be used to instantiate a new object</param>
            /// <returns>Effector object inside the trigger</returns>
            public ScenarioEffectorObject AddEffectorObject(string objectName, GameObject prefab)
            {
                if (objects.TryGetValue(objectName, out var effectorObject))
                {
                    Debug.LogWarning(
                        $"Trying to add duplicate effector object \"{objectName}\" for the \"{effector.TypeName} effector.");
                    return effectorObject;
                }

                //Check if there is unbound object
                Transform effectorTransform;
                var childWithName = gameObject.transform.Find(objectName);
                if (childWithName != null)
                {
                    effectorObject = childWithName.GetComponent<ScenarioEffectorObject>();
                    if (effectorObject != null)
                    {
                        //Reinitialize unbound object
                        effectorTransform = effectorObject.transform;
                        effectorTransform.localPosition = prefab.transform.localPosition;
                        effectorTransform.localRotation = prefab.transform.localRotation;
                        effectorObject.Setup(parentTrigger, effector);
                        objects.Add(objectName, effectorObject);
                        return effectorObject;
                    }

                    Destroy(effectorObject);
                }

                //Instantiate new object
                var instance = ScenarioManager.Instance.GetExtension<PrefabsPools>().GetInstance(prefab);
                effectorObject = instance.GetComponent<ScenarioEffectorObject>();
                if (effectorObject == null)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Cannot initialize effector object {prefab.name}, prefab requires {nameof(ScenarioEffectorObject)} component.");
                    return null;
                }

                effectorObject.gameObject.name = objectName;
                (effectorTransform = effectorObject.transform).SetParent(gameObject.transform);
                effectorTransform.localPosition = prefab.transform.localPosition;
                effectorTransform.localRotation = prefab.transform.localRotation;
                effectorObject.Setup(parentTrigger, effector);
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
                ScenarioManager.Instance.GetExtension<PrefabsPools>().ReturnInstance(effectorObject.gameObject);
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
        public Dictionary<string, ScenarioEffector> effectorsObjects =
            new Dictionary<string, ScenarioEffector>();

        /// <summary>
        /// Initializes created trigger and it's effectors
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// Deinitializes trigger clearing all the effectors
        /// </summary>
        public void Deinitalize()
        {
            ClearEffectors();

            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        /// <summary>
        /// Clears all the attached effectors
        /// </summary>
        public void ClearEffectors()
        {
            for (var i = Trigger.Effectors.Count - 1; i >= 0; i--)
            {
                var effector = Trigger.Effectors[i];
                Trigger.RemoveEffector(effector.TypeName);
            }

            foreach (var effector in effectorsObjects)
                effector.Value.Deinitialize();
            effectorsObjects.Clear();
        }

        /// <summary>
        /// Copies property values from the origin
        /// </summary>
        /// <param name="originTrigger">Origin trigger, properties will be copied from it to this trigger</param>
        public void CopyProperties(ScenarioTrigger originTrigger)
        {
            LinkedWaypoint = originTrigger.LinkedWaypoint;
            ClearEffectors();
            foreach (var effector in originTrigger.Trigger.Effectors)
            {
                var clone = effector.Clone() as TriggerEffector;
                Trigger.AddEffector(clone);
            }

            foreach (var effector in originTrigger.effectorsObjects)
            {
                if (!effectorsObjects.TryGetValue(effector.Key, out var scenarioEffector))
                {
                    var cloneEffector = Trigger.Effectors.Find((e) => e.TypeName == effector.Key);
                    if (cloneEffector == null)
                        continue;
                    scenarioEffector = new ScenarioEffector();
                    effectorsObjects.Add(effector.Key, scenarioEffector);
                    scenarioEffector.Initialize(this, cloneEffector);
                }

                scenarioEffector.CopyProperties(effector.Value);
            }
        }

        /// <summary>
        /// Gets an effector object from the trigger or creates new one
        /// </summary>
        /// <param name="effector">Effector that uses this object</param>
        /// <param name="objectName">The unique name for the object</param>
        /// <returns>Effector object inside the trigger</returns>
        public ScenarioEffectorObject GetEffectorObject(TriggerEffector effector, string objectName)
        {
            return effectorsObjects.TryGetValue(effector.TypeName, out var scenarioEffector) ? scenarioEffector.GetEffectorObject(objectName) : null;
        }

        /// <summary>
        /// Gets an effector object from the trigger or creates new one
        /// </summary>
        /// <param name="effector">Effector that uses this object</param>
        /// <param name="objectName">The unique name for the object</param>
        /// <param name="prefab">Prefab used to instantiate new effector object</param>
        /// <returns>Effector object inside the trigger</returns>
        public ScenarioEffectorObject AddEffectorObject(TriggerEffector effector, string objectName,
            GameObject prefab)
        {
            if (effectorsObjects.TryGetValue(effector.TypeName, out var scenarioEffector))
                return scenarioEffector.AddEffectorObject(objectName, prefab);
            scenarioEffector = new ScenarioEffector();
            scenarioEffector.Initialize(this, effector);
            effectorsObjects.Add(effector.TypeName, scenarioEffector);
            return scenarioEffector.AddEffectorObject(objectName, prefab);
        }

        /// <summary>
        /// Removes the effector object from the trigger
        /// </summary>
        /// <param name="effector">Effector that uses this object</param>
        /// <param name="objectName">Effector object name</param>
        public void RemoveEffectorObject(TriggerEffector effector, string objectName)
        {
            if (!effectorsObjects.TryGetValue(effector.TypeName, out var scenarioEffector)) return;
            scenarioEffector.RemoveEffectorObject(objectName);
        }

        /// <summary>
        /// Tries to get the scenario effector data
        /// </summary>
        /// <param name="effector">Effector for which data is requested</param>
        /// <returns>Scenario effector data, null if it is not available</returns>
        public ScenarioEffector TryGetEffector(TriggerEffector effector)
        {
            effectorsObjects.TryGetValue(effector.TypeName, out var scenarioEffector);
            return scenarioEffector;
        }

        /// <summary>
        /// Disposes all the effector objects
        /// </summary>
        /// <param name="effector">Effector which was removed and its objects will be disposed</param>
        public void DisposeEffector(TriggerEffector effector)
        {
            effectorsObjects.TryGetValue(effector.TypeName, out var scenarioEffector);
            if (scenarioEffector == null)
                return;
            scenarioEffector.Deinitialize();
            effectorsObjects.Remove(effector.TypeName);
        }
    }
}