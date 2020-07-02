/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Managers;
    using UnityEngine;

    /// <inheritdoc/>
    /// <remarks>
    /// This scenario agent source handles Pedestrian agents
    /// </remarks>
    public class ScenarioPedestrianAgentSource : ScenarioAgentSource
    {
        /// <inheritdoc/>
        public override string AgentTypeName => "PedestrianAgent";

        /// <inheritdoc/>
        public override int AgentTypeId => 3;

        /// <inheritdoc/>
        public override List<AgentVariant> AgentVariants { get; } = new List<AgentVariant>();
        
        /// <inheritdoc/>
        public override AgentVariant DefaultVariant { get; set; }

        /// <summary>
        /// Currently dragged agent instance
        /// </summary>
        private GameObject draggedInstance;

        /// <inheritdoc/>
        #pragma warning disable 1998
        public async override Task Initialize()
        {
            var pedestrianManager = Loader.Instance.SimulatorManagerPrefab.pedestrianManagerPrefab;
            var pedestriansInSimulation = pedestrianManager.pedModels;
            for (var i = 0; i < pedestriansInSimulation.Count; i++)
            {
                var pedestrian = pedestriansInSimulation[i];
                var egoAgent = new AgentVariant()
                {
                    source = this,
                    name = pedestrian.name,
                    prefab = pedestrian
                };
                AgentVariants.Add(egoAgent);
            }

            DefaultVariant = AgentVariants[0];
        }
        #pragma warning restore 1998

        /// <inheritdoc/>
        public override void Deinitialize()
        {
        }

        /// <inheritdoc/>
        public override GameObject GetModelInstance(AgentVariant variant)
        {
            var instance = ScenarioManager.Instance.prefabsPools.GetInstance(variant.prefab);
            if (instance.GetComponent<BoxCollider>() == null)
            {
                var collider = instance.AddComponent<BoxCollider>();
                var b = new Bounds(instance.transform.position, Vector3.zero);
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                    b.Encapsulate(r.bounds);
                collider.center = b.center - instance.transform.position;
                //Limit collider size
                b.size = new Vector3(Mathf.Clamp(b.size.x, 0.1f, 0.5f), b.size.y, Mathf.Clamp(b.size.z, 0.1f, 0.5f));
                collider.size = b.size;
            }
            
            if (instance.GetComponent<Rigidbody>() == null)
            {
                var rigidbody = instance.AddComponent<Rigidbody>();
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rigidbody.isKinematic = true;
            }

            return instance;
        }

        /// <inheritdoc/>
        public override ScenarioAgent GetAgentInstance(AgentVariant variant)
        {
            var newGameObject = new GameObject(AgentTypeName);
            newGameObject.transform.SetParent(ScenarioManager.Instance.transform);
            var scenarioAgent = newGameObject.AddComponent<ScenarioAgent>();
            scenarioAgent.Setup(this, variant);
            return scenarioAgent;
        }

        /// <inheritdoc/>
        public override void ReturnModelInstance(GameObject instance)
        {
            ScenarioManager.Instance.prefabsPools.ReturnInstance(instance);
        }

        /// <inheritdoc/>
        public override void DragNewAgent()
        {
            ScenarioManager.Instance.inputManager.StartDraggingElement(this);
        }

        /// <inheritdoc/>
        public override void DragStarted(Vector3 dragPosition)
        {
            draggedInstance = ScenarioManager.Instance.prefabsPools.GetInstance(AgentVariants[0].prefab);
            draggedInstance.transform.SetParent(ScenarioManager.Instance.transform);
            draggedInstance.transform.SetPositionAndRotation(dragPosition, Quaternion.Euler(0.0f, 0.0f, 0.0f));
        }

        /// <inheritdoc/>
        public override void DragMoved(Vector3 dragPosition)
        {
            draggedInstance.transform.position = dragPosition;
        }

        /// <inheritdoc/>
        public override void DragFinished(Vector3 dragPosition)
        {
            var agent = GetAgentInstance(AgentVariants[0]);
            agent.transform.SetPositionAndRotation(draggedInstance.transform.position,
                draggedInstance.transform.rotation);
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            draggedInstance = null;
        }

        /// <inheritdoc/>
        public override void DragCancelled(Vector3 dragPosition)
        {
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            draggedInstance = null;
        }
    }
}