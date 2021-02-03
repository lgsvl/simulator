/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Controllables
{
    using System;
    using System.Collections.Generic;
    using Controllable;
    using Elements;
    using ScenarioEditor.Controllables;
    using UnityEngine;

    /// <summary>
    /// Interface that allows replacing the default policy edit with custom controllable editor
    /// </summary>
    public interface IControllableEditPanel
    {
        /// <summary>
        /// Controllable type that can be edited with this panel
        /// </summary>
        Type EditedType { get; }
        
        /// <summary>
        /// Game object including this edit panel
        /// </summary>
        GameObject PanelObject { get; }
        
        /// <summary>
        /// Initialization method
        /// </summary>
        void Initialize();

        /// <summary>
        /// Deinitialization method
        /// </summary>
        void Deinitialize();

        /// <summary>
        /// Initialized the new controllable variant that will be edited by this panel
        /// </summary>
        /// <param name="variant">New controllable variant</param>
        void InitializeVariant(ControllableVariant variant);

        /// <summary>
        /// Initialized the new scenario controllable that will be edited by this panel
        /// </summary>
        /// <param name="scenarioControllable">New scenario controllable</param>
        void InitializeControllable(ScenarioControllable scenarioControllable);

        /// <summary>
        /// Method called when another scenario controllable has been selected
        /// </summary>
        /// <param name="scenarioControllable">Scenario controllable that has been selected</param>
        /// <param name="policy">Current policy of this controllable</param>
        void Setup(ScenarioControllable scenarioControllable, List<ControlAction> policy);
    }
}
