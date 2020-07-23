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
    using System.Threading.Tasks;
    using Database;
    using Database.Services;
    using Input;
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
        /// Cached reference to the scenario editor input manager
        /// </summary>
        private InputManager inputManager;

        /// <summary>
        /// Currently dragged agent instance
        /// </summary>
        private GameObject draggedInstance;

        /// <inheritdoc/>
        public override string AgentTypeName => "EgoAgent";

        /// <inheritdoc/>
        public override int AgentTypeId => 1;

        /// <inheritdoc/>
        public override List<AgentVariant> AgentVariants { get; } = new List<AgentVariant>();

        /// <inheritdoc/>
        public override AgentVariant DefaultVariant { get; set; }

        /// <inheritdoc/>
        public override async Task Initialize()
        {
            inputManager = ScenarioManager.Instance.inputManager;
            var library = await ConnectionManager.API.GetLibrary<VehicleDetailData>();

            var assetService = new AssetService();
            var vehiclesInDatabase = assetService.List(BundleConfig.BundleTypes.Environment);
            var cachedVehicles = vehiclesInDatabase as AssetModel[] ?? vehiclesInDatabase.ToArray();

            var isAnyPrefabAvailable = false;
            foreach (var vehicleDetailData in library)
            {
                var newVehicle = new CloudAgentVariant(vehicleDetailData.Id, vehicleDetailData.Name,
                    vehicleDetailData.AssetGuid)
                {
                    source = this,
                    assetModel =
                        cachedVehicles.FirstOrDefault(cachedMap => cachedMap.AssetGuid == vehicleDetailData.AssetGuid)
                };
                if (newVehicle.assetModel != null)
                {
                    newVehicle.AcquirePrefab();
                    if (DefaultVariant == null)
                        DefaultVariant = newVehicle;
                    isAnyPrefabAvailable = true;
                }

                AgentVariants.Add(newVehicle);
            }

            if (!isAnyPrefabAvailable)
            {
                await ((CloudAgentVariant) AgentVariants[0]).DownloadAsset();
                if (DefaultVariant == null)
                    DefaultVariant = AgentVariants[0];
            }
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
        }

        /// <inheritdoc/>
        public override GameObject GetModelInstance(AgentVariant variant)
        {
            if (variant.prefab == null)
            {
                Debug.LogError("Variant has to be prepared before getting it's model.");
                return null;
            }

            var instance = ScenarioManager.Instance.prefabsPools.GetInstance(variant.prefab);
            instance.GetComponent<VehicleController>().enabled = false;
            Object.DestroyImmediate(instance.GetComponent<VehicleActions>());
            return instance;
        }

        /// <inheritdoc/>
        public override ScenarioAgent GetAgentInstance(AgentVariant variant)
        {
            if (variant.prefab == null)
            {
                Debug.LogError("Variant has to be prepared before getting it's instance.");
                return null;
            }

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
        public override void DragStarted()
        {
            draggedInstance = GetModelInstance(DefaultVariant);
            draggedInstance.transform.SetParent(ScenarioManager.Instance.transform);
            draggedInstance.transform.SetPositionAndRotation(inputManager.MouseRaycastPosition,
                Quaternion.Euler(0.0f, 0.0f, 0.0f));
            ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragMoved()
        {
            draggedInstance.transform.position = inputManager.MouseRaycastPosition;
            ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragFinished()
        {
            var agent = GetAgentInstance(DefaultVariant);
            agent.TransformToRotate.rotation = draggedInstance.transform.rotation;
            agent.Reposition(draggedInstance.transform.position);
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            draggedInstance = null;
        }

        /// <inheritdoc/>
        public override void DragCancelled()
        {
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            draggedInstance = null;
        }
    }
}