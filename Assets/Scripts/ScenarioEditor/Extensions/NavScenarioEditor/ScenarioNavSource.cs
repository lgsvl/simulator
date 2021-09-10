/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Nav
{
    using System.Collections.Generic;
    using Agents;
    using Elements;
    using Input;
    using Managers;
    using Map;

    /// <summary>
    /// Scenario source of the Nav elements like <see cref="ScenarioNavOrigin"/>
    /// </summary>
    public class ScenarioNavSource : ScenarioElementSource
    {
        /// <inheritdoc/>
        public override string ElementTypeName => "NavElements";

        /// <inheritdoc/>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();

        /// <inheritdoc/>
        public override void OnVariantSelected(SourceVariant variant)
        {
            var availableInstance = ScenarioManager.Instance.GetExtension<ScenarioNavExtension>()
                .GetVariantInstance(variant);
            if (availableInstance != null)
            {
                var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
                inputManager.FocusOnScenarioElement(availableInstance);
            }
            else
            {
                base.OnVariantSelected(variant);
            }
        }

        /// <inheritdoc/>
        public override ScenarioElement GetElementInstance(SourceVariant variant)
        {
            var newPoint = Instantiate(variant.Prefab, transform);
            var scenarioNavOrigin = newPoint.AddComponent<ScenarioNavOrigin>();
            var navOrigin = newPoint.GetComponent<NavOrigin>();
            if (navOrigin == null)
                navOrigin = newPoint.AddComponent<NavOrigin>();
            scenarioNavOrigin.Setup(navOrigin, false);
            return scenarioNavOrigin;
        }
    }
}