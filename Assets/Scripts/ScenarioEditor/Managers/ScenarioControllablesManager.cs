/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Controllable;
    using Controllables;
    using Data;
    using Input;
    using Map;
    using SimpleJSON;
    using Simulator.Utilities;
    using UnityEngine;

    /// <summary>
    /// Manager for caching and handling all the scenario controllables
    /// </summary>
    public class ScenarioControllablesManager : MonoBehaviour, IScenarioEditorExtension, ISerializedExtension
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Source of the controllable elements
        /// </summary>
        [SerializeField]
        private ScenarioControllableSource source;
#pragma warning restore 0649

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Source of the controllable elements
        /// </summary>
        public ScenarioControllableSource Source { get; private set; }

        /// <summary>
        /// All instantiated scenario controllables
        /// </summary>
        public List<ScenarioControllable> Controllables { get; } = new List<ScenarioControllable>();

        /// <summary>
        /// Event invoked when a new controllable is registered
        /// </summary>
        public event Action<ScenarioControllable> ControllableRegistered;

        /// <summary>
        /// Event invoked when controllable is unregistered
        /// </summary>
        public event Action<ScenarioControllable> ControllableUnregistered;

        /// <summary>
        /// <see cref="IControllable"/> that is the target of copied policy
        /// </summary>
        private IControllable copiedPolicyTarget;

        /// <summary>
        /// Copied controllable policy
        /// </summary>
        private List<ControlAction> copiedPolicy;

        /// <summary>
        /// Initialization method
        /// </summary>
        public async Task Initialize()
        {
            if (IsInitialized)
                return;
            var loadingProcess = ScenarioManager.Instance.loadingPanel.AddProgress();
            loadingProcess.Update("Initializing controllables.");
            await ScenarioManager.Instance.WaitForExtension<InputManager>();
            await ScenarioManager.Instance.WaitForExtension<ScenarioMapManager>();
            Source = Instantiate(source, transform);
            var sourceProgress = new Progress<float>(f =>
                loadingProcess.Update($"Loading controllables {f:P}."));
            await Source.Initialize(sourceProgress);
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            OnMapChanged(mapManager.CurrentMapMetaData);
            mapManager.MapChanged += OnMapChanged;
            ScenarioManager.Instance.ScenarioReset += OnScenarioReset;
            IsInitialized = true;
            loadingProcess.NotifyCompletion();
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            OnScenarioReset();
            Source.Deinitialize();
            Destroy(Source);
            Source = null;
            Controllables.Clear();
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            mapManager.MapChanged -= OnMapChanged;
            ScenarioManager.Instance.ScenarioReset -= OnScenarioReset;
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
        }

        /// <summary>
        /// Method called when new map is loaded
        /// </summary>
        /// <param name="mapData">Loaded map data</param>
        private void OnMapChanged(ScenarioMapManager.MapMetaData mapData)
        {
            for (var i = Controllables.Count - 1; i >= 0; i--)
            {
                var controllable = Controllables[i];
                controllable.RemoveFromMap();
                if (!controllable.IsEditableOnMap)
                    controllable.Dispose();
            }

            LoadMapControllables();
        }

        /// <summary>
        /// Loads controllables objects from the map
        /// </summary>
        private void LoadMapControllables()
        {
            var mapSignals = FindObjectsOfType<MapSignal>();
            if (mapSignals.Length <= 0) return;
            foreach (var mapSignal in mapSignals)
            {
                var mapSignalVariant = new ControllableVariant();
                mapSignal.SetSignalMeshData();
                mapSignalVariant.Setup(nameof(MapSignal), mapSignal);
                var scenarioControllable = mapSignal.CurrentSignalLight.gameObject.AddComponent<ScenarioControllable>();
                scenarioControllable.Uid = mapSignal.UID;
                var policy = new List<ControlAction>(mapSignal.DefaultControlPolicy);
                scenarioControllable.Setup(source, mapSignalVariant, policy);
                var boxCollider = scenarioControllable.gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                var meshRenderers = scenarioControllable.GetComponentsInChildren<MeshRenderer>();
                //Set a default collider size if no mesh renderers were found
                if (meshRenderers.Length <= 0)
                {
                    boxCollider.center = Vector3.zero;
                    boxCollider.size = Vector3.one;
                    continue;
                }

                var bounds = meshRenderers[0].bounds;
                for (var i = 1; i < meshRenderers.Length; i++)
                    bounds.Encapsulate(meshRenderers[i].bounds);
                boxCollider.center = bounds.center - scenarioControllable.transform.position;
                boxCollider.size = bounds.extents * 2;
            }
        }

        /// <summary>
        /// Method invoked when current scenario is being reset
        /// </summary>
        private void OnScenarioReset()
        {
            for (var i = Controllables.Count - 1; i >= 0; i--)
            {
                var controllable = Controllables[i];
                if (!controllable.IsEditableOnMap)
                {
                    controllable.Policy =
                        new List<ControlAction>(controllable.Variant.controllable.DefaultControlPolicy);
                    continue;
                }

                controllable.RemoveFromMap();
                controllable.Dispose();
            }
        }

        /// <summary>
        /// Registers the controllable in the manager
        /// </summary>
        /// <param name="controllable">Controllable to register</param>
        public void RegisterControllable(ScenarioControllable controllable)
        {
            Controllables.Add(controllable);
            ControllableRegistered?.Invoke(controllable);
        }

        /// <summary>
        /// Unregisters the controllable in the manager
        /// </summary>
        /// <param name="controllable">Controllable to register</param>
        public void UnregisterControllable(ScenarioControllable controllable)
        {
            Controllables.Remove(controllable);
            ControllableUnregistered?.Invoke(controllable);
        }

        /// <summary>
        /// Searches for a registered controllable that contains given uid
        /// </summary>
        /// <param name="uid">Requested controllable uid</param>
        /// <returns>Controllable with given uid, null if it was not found</returns>
        public ScenarioControllable FindControllable(string uid)
        {
            foreach (var controllable in Controllables)
            {
                if (controllable.Uid == uid)
                    return controllable;
            }

            return null;
        }

        /// <summary>
        /// Copies policy value and required target
        /// </summary>
        /// <param name="target">Target controllable required for copied policy</param>
        /// <param name="policy">Copied policy value</param>
        public void CopyPolicy(IControllable target, List<ControlAction> policy)
        {
            copiedPolicy = policy;
            copiedPolicyTarget = target;
        }

        /// <summary>
        /// Gets the copied policy, returns false if target controllable is different than copied one
        /// </summary>
        /// <param name="target">Target controllable required for copied policy</param>
        /// <param name="policy">Copied policy value, empty if target is different than copied one</param>
        /// <returns>True if target is the same as copied one, false otherwise</returns>
        public bool GetCopiedPolicy(IControllable target, out List<ControlAction> policy)
        {
            if (target.GetType() == copiedPolicyTarget.GetType())
            {
                policy = copiedPolicy;
                return true;
            }

            policy = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Serialize(JSONNode data)
        {
            //Add controllables
            var controllablesNode = data.GetValueOrDefault("controllables", new JSONArray());
            if (!data.HasKey("controllables"))
                data.Add("controllables", controllablesNode);
            foreach (var controllable in Controllables)
                AddControllableNode(controllablesNode, controllable);
            return true;
        }

        /// <summary>
        /// Adds an controllable node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="controllable">Scenario controllable to serialize</param>
        private static void AddControllableNode(JSONNode data, ScenarioControllable controllable)
        {
            var controllableNode = new JSONObject();
            data.Add(controllableNode);
            controllableNode.Add("uid", new JSONString(controllable.Uid));
            controllableNode.Add("policy", Utility.SerializeControlPolicy(controllable.Policy));
            controllableNode.Add("spawned", controllable.IsEditableOnMap);
            if (!controllable.IsEditableOnMap) return;

            controllableNode.Add("name", new JSONString(controllable.Variant.Name));
            var transform = new JSONObject();
            controllableNode.Add("transform", transform);
            var position = new JSONObject().WriteVector3(controllable.TransformToMove.position);
            transform.Add("position", position);
            var rotation = new JSONObject().WriteVector3(controllable.TransformToRotate.rotation.eulerAngles);
            transform.Add("rotation", rotation);
        }

        /// <inheritdoc/>
        public Task<bool> Deserialize(JSONNode data)
        {
            var controllablesNode = data["controllables"] as JSONArray;
            if (controllablesNode == null)
                return Task.FromResult(false);
            foreach (var controllableNode in controllablesNode.Children)
            {
                var controllablesManager = ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>();
                IControllable iControllable;
                ScenarioControllable scenarioControllable;

                var uid = controllableNode["uid"];
                bool spawned;
                if (controllableNode.HasKey("spawned"))
                    spawned = controllableNode["spawned"].AsBool;
                else
                    spawned = controllablesManager.FindControllable(uid) == null;
                //Check if this controllable is already on the map, if yes just apply the policy
                if (!spawned)
                {
                    scenarioControllable = controllablesManager.FindControllable(uid);
                    if (scenarioControllable == null)
                    {
                        ScenarioManager.Instance.logPanel.EnqueueWarning(
                            $"Could not load controllable with uid: {uid}.");
                        continue;
                    }

                    iControllable = scenarioControllable.Variant.controllable;
                    scenarioControllable.Policy = iControllable.ParseControlPolicy(controllableNode["policy"], out _);
                    continue;
                }

                var controllableName = controllableNode["name"];
                var variant = controllablesManager.Source.Variants.Find(v => v.Name == controllableName);
                if (variant == null)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Error while deserializing Scenario. Controllable variant '{controllableName}' could not be found in Simulator.");
                    continue;
                }

                if (!(variant is ControllableVariant controllableVariant))
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Could not properly deserialize variant '{controllableName}' as {nameof(ControllableVariant)} class.");
                    continue;
                }

                var policy = Utility.ParseControlPolicy(null, controllableNode["policy"], out _);
                scenarioControllable = controllablesManager.Source.GetControllableInstance(controllableVariant, policy);
                scenarioControllable.Uid = uid;
                if (scenarioControllable.IsEditableOnMap)
                {
                    var transformNode = controllableNode["transform"];
                    scenarioControllable.transform.position = transformNode["position"].ReadVector3();
                    scenarioControllable.TransformToRotate.rotation =
                        Quaternion.Euler(transformNode["rotation"].ReadVector3());
                }
            }

            return Task.FromResult(true);
        }
    }
}