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
    using System.Threading.Tasks;
    using Elements;
    using Elements.Agents;
    using Elements.Waypoints;
    using Managers;
    using UnityEngine;
    using Web;

    /// <inheritdoc/>
    /// <remarks>
    /// This scenario agent source handles Pedestrian agents
    /// </remarks>
    public class ScenarioPedestrianAgentSource : ScenarioAgentSource
    {
        /// <inheritdoc/>
        public override string ElementTypeName => "PedestrianAgent";

        /// <inheritdoc/>
        public override string ParameterType => "";

        /// <inheritdoc/>
        public override int AgentTypeId => 3;

        /// <inheritdoc/>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();

        /// <inheritdoc/>
        public override Task Initialize(IProgress<float> progress)
        {
            var pedestriansInSimulation = Config.Pedestrians;
            var i = 0;
            foreach (var pedestrian in pedestriansInSimulation)
            {
                Debug.Log($"Loading pedestrian {pedestrian.Value.Name} from the pedestrian manager.");
                var variant = new AgentVariant(this, pedestrian.Value.Name, pedestrian.Value.Prefab, string.Empty);
                Variants.Add(variant);
                progress.Report((float) (++i) / pedestriansInSimulation.Count);
            }

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
                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Could not instantiate a prefab for the {variant.Name} pedestrian variant.");
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
                //Limit collider size
                b.size = new Vector3(Mathf.Clamp(b.size.x, 0.1f, 0.5f), b.size.y, Mathf.Clamp(b.size.z, 0.1f, 0.5f));
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
            scenarioAgent.GetOrAddExtension<AgentWaypointsPath>();
            return scenarioAgent;
        }

        /// <inheritdoc/>
        public override bool AgentSupportWaypoints(ScenarioAgent agent)
        {
            return true;
        }

        protected override void OnDraggedInstanceMove()
        {
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                draggedInstance.TransformToMove,
                draggedInstance.TransformToRotate);
        }
    }
}
