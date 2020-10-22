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
    using Elements.Agents;
    using Input;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using Utilities;

    /// <inheritdoc/>
    /// <remarks>
    /// This scenario agent source handles Pedestrian agents
    /// </remarks>
    public class ScenarioPedestrianAgentSource : ScenarioAgentSource
    {
        /// <summary>
        /// Cached reference to the scenario editor input manager
        /// </summary>
        private InputManager inputManager;

        /// <summary>
        /// Currently dragged agent instance
        /// </summary>
        private GameObject draggedInstance;

        /// <inheritdoc/>
        public override string ElementTypeName => "PedestrianAgent";

        /// <inheritdoc/>
        public override string ParameterType => "";

        /// <inheritdoc/>
        public override int AgentTypeId => 3;

        /// <inheritdoc/>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();

        /// <inheritdoc/>
#pragma warning disable 1998
        public override async Task Initialize()
        {
            inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var pedestrianManager = Loader.Instance.SimulatorManagerPrefab.pedestrianManagerPrefab;
            var pedestriansInSimulation = pedestrianManager.pedModels;
            for (var i = 0; i < pedestriansInSimulation.Count; i++)
            {
                var pedestrian = pedestriansInSimulation[i];
                var egoAgent = new AgentVariant(this, pedestrian.name, pedestrian, string.Empty);
                Variants.Add(egoAgent);
            }
        }
#pragma warning restore 1998

        /// <inheritdoc/>
        public override void Deinitialize()
        {
        }

        /// <inheritdoc/>
        public override GameObject GetModelInstance(SourceVariant variant)
        {
            var instance = base.GetModelInstance(variant);
            if (instance.GetComponent<BoxCollider>() == null)
            {
                var collider = instance.AddComponent<BoxCollider>();
                collider.isTrigger = true;
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
            var newGameObject = new GameObject(ElementTypeName);
            newGameObject.transform.SetParent(transform);
            var scenarioAgent = newGameObject.AddComponent<ScenarioAgent>();
            scenarioAgent.Setup(this, variant);
            scenarioAgent.PathRenderer.material = waypointsMaterial;
            return scenarioAgent;
        }

        /// <inheritdoc/>
        public override bool AgentSupportWaypoints(ScenarioAgent agent)
        {
            return true;
        }

        /// <inheritdoc/>
        public override void DragStarted()
        {
            draggedInstance = GetModelInstance(selectedVariant);
            draggedInstance.transform.SetParent(ScenarioManager.Instance.transform);
            draggedInstance.transform.SetPositionAndRotation(inputManager.MouseRaycastPosition,
                Quaternion.Euler(0.0f, 0.0f, 0.0f));
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragMoved()
        {
            draggedInstance.transform.position = inputManager.MouseRaycastPosition;
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragFinished()
        {
            var agent = GetAgentInstance(selectedVariant);
            agent.TransformToRotate.rotation = draggedInstance.transform.rotation;
            agent.ForceMove(draggedInstance.transform.position);
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoAddElement(agent));
            draggedInstance = null;
        }

        /// <inheritdoc/>
        public override void DragCancelled()
        {
            draggedInstance = null;
        }
    }
}