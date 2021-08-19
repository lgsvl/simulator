/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Waypoints
{
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Scenario destination point extension that handles the playback waypoints path
    /// </summary>
    public class DestinationPointWaypointsPath : WaypointsPath
    {
        /// <summary>
        /// Scenario destination point that this object extends
        /// </summary>
        public ScenarioDestinationPoint ParentDestinationPoint { get; private set; }

        /// <inheritdoc/>
        protected override string JsonNodeName => "playbackWaypointsPath";

        /// <inheritdoc/>
        public override void Initialize(ScenarioElement parentElement)
        {
            ParentDestinationPoint = (ScenarioDestinationPoint) parentElement;
            base.Initialize(ParentDestinationPoint.ParentAgent);
            SetStartEndElements(ParentDestinationPoint.ParentAgent, ParentDestinationPoint);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            base.Deinitialize();
            ParentDestinationPoint = null;
        }

        /// <inheritdoc/>
        public override ScenarioWaypoint GetWaypointInstance()
        {
            return ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>()
                .GetWaypointInstance<ScenarioDestinationPointWaypoint>();
        }

        /// <inheritdoc/>
        protected override Material GetWaypointsMaterial()
        {
            return ParentDestinationPoint.ParentAgent.Source.WaypointsMaterial;
        }
    }
}