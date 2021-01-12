/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Agents;
    using Controllable;
    using Controllables;
    using Input;
    using Map;
    using Simulator.Utilities;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Manager for caching and handling all the scenario controllables
    /// </summary>
    public class ScenarioControllablesManager : MonoBehaviour, IScenarioEditorExtension
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
        private string copiedPolicy;

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
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            Source = Instantiate(source, transform);
            var sourceProgress = new Progress<float>(f =>
                loadingProcess.Update($"Loading controllables {f:P}."));
            await Source.Initialize(sourceProgress);
            OnMapChanged(mapManager.CurrentMapMetaData);
            mapManager.MapChanged += OnMapChanged;
            ScenarioManager.Instance.ScenarioReset += InstanceOnScenarioReset;
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
            InstanceOnScenarioReset();
            Source.Deinitialize();
            Destroy(Source);
            Source = null;
            Controllables.Clear();
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            mapManager.MapChanged -= OnMapChanged;
            ScenarioManager.Instance.ScenarioReset -= InstanceOnScenarioReset;
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
        }

        /// <summary>
        /// Method called when new map is loaded
        /// </summary>
        /// <param name="mapData">Loaded map data</param>
        private void OnMapChanged(ScenarioMapManager.MapMetaData mapData)
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
                scenarioControllable.Setup(source, mapSignalVariant);
                scenarioControllable.Policy = mapSignal.DefaultControlPolicy;
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
                boxCollider.center = bounds.center-scenarioControllable.transform.position;
                boxCollider.size = bounds.extents*2;
            }
        }

        /// <summary>
        /// Method invoked when current scenario is being reset
        /// </summary>
        private void InstanceOnScenarioReset()
        {
            for (var i = Controllables.Count - 1; i >= 0; i--)
            {
                var controllable = Controllables[i];
                if (!controllable.IsEditableOnMap)
                    continue;
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
        public void CopyPolicy(IControllable target, string policy)
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
        public bool GetCopiedPolicy(IControllable target, out string policy)
        {
            if (target.GetType() == copiedPolicyTarget.GetType())
            {
                policy = copiedPolicy;
                return true;
            }

            policy = "";
            return false;
        }
    }
}