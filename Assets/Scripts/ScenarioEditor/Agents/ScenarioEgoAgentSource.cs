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
    using Elements.Agent;
    using Input;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using Utilities;
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
        public override string ElementTypeName => "EgoAgent";

        /// <inheritdoc/>
        public override int AgentTypeId => 1;

        /// <inheritdoc/>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();
        
        /// <inheritdoc/>
        public override async Task Initialize()
        {
            inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var library = await ConnectionManager.API.GetLibrary<VehicleDetailData>();

            var assetService = new AssetService();
            var vehiclesInDatabase = assetService.List(BundleConfig.BundleTypes.Environment);
            var cachedVehicles = vehiclesInDatabase as AssetModel[] ?? vehiclesInDatabase.ToArray();

            var downloadTasks = new List<Task>();
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
                    newVehicle.AcquirePrefab();
                else 
                    downloadTasks.Add(newVehicle.DownloadAsset());

                Variants.Add(newVehicle);
            }

            await Task.WhenAll(downloadTasks);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
        }

        /// <inheritdoc/>
        public override GameObject GetModelInstance(SourceVariant variant)
        {
            var instance = base.GetModelInstance(variant);
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

            var agentsManager = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>();
            var newGameObject = new GameObject(ElementTypeName);
            newGameObject.transform.SetParent(agentsManager.transform);
            var scenarioAgent = newGameObject.AddComponent<ScenarioAgent>();
            scenarioAgent.Setup(this, variant);
            //Add destination point
            var destinationPointObject = ScenarioManager.Instance.GetExtension<PrefabsPools>()
                .GetInstance(agentsManager.destinationPoint);
            var destinationPoint = destinationPointObject.GetComponent<ScenarioDestinationPoint>();
            destinationPoint.AttachToAgent(scenarioAgent);
            return scenarioAgent;
        }

        /// <inheritdoc/>
        public override bool AgentSupportWaypoints(ScenarioAgent agent)
        {
            return false;
        }

        /// <inheritdoc/>
        public override void DragStarted()
        {
            draggedInstance = GetModelInstance(selectedVariant);
            draggedInstance.transform.SetParent(ScenarioManager.Instance.transform);
            draggedInstance.transform.SetPositionAndRotation(inputManager.MouseRaycastPosition,
                Quaternion.Euler(0.0f, 0.0f, 0.0f));
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragMoved()
        {
            draggedInstance.transform.position = inputManager.MouseRaycastPosition;
            ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                draggedInstance.transform,
                draggedInstance.transform);
        }

        /// <inheritdoc/>
        public override void DragFinished()
        {
            var agent = GetAgentInstance(selectedVariant);
            agent.TransformToRotate.rotation = draggedInstance.transform.rotation;
            agent.ForceMove(draggedInstance.transform.position);
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