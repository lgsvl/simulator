/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Controllables
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Agents;
    using Controllable;
    using Elements;
    using UI.EditElement.Controllables;
    using UnityEngine;
    using Web;

    /// <summary>
    /// This scenario source handles adding controllable
    /// </summary>
    public class ScenarioControllableSource : ScenarioElementSource
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Prefabs with controllables prefabs
        /// </summary>
        [SerializeField]
        private List<MonoBehaviour> controllablesPrefabs;
        
        /// <summary>
        /// Prefabs that includes custom edit panels for controllables
        /// </summary>
        [SerializeField]
        private List<MonoBehaviour> customEditPanelsPrefabs;
#pragma warning restore 0649

        /// <summary>
        /// Is this source initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc/>
        public override string ElementTypeName { get; } = "Controllable";

        /// <summary>
        /// List of available controllable variants
        /// </summary>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();

        /// <summary>
        /// List of custom edit panels for controllables
        /// </summary>
        public List<IControllableEditPanel> CustomEditPanels { get; } = new List<IControllableEditPanel>();

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="progress">Progress value of the initialization</param>
        /// <returns>Task</returns>
        public Task Initialize(IProgress<float> progress)
        {
            if (IsInitialized)
            {
                progress.Report(1.0f);
                return Task.CompletedTask;
            }
            
            //Load referenced controllables
            foreach (var controllablePrefab in controllablesPrefabs)
            {
                if (controllablePrefab == null)
                    continue;
                var controllable = controllablePrefab.GetComponent<IControllable>();
                if (controllable == null)
                    continue;
                var variant = new ControllableVariant();
                controllable.Spawned = true;
                variant.Setup(controllable.GetType().Name, controllable);
                Variants.Add(variant);
            }
            foreach (var editPanelsPrefab in customEditPanelsPrefabs)
            {
                if (editPanelsPrefab == null)
                    continue;
                var editPanel = editPanelsPrefab.GetComponent<IControllableEditPanel>();
                if (editPanel == null) continue;
                editPanel = Instantiate(editPanelsPrefab, transform).GetComponent<IControllableEditPanel>();
                editPanel.PanelObject.SetActive(false);
                CustomEditPanels.Add(editPanel);
            }
            
            //Import controllables from config
            var controllables = Config.Controllables;
            var controllablesCount = controllables.Count;
            var i = 0;
            foreach (var controllable in controllables)
            {
                if (Variants.Any(v => ((ControllableVariant)v).controllable.GetType() == controllable.GetType()))
                    continue;
                var variant = new ControllableVariant();
                Debug.Log($"Loading controllable {controllable.Key} from the config.");
                controllable.Value.Spawned = true;
                variant.Setup(controllable.Key, controllable.Value);
                Variants.Add(variant);
                var assets = Config.ControllableAssets[controllable.Value];
                foreach (var asset in assets)
                {
                    var editPanel = asset.GetComponent<IControllableEditPanel>();
                    //Add edit panel if same type is not registered yet
                    if (editPanel!=null && customEditPanelsPrefabs.All(panel => panel.GetType() != editPanel.GetType()))
                        CustomEditPanels.Add(editPanel);
                }
                progress.Report((float)++i/controllablesCount);
            }

            //Let all the custom edit panels edit initialized controllable variants
            foreach (var variant in Variants)
            {
                var controllableVariant = variant as ControllableVariant;
                if (controllableVariant == null)
                    continue;
                foreach (var customEditPanel in CustomEditPanels)
                {
                    if (customEditPanel.EditedType == controllableVariant.controllable.GetType())
                        customEditPanel.InitializeVariant(controllableVariant);
                }
            }

            IsInitialized = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            IsInitialized = false;
        }

        /// <summary>
        /// Method that instantiates new <see cref="ScenarioControllable"/> and initializes it with selected variant
        /// </summary>
        /// <param name="variant">Controllable variant which model should be instantiated</param>
        /// <param name="initialPolicy">Policy that will be applied to the new instance, default variant policy will be applied if this parameter is null</param>
        /// <returns><see cref="ScenarioControllable"/> initialized with selected variant</returns>
        public ScenarioControllable GetControllableInstance(ControllableVariant variant, List<ControlAction> initialPolicy = null)
        {
            var newGameObject = new GameObject(ElementTypeName);
            newGameObject.transform.SetParent(transform);
            var scenarioControllable = newGameObject.AddComponent<ScenarioControllable>();
            if (initialPolicy == null)
            {
                initialPolicy = new List<ControlAction>();
                var defaultPolicy = variant.controllable.DefaultControlPolicy;
                if (defaultPolicy!=null && defaultPolicy.Count>0)
                    initialPolicy.AddRange(variant.controllable.DefaultControlPolicy);
            }

            scenarioControllable.Setup(this, variant, initialPolicy);
            SetupNewControllable(scenarioControllable);
            return scenarioControllable;
        }

        public override ScenarioElement GetElementInstance(SourceVariant variant)
        {
            return GetControllableInstance(variant as ControllableVariant);
        }

        /// <summary>
        /// Initializes required components for the new scenario controllable
        /// </summary>
        /// <param name="scenarioControllable">New scenario controllable</param>
        public void SetupNewControllable(ScenarioControllable scenarioControllable)
        {
            var rb = scenarioControllable.gameObject.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.isKinematic = true;
            }
            var colliders = scenarioControllable.gameObject.GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.isTrigger = true;
        }
    }
}