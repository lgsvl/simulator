/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Controllables
{
    using System.Threading.Tasks;
    using Agents;
    using Controllable;
    using Managers;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Data describing a single controllable variant
    /// </summary>
    public class ControllableVariant : SourceVariant
    {
        /// <summary>
        /// Name of this controllable variant
        /// </summary>
        public string name;

        /// <summary>
        /// <see cref="IControllable"/> bound to this source variant
        /// </summary>
        public IControllable controllable;

        /// <inheritdoc/>
        public override string Name => name;

        /// <inheritdoc/>
        public override GameObject Prefab => controllable.gameObject;

        /// <summary>
        /// Setup the controllable variant with the required data
        /// </summary>
        /// <param name="name">Name of this controllable variant</param>
        /// <param name="controllable"><see cref="IControllable"/> bound to this source variant</param>
        public void Setup(string name, IControllable controllable)
        {
            this.name = name;
            this.controllable = controllable;
            IsPrepared = Prefab != null;
        }
        
        /// <inheritdoc/>
        public override Task Prepare()
        {
            return Task.CompletedTask;
        }
    }
}
