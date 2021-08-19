/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Triggers
{
    using System.Collections.Generic;
    using System.Linq;
    using Managers;
    using ScenarioEditor.Agents.Triggers;
    using UnityEngine;

    /// <remarks>
    /// Scenario trigger data
    /// </remarks>
    public class ScenarioTrigger : ScenarioElement
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
            /// Checks if there is any object in this scenario effector
            /// </summary>
            public bool IsEmpty => !objects.Any();

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
                    ScenarioManager.Instance.prefabsPools
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
                    var effectorObject = GetEffectorObject(originObject.Key);
                    if (effectorObject == null)
                        effectorObject = AddEffectorObject(originObject.Key, originObject.Value.gameObject);
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
                var instance = ScenarioManager.Instance.prefabsPools.GetInstance(prefab);
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
                ScenarioManager.Instance.prefabsPools.ReturnInstance(effectorObject.gameObject);
                objects.Remove(objectName);
            }

            /// <summary>
            /// Method invoked before this effector object is serialized
            /// </summary>
            public void OnBeforeSerialize()
            {
                foreach (var effectorObject in objects)
                {
                    effectorObject.Value.OnBeforeSerialize();
                }
            }
        }

        /// <inheritdoc/>
        public override string ElementType => "ScenarioTrigger";

        /// <summary>
        /// Agent type that will be modified with this trigger
        /// </summary>
        public AgentType TargetAgentType { get; set; }

        /// <summary>
        /// Effectors that will be invoked on this trigger
        /// </summary>
        public WaypointTrigger Trigger { get; set; } = new WaypointTrigger();

        /// <summary>
        /// Effectors objects that are added as addons to this trigger
        /// </summary>
        public readonly Dictionary<string, ScenarioEffector> effectorsObjects =
            new Dictionary<string, ScenarioEffector>();

        /// <summary>
        /// Initializes created trigger and it's effectors
        /// </summary>
        public void Initialize()
        {
            Trigger.EffectorAdded += OnEffectorAdded;
            Trigger.EffectorRemoved += OnEffectorRemoved;
        }

        /// <summary>
        /// Deinitializes trigger clearing all the effectors
        /// </summary>
        public void Deinitalize()
        {
            Trigger.EffectorAdded -= OnEffectorAdded;
            Trigger.EffectorRemoved -= OnEffectorRemoved;
            Dispose();
        }
        
        /// <inheritdoc/>
        public override void Dispose()
        {
            ClearEffectors();
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        /// <summary>
        /// Method invoked when the effector is added to the trigger
        /// </summary>
        /// <param name="effector">Added effector</param>
        private void OnEffectorAdded(TriggerEffector effector)
        {
            if (effectorsObjects.ContainsKey(effector.TypeName))
                return;
            var scenarioEffector = new ScenarioEffector();
            scenarioEffector.Initialize(this, effector);
            effectorsObjects.Add(effector.TypeName, scenarioEffector);
        }

        /// <summary>
        /// Method invoked when the effector is removed to the trigger
        /// </summary>
        /// <param name="effector">Removed effector</param>
        private void OnEffectorRemoved(TriggerEffector effector)
        {
            effectorsObjects.TryGetValue(effector.TypeName, out var scenarioEffector);
            if (scenarioEffector == null)
                return;
            scenarioEffector.Deinitialize();
            effectorsObjects.Remove(effector.TypeName);
        }

        /// <summary>
        /// Clears all the attached effectors
        /// </summary>
        public void ClearEffectors()
        {
            for (var i = Trigger.Effectors.Count - 1; i >= 0; i--)
            {
                var effector = Trigger.Effectors[i];
                Trigger.RemoveEffector(effector);
            }

            foreach (var effector in effectorsObjects)
                effector.Value.Deinitialize();
            effectorsObjects.Clear();
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            var originTrigger = origin as ScenarioTrigger;
            if (originTrigger == null)
            {
                ScenarioManager.Instance.logPanel.EnqueueWarning($"Cannot copy properties from {origin.ElementType} to {ElementType}.");
                return;
            }

            TargetAgentType = originTrigger.TargetAgentType;
            ClearEffectors();
            var effectorsAdded = new List<TriggerEffector>();
            foreach (var effectorObject in originTrigger.effectorsObjects)
            {
                if (effectorObject.Value.IsEmpty)
                    continue;
                if (!effectorsObjects.TryGetValue(effectorObject.Key, out var scenarioEffector))
                {
                    var effectorOrigin =
                        originTrigger.Trigger.Effectors.Find(e => e.TypeName == effectorObject.Key);
                    var cloneEffector = effectorOrigin.Clone() as TriggerEffector;
                    scenarioEffector = new ScenarioEffector();
                    effectorsObjects.Add(effectorObject.Key, scenarioEffector);
                    scenarioEffector.Initialize(this, cloneEffector);
                    Trigger.AddEffector(cloneEffector);
                    effectorsAdded.Add(effectorOrigin);
                }

                scenarioEffector.CopyProperties(effectorObject.Value);
            }

            foreach (var effector in originTrigger.Trigger.Effectors)
            {
                if (effectorsAdded.Contains(effector))
                    continue;
                var cloneEffector = effector.Clone() as TriggerEffector;
                Trigger.AddEffector(cloneEffector);
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
            return effectorsObjects.TryGetValue(effector.TypeName, out var scenarioEffector)
                ? scenarioEffector.GetEffectorObject(objectName)
                : null;
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
            OnEffectorAdded(effector);
            return effectorsObjects[effector.TypeName].AddEffectorObject(objectName, prefab);
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
        /// Method invoked before this trigger is serialized
        /// </summary>
        public void OnBeforeSerialize()
        {
            foreach (var scenarioEffector in effectorsObjects)
            {
                scenarioEffector.Value.OnBeforeSerialize();
            }
        }
    }
}