/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Controllables
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Agents;
    using Controllable;
    using UnityEngine;

    /// <summary>
    /// Data describing a single controllable variant
    /// </summary>
    public class ControllableVariant : SourceVariant
    {
        /// <summary>
        /// Name of this controllable variant
        /// </summary>
        protected string name;
        
        /// <summary>
        /// Description of this controllable variant
        /// </summary>
        protected string description;

        /// <summary>
        /// <see cref="IControllable"/> bound to this source variant
        /// </summary>
        public IControllable controllable;

        /// <inheritdoc/>
        public override string Name => name;

        /// <inheritdoc/>
        public override string Description => description;

        /// <inheritdoc/>
        public override GameObject Prefab => controllable.Spawned ? controllable.gameObject : null;
        
        /// <summary>
        /// True if this controllable variant can be resized
        /// </summary>
        public bool CanBeResized { get; set; }

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
            var sb = new StringBuilder();
            sb.Append("Control type: ");
            sb.Append(controllable.ControlType);
            if (controllable.ValidActions.Length > 0)
            {
                sb.Append("\nValid actions: ");
                for (var i = 0; i < controllable.ValidActions.Length; i++)
                {
                    var validAction = controllable.ValidActions[i];
                    sb.Append(validAction);
                    if (i<controllable.ValidActions.Length-1)
                        sb.Append(", ");
                }
            }

            if (controllable.ValidStates.Length > 0)
            {
                sb.Append("\nValid states: ");
                for (var i = 0; i < controllable.ValidStates.Length; i++)
                {
                    var validState = controllable.ValidStates[i];
                    sb.Append(validState);
                    if (i<controllable.ValidStates.Length-1)
                        sb.Append(", ");
                }
            }

            description = sb.ToString();
        }
        
        /// <inheritdoc/>
        public override Task Prepare(IProgress<SourceVariant> progress = null)
        {
            progress?.Report(this);
            return Task.CompletedTask;
        }
    }
}
