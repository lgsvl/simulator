/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Elements;
    using Elements.Agents;
    using Elements.Waypoints;
    using Managers;
    using UnityEngine;
    using Web;

    /// <inheritdoc/>
    /// <remarks>
    /// This scenario agent source handles NPC agents
    /// </remarks>
    public class ScenarioNPCAgentSource : ScenarioAgentSource
    {
        /// <inheritdoc/>
        public override string ElementTypeName => "NPCAgent";

        /// <inheritdoc/>
        public override string ParameterType => "";

        /// <inheritdoc/>
        public override int AgentTypeId => 2;

        /// <inheritdoc/>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();

        /// <inheritdoc/>
        public override Task Initialize(IProgress<float> progress)
        {
            var npcVehiclesInSimulation = Config.NPCVehicles;
            var npcsCount = npcVehiclesInSimulation.Count;
            var i = 0;
            foreach (var npcAssetData in npcVehiclesInSimulation)
            {
                var sb = new StringBuilder();
                Debug.Log($"Loading NPC {npcAssetData.Value.Name} from the config.");
                sb.Append("NPC type: ");
                sb.Append(npcAssetData.Value.NPCType);
                var npcVariant = new AgentVariant(this, npcAssetData.Value.Name, npcAssetData.Value.Prefab,
                    sb.ToString());
                Variants.Add(npcVariant);
                progress.Report((float)++i/npcsCount);
            }

            Behaviours = new List<string>();
            var npcsManager = ScenarioManager.Instance.GetExtension<ScenarioNPCsManager>();
            Behaviours.AddRange(npcsManager.AvailableBehaviourTypes.Select(t => t.Name));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
        }

        /// <inheritdoc/>
        public override GameObject GetModelInstance(SourceVariant variant)
        {
            var instance = base.GetModelInstance(variant);
            if (instance == null)
            {
                ScenarioManager.Instance.logPanel.EnqueueError($"Could not instantiate a prefab for the {variant.Name} NPC variant.");
                return null;
            }
            if (instance.GetComponent<BoxCollider>() == null)
            {
                var collider = instance.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                var b = new Bounds(instance.transform.position, Vector3.zero);
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                    b.Encapsulate(r.bounds);
                collider.center = b.center - instance.transform.position;
                collider.size = b.size;
            }

            // Set/Add limited rigidbody
            var rigidbody = instance.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = instance.AddComponent<Rigidbody>();
            }
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidbody.isKinematic = true;

            return instance;
        }
        
        /// <inheritdoc/>
        public override ScenarioElement GetElementInstance(SourceVariant variant)
        {
            var newGameObject = new GameObject(ElementTypeName);
            newGameObject.transform.SetParent(transform);
            var scenarioAgent = newGameObject.AddComponent<ScenarioAgent>();
            scenarioAgent.Setup(this, variant);
            scenarioAgent.GetOrAddExtension<AgentBehaviour>();
            scenarioAgent.GetOrAddExtension<AgentColorExtension>();
            scenarioAgent.GetOrAddExtension<AgentWaypointsPath>();
            return scenarioAgent;
        }

        /// <inheritdoc/>
        public override bool AgentSupportWaypoints(ScenarioAgent agent)
        {
            var behaviourExtension = agent.GetExtension<AgentBehaviour>();
            return behaviourExtension!=null && behaviourExtension.Behaviour == nameof(NPCWaypointBehaviour);
        }

        protected override void OnDraggedInstanceMove()
        {
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(
                LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.TransformToMove,
                draggedInstance.TransformToRotate);
        }
    }
}
