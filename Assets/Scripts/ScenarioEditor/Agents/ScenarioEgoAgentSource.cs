/**
 * Copyright (c) 2020 LG Electronics, Inc.
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
    using Elements.Agents;
    using Input;
    using Managers;
    using Undo;
    using Undo.Records;
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
            inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var library = await ConnectionManager.API.GetLibrary<VehicleDetailData>();

            var assetService = new AssetService();
            var vehiclesInDatabase = assetService.List(BundleConfig.BundleTypes.Vehicle);
            var cachedVehicles = vehiclesInDatabase as AssetModel[] ?? vehiclesInDatabase.ToArray();

            for (var i = 0; i < library.Length; i++)
            {
                var vehicleDetailData = library[i];
                Debug.Log($"Loading ego vehicle {vehicleDetailData.Name} from the library.");
                var sb = new StringBuilder();
                sb.Append(vehicleDetailData.Description);
                var newVehicle = new CloudAgentVariant(this, vehicleDetailData.Name, null,
                    sb.ToString(), vehicleDetailData.Id, vehicleDetailData.AssetGuid)
                {
                    assetModel =
                        cachedVehicles.FirstOrDefault(model => model.AssetGuid == vehicleDetailData.AssetGuid)
                };
                if (newVehicle.assetModel != null)
                    newVehicle.AcquirePrefab();

                Variants.Add(newVehicle);
                progress.Report((float)(i+1)/library.Length);
            }
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
        }

        /// <inheritdoc/>
        public override GameObject GetModelInstance(SourceVariant variant)
        {
            var instance = base.GetModelInstance(variant);
            var colliders = instance.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders) collider.isTrigger = true;
            
            //Destroy all the custom components from the ego vehicle
            var allComponents = instance.GetComponents<MonoBehaviour>();
            for (var i = 0; i < allComponents.Length; i++)
            {
                var component = allComponents[i];
                DestroyImmediate(component);
            }

            var rigidbody = instance.GetComponent<Rigidbody>();
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidbody.isKinematic = true;
            return instance;
        }

        /// <inheritdoc/>
        public override ScenarioAgent GetAgentInstance(AgentVariant variant)
        {
            if (variant.Prefab == null)
            {
                Debug.LogError("Variant has to be prepared before getting it's instance.");
                return null;
            }

            var agentsManager = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>();
            var newGameObject = new GameObject(ElementTypeName);
            newGameObject.transform.SetParent(transform);
            var scenarioAgent = newGameObject.AddComponent<ScenarioAgent>();
            scenarioAgent.Setup(this, variant);
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

        /// <inheritdoc/>
        public override void DragStarted()
        {
            draggedInstance = GetModelInstance(selectedVariant);
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
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            draggedInstance = null;
        }
    }
}