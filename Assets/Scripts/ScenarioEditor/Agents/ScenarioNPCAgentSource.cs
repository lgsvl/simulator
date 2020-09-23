/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Input;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using Utilities;

    /// <inheritdoc/>
    /// <remarks>
    /// This scenario agent source handles NPC agents
    /// </remarks>
    public class ScenarioNPCAgentSource : ScenarioAgentSource
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
        public override string AgentTypeName => "NPCAgent";

        /// <inheritdoc/>
        public override int AgentTypeId => 2;

        /// <inheritdoc/>
        public override List<AgentVariant> AgentVariants { get; } = new List<AgentVariant>();

        /// <inheritdoc/>
        public override AgentVariant DefaultVariant { get; set; }

        /// <inheritdoc/>
#pragma warning disable 1998
        public override async Task Initialize()
        {
            inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var npcVehiclesInSimulation = Web.Config.NPCVehicles;
            foreach (var npcAssetData in npcVehiclesInSimulation)
            {
                var npcVariant = new AgentVariant()
                {
                    source = this,
                    name = npcAssetData.Value.Name,
                    prefab = npcAssetData.Value.prefab
                };
                AgentVariants.Add(npcVariant);
            }

            Behaviours = new List<string>();
            var npcsManager = ScenarioManager.Instance.GetExtension<ScenarioNPCsManager>();
            Behaviours.AddRange(npcsManager.AvailableBehaviourTypes.Select(t => t.Name));

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
            var instance = ScenarioManager.Instance.GetExtension<PrefabsPools>().GetInstance(variant.prefab);
            if (instance.GetComponent<BoxCollider>() == null)
            {
                var collider = instance.AddComponent<BoxCollider>();
                var b = new Bounds(instance.transform.position, Vector3.zero);
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                    b.Encapsulate(r.bounds);
                collider.center = b.center - instance.transform.position;
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
            ScenarioManager.Instance.GetExtension<PrefabsPools>().ReturnInstance(instance);
        }

        /// <inheritdoc/>
        public override bool AgentSupportWaypoints(ScenarioAgent agent)
        {
            return agent.Behaviour == nameof(NPCWaypointBehaviour);
        }

        /// <inheritdoc/>
        public override void DragNewAgent()
        {
            ScenarioManager.Instance.GetExtension<InputManager>().StartDraggingElement(this);
        }

        /// <inheritdoc/>
        public override void DragStarted()
        {
            draggedInstance = ScenarioManager.Instance.GetExtension<PrefabsPools>()
                .GetInstance(AgentVariants[0].prefab);
            draggedInstance.transform.SetParent(ScenarioManager.Instance.transform);
            draggedInstance.transform.SetPositionAndRotation(inputManager.MouseRaycastPosition,
                Quaternion.Euler(0.0f, 0.0f, 0.0f));
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(
                LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragMoved()
        {
            draggedInstance.transform.position = inputManager.MouseRaycastPosition;
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(
                LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragFinished()
        {
            var agent = GetAgentInstance(AgentVariants[0]);
            agent.TransformToRotate.rotation = draggedInstance.transform.rotation;
            agent.ForceMove(draggedInstance.transform.position);
            agent.ChangeBehaviour(nameof(NPCWaypointBehaviour));
            ScenarioManager.Instance.GetExtension<PrefabsPools>().ReturnInstance(draggedInstance);
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoAddElement(agent));
            draggedInstance = null;
        }

        /// <inheritdoc/>
        public override void DragCancelled()
        {
            ScenarioManager.Instance.GetExtension<PrefabsPools>().ReturnInstance(draggedInstance);
            draggedInstance = null;
        }
    }
}