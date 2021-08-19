/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Elements;
    using Elements.Waypoints;
    using UI.EditElement.Effectors;
    using UI.EditElement.Effectors.Effectors;
    using UnityEngine;

    /// <summary>
    /// Manager for caching and handling all the scenario triggers
    /// </summary>
    public class ScenarioTriggersManager : MonoBehaviour, IScenarioEditorExtension
    {
        /// <summary>
        /// Sample of the effector panel
        /// </summary>
        public DefaultEffectorEditPanel defaultEffectorEditPanel;

        /// <summary>
        /// Custom effector edit panels that are build within the VSE
        /// </summary>
        public List<EffectorEditPanel> customEffectorEditPanels;

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc/>
        public Task Initialize()
        {
            if (IsInitialized)
                return Task.CompletedTask;
            ScenarioManager.Instance.ScenarioElementActivated += OnElementActivatedActivation;
            IsInitialized = true;
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            ScenarioManager.Instance.ScenarioElementActivated -= OnElementActivatedActivation;
            IsInitialized = false;
        }

        /// <summary>
        /// Method called when new scenario element has been activated
        /// </summary>
        /// <param name="selectedElement">Scenario element that has been activated</param>
        private void OnElementActivatedActivation(ScenarioElement selectedElement)
        {
            if (!(selectedElement is ScenarioAgentWaypoint waypoint)) return;
            var trigger = waypoint.LinkedTrigger;
            var effectors = trigger.Trigger.Effectors;
            foreach (var effector in effectors)
            {
                var effectorPanel = customEffectorEditPanels.Find(p => p.EditedEffectorType == effector.GetType());
                if (effectorPanel!=null)
                    effectorPanel.EffectorAddedToTrigger(trigger, effector);
            }
        }
    }
}