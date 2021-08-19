/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Waypoints
{
    using System.Collections.Generic;
    using Agents;
    using Managers;
    using SimpleJSON;
    using Simulator.Utilities;
    using Triggers;
    using UnityEngine;

    /// <summary>
    /// Scenario agent extension that handles the waypoints
    /// </summary>
    public class AgentWaypointsPath : WaypointsPath
    {
        /// <summary>
        /// Minimal agent waypoint implementation for Bezier calculations
        /// </summary>
        public class AgentWaypoint : Waypoint
        {
            /// <summary>
            /// Waypoint trigger linked to this agent waypoint
            /// </summary>
            public WaypointTrigger Trigger { get; set; }
            
            /// <inheritdoc/>
            public override IWaypoint Clone()
            {
                return new AgentWaypoint()
                {
                    Position = Position,
                    Angle = Angle,
                    MaxSpeed = MaxSpeed,
                    Acceleration = Acceleration,
                    Trigger = Trigger
                };
            }

            /// <inheritdoc/>
            public override IWaypoint GetControlPoint()
            {
                return new AgentWaypoint()
                {
                    Position = Position,
                    Angle = Angle,
                    MaxSpeed = MaxSpeed,
                    Acceleration = Acceleration,
                    Trigger = null
                };
            }
        }
        
        /// <summary>
        /// Scenario agent that this object extends
        /// </summary>
        public ScenarioAgent ParentAgent { get; private set; }
        
        /// <summary>
        /// Included triggers that will influence this agent
        /// </summary>
        private readonly List<ScenarioTrigger> triggers = new List<ScenarioTrigger>();

        /// <inheritdoc/>
        public override void Initialize(ScenarioElement parentElement)
        {
            base.Initialize(parentElement);
            ParentAgent = (ScenarioAgent) parentElement;
            SetStartEndElements(parentElement, null);
            ParentAgent.ExtensionAdded += ParentAgentOnExtensionAdded;
            ParentAgent.ExtensionRemoved += ParentAgentOnExtensionRemoved;
            var behaviourExtension = ParentAgent.GetExtension<AgentBehaviour>();
            if (behaviourExtension != null)
                behaviourExtension.BehaviourChanged += ParentAgentOnBehaviourChanged;
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            ParentAgent.ExtensionAdded -= ParentAgentOnExtensionAdded;
            ParentAgent.ExtensionRemoved -= ParentAgentOnExtensionRemoved;
            var behaviourExtension = ParentAgent.GetExtension<AgentBehaviour>();
            if (behaviourExtension != null)
                behaviourExtension.BehaviourChanged -= ParentAgentOnBehaviourChanged;
            base.Deinitialize();
            ParentAgent = null;
        }

        /// <inheritdoc/>
        public override ScenarioWaypoint GetWaypointInstance()
        {
            return ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>()
                .GetWaypointInstance<ScenarioAgentWaypoint>();
        }

        /// <inheritdoc/>
        protected override Material GetWaypointsMaterial()
        {
            return ParentAgent.Source.WaypointsMaterial;
        }

        /// <inheritdoc/>
        protected override Waypoint GetBezierWaypoint(int index)
        {
            return new AgentWaypoint
            {
                Position = waypoints[index].TransformToMove.localPosition,
                Angle = waypoints[index].TransformToRotate.localRotation.eulerAngles,
                MaxSpeed = waypoints[index].DestinationSpeed,
                Acceleration = waypoints[index].Acceleration,
                Trigger = ((ScenarioAgentWaypoint)waypoints[index]).LinkedTrigger.Trigger
            };
        }

        /// <summary>
        /// Method invoked when an extension is added to the parent agent
        /// </summary>
        /// <param name="extension">Added extension</param>
        private void ParentAgentOnExtensionAdded(IScenarioElementExtension extension)
        {
            if (extension is AgentBehaviour behaviourExtension)
                behaviourExtension.BehaviourChanged += ParentAgentOnBehaviourChanged;
        }

        /// <summary>
        /// Method invoked when an extension is removed from the parent agent
        /// </summary>
        /// <param name="extension">Removed extension</param>
        private void ParentAgentOnExtensionRemoved(IScenarioElementExtension extension)
        {
            if (extension is AgentBehaviour behaviourExtension)
                behaviourExtension.BehaviourChanged -= ParentAgentOnBehaviourChanged;
        }

        /// <inheritdoc/>
        protected override void SerializeWaypoint(ScenarioWaypoint waypoint, int waypointIndex, JSONObject waypointNode)
        {
            base.SerializeWaypoint(waypoint, waypointIndex, waypointNode);
            if (waypoint is ScenarioAgentWaypoint agentWaypoint)
            {
                waypointNode.Add("waitTime", new JSONNumber(agentWaypoint.WaitTime));
                AddTriggerNode(waypointNode, agentWaypoint.LinkedTrigger);
            }
        }

        /// <summary>
        /// Adds triggers nodes to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioTrigger">Scenario trigger to serialize</param>
        private void AddTriggerNode(JSONObject data, ScenarioTrigger scenarioTrigger)
        {
            scenarioTrigger.OnBeforeSerialize();
            var triggerNode = scenarioTrigger.Trigger.SerializeTrigger();
            data.Add("trigger", triggerNode);
        }

        /// <inheritdoc/>
        protected override void DeserializeWaypoint(ScenarioWaypoint waypoint, JSONNode waypointNode)
        {
            base.DeserializeWaypoint(waypoint, waypointNode);
            if (waypoint is ScenarioAgentWaypoint agentWaypoint)
            {
                var waitTime = waypointNode["waitTime"];
                if (waitTime == null)
                    waitTime = waypointNode["wait_time"];
                agentWaypoint.WaitTime = waitTime;
                DeserializeTrigger(agentWaypoint.LinkedTrigger, waypointNode["trigger"]);
            }
        }

        /// <summary>
        /// Deserializes a trigger from the json data
        /// </summary>
        /// <param name="trigger">Trigger object to fill with effectors</param>
        /// <param name="triggerNode">Json data with a trigger</param>
        private void DeserializeTrigger(ScenarioTrigger trigger, JSONNode triggerNode)
        {
            trigger.Trigger = WaypointTrigger.DeserializeTrigger(triggerNode);
            trigger.TargetAgentType = ParentAgent.Type;
        }

        /// <summary>
        /// Method invoked when the parent agent's behaviour changes
        /// </summary>
        /// <param name="newBehaviour">New behaviour of the parent agent</param>
        private void ParentAgentOnBehaviourChanged(string newBehaviour)
        {
            var isActive = ParentAgent.Source.AgentSupportWaypoints(ParentAgent);
            SetActive(isActive);
        }


        /// <inheritdoc/>
        public override void AddWaypoint(ScenarioWaypoint waypoint, ScenarioWaypoint previousWaypoint)
        {
            base.AddWaypoint(waypoint, previousWaypoint);
            if (waypoint is ScenarioAgentWaypoint agentWaypoint)
            {
                AddTrigger(agentWaypoint.LinkedTrigger);
                agentWaypoint.LinkedTrigger.TargetAgentType = ParentAgent.Type;
            }
        }

        /// <inheritdoc/>
        public override void RemoveWaypoint(ScenarioWaypoint waypoint)
        {
            if (waypoint is ScenarioAgentWaypoint agentWaypoint)
            {
                RemoveTrigger(agentWaypoint.LinkedTrigger);
            }
            base.RemoveWaypoint(waypoint);
        }

        /// <summary>
        /// Adds trigger to this agent
        /// </summary>
        /// <param name="trigger">Trigger that will be added to this agent</param>
        public void AddTrigger(ScenarioTrigger trigger)
        {
            triggers.Add(trigger);
            trigger.TargetAgentType = ParentAgent.Source.AgentType;
        }

        /// <summary>
        /// Removes trigger from this agent
        /// </summary>
        /// <param name="trigger">Trigger that will be removed from this agent</param>
        public void RemoveTrigger(ScenarioTrigger trigger)
        {
            triggers.Remove(trigger);
        }
    }
}