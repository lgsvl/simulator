/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using System.Collections.Generic;
    using Agents;

    /// <remarks>
    /// Scenario trigger data
    /// </remarks>
    public class ScenarioTrigger
    {
        /// <summary>
        /// Parent agent which includes this trigger
        /// </summary>
        public ScenarioAgent ParentAgent { get; set; }
        
        /// <summary>
        /// Waypoint that is linked to this trigger
        /// </summary>
        public ScenarioWaypoint LinkedWaypoint { get; set; }

        /// <summary>
        /// Effectors that will be invoked on this trigger
        /// </summary>
        public WaypointTrigger Trigger { get; set; } = new WaypointTrigger();
    }
}