/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents.Triggers
{
    using Elements;
    using Elements.Triggers;

    /// <summary>
    /// Object that represents effector in the scenario
    /// </summary>
    public abstract class ScenarioEffectorObject : ScenarioElement
    {
        /// <summary>
        /// Parent trigger of this object
        /// </summary>
        protected ScenarioTrigger trigger;
        
        /// <summary>
        /// Effector of this zone
        /// </summary>
        protected TriggerEffector effector;
        
        /// <inheritdoc/>
        public override void RemoveFromMap()
        {
            base.RemoveFromMap();
            if (trigger != null)
                trigger.Trigger.RemoveEffector(effector);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            //Effector object is disposed by the <see cref="ScenarioTrigger"/>
        }

        /// <summary>
        /// Setups the zone object with the data
        /// </summary>
        /// <param name="trigger">Parent trigger of this zone</param>
        /// <param name="effector">Parent effector of this zone</param>
        public virtual void Setup(ScenarioTrigger trigger, TriggerEffector effector)
        {
            this.trigger = trigger;
            this.effector = effector;
        }

        /// <summary>
        /// Refresh the effector object with current state
        /// </summary>
        public abstract void Refresh();

        /// <summary>
        /// Method invoked before this effector object is serialized
        /// </summary>
        public abstract void OnBeforeSerialize();
    }
}