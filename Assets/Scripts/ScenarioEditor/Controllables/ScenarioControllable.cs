/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Controllables
{
    using Agents;
    using Elements;
    using Managers;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario controllable representation
    /// </remarks>
    public class ScenarioControllable : ScenarioElementWithVariant
    {
        /// <inheritdoc/>
        public override bool CanBeCopied => true;
        
        /// <summary>
        /// This controllable variant
        /// </summary>
        public ControllableVariant Variant => variant as ControllableVariant;
        
        /// <inheritdoc/>
        public override void Setup(ScenarioElementSource source, SourceVariant variant)
        {
            base.Setup(source, variant);
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
            ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>().RegisterControllable(this);
        }
    }
}