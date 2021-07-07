/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Playback
{
    using System;
    using System.Collections.Generic;
    using Agents;
    using Elements.Agents;
    using Managers;
    using Simulator.Utilities;
    using UnityEngine;

    /// <summary>
    /// Controller that handles agents movement during the playback
    /// </summary>
    public class AgentsPlayback : PlaybackController
    {
        /// <summary>
        /// Controller that caches and handles movement of one scenario agent
        /// </summary>
        private class AgentController
        {
            /// <summary>
            /// Scenario agent that will be controlled
            /// </summary>
            public readonly ScenarioAgent agent;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="parentAgent">Scenario agent that will be controlled</param>
            public AgentController(ScenarioAgent parentAgent)
            {
                agent = parentAgent;
            }

            /// <summary>
            /// Path positions that will be traversed by this agent
            /// </summary>
            private readonly List<Vector3> pathPositions = new List<Vector3>();

            /// <summary>
            /// Path angles that will be applies when reaching corresponding positions
            /// </summary>
            private readonly List<Quaternion> pathAngles = new List<Quaternion>();

            /// <summary>
            /// Path arrival times at which agent will reach the path position
            /// </summary>
            private readonly List<float> pathArrivals = new List<float>();

            /// <summary>
            /// Duration of traversing the whole path
            /// </summary>
            public float duration;

            /// <summary>
            /// Prepares data for the next playback
            /// </summary>
            public void PrepareForPlay()
            {
                RecalculatePath();
            }

            /// <summary>
            /// Recalculates the agent path that will be traversed
            /// </summary>
            private void RecalculatePath()
            {
                pathPositions.Clear();
                pathArrivals.Clear();
                var previousPosition = agent.TransformForPlayback.position;
                pathPositions.Add(previousPosition);
                pathAngles.Add(agent.TransformToRotate.rotation);
                var arrival = 0.0f;
                pathArrivals.Add(arrival);
                var waypointsExtension = agent.GetExtension<AgentWaypoints>();
                if (waypointsExtension != null)
                {
                    switch (waypointsExtension.PathType)
                    {
                        case WaypointsPathType.Linear:
                            for (var i = 0; i < waypointsExtension.Waypoints.Count; i++)
                            {
                                var waypoint = waypointsExtension.Waypoints[i];
                                var speed = waypoint.Speed;
                                //Agent won't move further after stopping
                                if (speed <= 0.0f)
                                    break;
                                var position = waypoint.transform.position;
                                pathPositions.Add(position);
                                pathAngles.Add(Quaternion.LookRotation((position - previousPosition).normalized));
                                var distance = Vector3.Distance(previousPosition, position);
                                arrival += distance / speed;
                                pathArrivals.Add(arrival);
                                previousPosition = position;
                            }

                            break;
                        case WaypointsPathType.BezierSpline:
                            var waypoints = waypointsExtension.CachedBezierSpline.GetBezierWaypoints();
                            for (var i = 1; i < waypoints.Count; i++)
                            {
                                var waypoint = waypoints[i];
                                var speed = waypoint.Speed;
                                //Agent won't move further after stopping
                                if (speed <= 0.0f)
                                    break;
                                var position = waypoint.Position + agent.transform.position;
                                pathPositions.Add(position);
                                pathAngles.Add(Quaternion.Euler(waypoint.Angle));
                                var distance = Vector3.Distance(previousPosition, position);
                                arrival += distance / speed;
                                pathArrivals.Add(arrival);
                                previousPosition = position;
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                duration = pathArrivals[pathArrivals.Count - 1];
            }

            /// <summary>
            /// Reverts the changes done to agent by this controller
            /// </summary>
            public void RevertChanges()
            {
                if (pathPositions.Count > 0)
                    agent.TransformForPlayback.position = pathPositions[0];
                if (pathPositions.Count > 1)
                    agent.TransformForPlayback.rotation =
                        Quaternion.LookRotation((pathPositions[1] - pathPositions[0]).normalized);
            }

            /// <summary>
            /// Apply current playback time to this agent controller
            /// </summary>
            /// <param name="time">Current playback time</param>
            public void ApplyTime(float time)
            {
                if (pathArrivals.Count == 1)
                    return;
                if (time >= duration)
                {
                    agent.TransformForPlayback.position = pathPositions[pathPositions.Count - 1];
                    agent.TransformForPlayback.rotation = pathAngles[pathAngles.Count - 1];
                }
                else
                {
                    int idx = 1;
                    while (pathArrivals[idx] < time)
                        idx++;

                    var t = 1.0f - (pathArrivals[idx] - time) / (pathArrivals[idx] - pathArrivals[idx - 1]);
                    agent.TransformForPlayback.position = Vector3.Lerp(pathPositions[idx - 1], pathPositions[idx], t);
                    agent.TransformForPlayback.rotation = pathAngles[idx];
                }
            }
        }

        /// <summary>
        /// Dictionary of all the agent controllers accessed by scenario agent reference
        /// </summary>
        private readonly Dictionary<ScenarioAgent, AgentController> agents =
            new Dictionary<ScenarioAgent, AgentController>();

        /// <inheritdoc/>
        public override void Initialize()
        {
            Duration = 0.0f;
            var agentsManager = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>();
            for (var i = 0; i < agentsManager.Agents.Count; i++)
            {
                var agent = agentsManager.Agents[i];
                var path = new AgentController(agent);
                agents.Add(agent, path);
                path.PrepareForPlay();
                if (path.duration > Duration)
                    Duration = path.duration;
            }
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            agents.Clear();
        }

        /// <inheritdoc/>
        public override void PlaybackUpdate(float time)
        {
            foreach (var path in agents) path.Value.ApplyTime(time);
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            foreach (var path in agents) path.Value.RevertChanges();
        }
    }
}