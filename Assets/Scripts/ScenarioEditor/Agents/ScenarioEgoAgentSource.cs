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
    using Database;
    using Database.Services;
    using Elements;
    using Elements.Agents;
    using Elements.Waypoints;
    using Managers;
    using UnityEngine;
    using Web;

    /// <inheritdoc/>
    /// <remarks>
    /// This scenario agent source handles Ego agents
    /// </remarks>
    public class ScenarioEgoAgentSource : ScenarioAgentSource
    {
        /// <summary>
        /// Renderer prefab that will be instantiated if instance has no mesh renderers
        /// </summary>
        public GameObject defaultRendererPrefab;
        
        /// <inheritdoc/>
        public override string ElementTypeName => "EgoAgent";

        /// <inheritdoc/>
        public override string ParameterType => "vehicle";

        /// <inheritdoc/>
        public override int AgentTypeId => 1;

        /// <inheritdoc/>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();

        /// <inheritdoc/>
        public override async Task Initialize(IProgress<float> progress)
        {
            var library = await ConnectionManager.API.GetLibrary<VehicleDetailData>();

            var assetService = new AssetService();
            var vehiclesInDatabase = assetService.List(BundleConfig.BundleTypes.Vehicle);
            var cachedVehicles = vehiclesInDatabase as AssetModel[] ?? vehiclesInDatabase.ToArray();

            for (var i = 0; i < library.Length; i++)
            {
                var vehicleDetailData = library[i];
                // Ignore vehicles with invalid data
                if (string.IsNullOrEmpty(vehicleDetailData.Name) || string.IsNullOrEmpty(vehicleDetailData.AssetGuid))
                    continue;
                Debug.Log($"Loading ego vehicle {vehicleDetailData.Name} from the library.");
                var sb = new StringBuilder();
                sb.Append(vehicleDetailData.Description);
                var newVehicle = new EgoAgentVariant(this, vehicleDetailData.Name, null,
                    sb.ToString(), vehicleDetailData.Id, vehicleDetailData.AssetGuid)
                {
                    assetModel =
                        cachedVehicles.FirstOrDefault(model => model.AssetGuid == vehicleDetailData.AssetGuid)
                };
                foreach (var sensorsConfiguration in vehicleDetailData.SensorsConfigurations)
                {
                    newVehicle.SensorsConfigurations.Add(new EgoAgentVariant.SensorsConfiguration
                    {
                        Id = sensorsConfiguration.Id,
                        Name = sensorsConfiguration.Name
                    });
                }

                if (newVehicle.assetModel != null)
                    newVehicle.AcquirePrefab();

                Variants.Add(newVehicle);
                progress.Report((float) (i + 1) / library.Length);
            }
        }

        /// <inheritdoc/>
        public override void Deinitialize() { }
        
        /// <inheritdoc/>
        public override GameObject GetModelInstance(SourceVariant variant)
        {
            var instance = variant.Prefab != null
                ? base.GetModelInstance(variant)
                : ScenarioManager.Instance.prefabsPools.GetInstance(defaultRendererPrefab);
            
            ((EgoAgentVariant) variant).AddRequiredComponents(instance, defaultRendererPrefab);
            var colliders = instance.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
                collider.isTrigger = true;

            var agent = instance.GetComponent<IAgentController>();
            agent?.DisableControl();

            // Destroy all the custom components from the ego vehicle
            var allComponents = instance.GetComponents<MonoBehaviour>();
            for (var i = 0; i < allComponents.Length; i++)
            {
                var component = allComponents[i];
                DestroyImmediate(component);
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
            if (variant.Prefab == null)
            {
                Debug.LogWarning("Variant has to be prepared before getting it's instance.");
                return null;
            }

            var agentsManager = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>();
            var newGameObject = new GameObject(ElementTypeName);
            newGameObject.transform.SetParent(transform);
            var scenarioAgent = newGameObject.AddComponent<ScenarioAgent>();
            scenarioAgent.Setup(this, variant);
            scenarioAgent.GetOrAddExtension<AgentSensorsConfiguration>();
            scenarioAgent.GetOrAddExtension<AgentDestinationPoint>();
            //Add destination point
            var destinationPointObject = ScenarioManager.Instance.prefabsPools
                .GetInstance(agentsManager.destinationPoint);
            var destinationPoint = destinationPointObject.GetComponent<ScenarioDestinationPoint>();
            destinationPoint.AttachToAgent(scenarioAgent, true);
            destinationPoint.SetActive(false);
            destinationPoint.SetVisibility(false);
            return scenarioAgent;
        }

        /// <inheritdoc/>
        public override bool AgentSupportWaypoints(ScenarioAgent agent)
        {
            return false;
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
