/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Controllables
{
    using System.Collections.Generic;
    using Agents;
    using Controllable;
    using Elements;
    using Managers;
    using UnityEngine;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario controllable representation
    /// </remarks>
    public class ScenarioControllable : ScenarioElementWithVariant
    {
        /// <inheritdoc/>
        public override string ElementType => Variant == null ? "Controllable" : Variant.Name;

        /// <inheritdoc/>
        public override bool CanBeMoved => IsEditableOnMap;

        /// <inheritdoc/>
        public override bool CanBeRotated => IsEditableOnMap;

        /// <inheritdoc/>
        public override bool CanBeRemoved => IsEditableOnMap;
        
        /// <inheritdoc/>
        public override bool CanBeCopied => IsEditableOnMap;

        /// <inheritdoc/>
        public override bool CanBeResized => Variant.CanBeResized;

        /// <inheritdoc/>
        public override Transform TransformToResize => OverriddenTransformToResize;

        /// <summary>
        /// Transform that will be resized instead of default transform
        /// </summary>
        public Transform OverriddenTransformToResize { get; set; }

        /// <summary>
        /// This controllable variant
        /// </summary>
        public ControllableVariant Variant => variant as ControllableVariant;

        /// <inheritdoc/>
        public override bool IsEditableOnMap => Variant != null && Variant.controllable.Spawned;
        
        /// <summary>
        /// Currently set policy for this controllable
        /// </summary>
        public List<ControlAction> Policy { get; set; } = new List<ControlAction>();
        
        /// <inheritdoc/>
        public override void Setup(ScenarioElementSource source, SourceVariant variant)
        {
            OverriddenTransformToResize = transform;
            base.Setup(source, variant);
            ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>().RegisterControllable(this);
        }
        
        /// <summary>
        /// Setup method for initializing the required element data
        /// </summary>
        /// <param name="source">Source of this variant</param>
        /// <param name="variant">This agent variant</param>
        /// <param name="initialPolicy">Initial policy that will be set</param>
        public void Setup(ScenarioElementSource source, SourceVariant variant, List<ControlAction> initialPolicy)
        {
            OverriddenTransformToResize = transform;
            base.Setup(source, variant);
            Policy = initialPolicy;
            ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>().RegisterControllable(this);
        }

        /// <inheritdoc/>
        public override void RemoveFromMap()
        {
            base.RemoveFromMap();
            ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>().UnregisterControllable(this);
        }

        /// <inheritdoc/>
        public override void UndoRemove()
        {
            base.UndoRemove();
            ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>().RegisterControllable(this);
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            base.CopyProperties(origin);
            if (origin is ScenarioControllable originControllable)
            {
                Policy.Clear();
                Policy.AddRange(originControllable.Policy);
            }
            ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>().RegisterControllable(this);
        }
    }
}