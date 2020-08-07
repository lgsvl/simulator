/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Playback
{
    using System.Collections.Generic;
    using Agents;
    using Managers;
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
            private List<Vector3> pathPositions = new List<Vector3>();

            /// <summary>
            /// Path arrival times at which agent will reach the path position
            /// </summary>
            private List<float> pathArrivals = new List<float>();

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
                var arrival = 0.0f;
                pathArrivals.Add(arrival);
                for (var i = 0; i < agent.Waypoints.Count; i++)
                {
                    var waypoint = agent.Waypoints[i];
                    var speed = waypoint.Speed;
                    var position = waypoint.transform.position;
                    pathPositions.Add(position);
                    var distance = Vector3.Distance(previousPosition, position);
                    arrival += distance / speed;
                    pathArrivals.Add(arrival);
                    previousPosition = position;
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
                    if (pathPositions.Count > 1)
                        agent.TransformForPlayback.rotation = Quaternion.LookRotation(
                            (pathPositions[pathPositions.Count - 1] - pathPositions[pathPositions.Count - 2])
                            .normalized);
                }
                else
                {
                    int idx = 1;
                    while (pathArrivals[idx] < time)
                        idx++;

                    var t = 1.0f - (pathArrivals[idx] - time) / (pathArrivals[idx] - pathArrivals[idx - 1]);
                    agent.TransformForPlayback.position = Vector3.Lerp(pathPositions[idx - 1], pathPositions[idx], t);
                    agent.TransformForPlayback.rotation = Quaternion.LookRotation(
                        (pathPositions[idx] - pathPositions[idx - 1]).normalized);
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
            for (var i = 0; i < ScenarioManager.Instance.agentsManager.Agents.Count; i++)
            {
                var agent = ScenarioManager.Instance.agentsManager.Agents[i];
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