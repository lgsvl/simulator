/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Waypoints
{
    using Agents;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario waypoint representation
    /// </remarks>
    public class ScenarioDestinationPointWaypoint : ScenarioWaypoint
    {
        /// <inheritdoc/>
        public override string ElementType { get; } = "DestinationPointWaypoint";

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        { 
            base.CopyProperties(origin);
            var originWaypoint = origin.GetComponent<ScenarioDestinationPointWaypoint>();
            if (originWaypoint == null) return;
        }

        /// <inheritdoc/>
        public override void Selected()
        {
            base.Selected();

            var extension = ((ScenarioAgent)ParentElement).GetExtension<AgentDestinationPoint>();
            extension?.DestinationPoint.SetVisibility(true);
        }

        /// <inheritdoc/>
        public override void Deselected()
        {
            base.Deselected();
            
            var extension = ((ScenarioAgent)ParentElement).GetExtension<AgentDestinationPoint>();
            extension?.DestinationPoint.SetVisibility(false);
        }
    }
}