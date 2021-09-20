/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Playback
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Elements.Agents;
    using Elements.Waypoints;
    using Managers;
    using Simulator.Utilities;
    using UI.Playback;
    using UnityEngine;

    /// <summary>
    /// Controller that handles agents movement during the playback
    /// </summary>
    public class AgentsPlayback : PlaybackController
    {
        /// <summary>
        /// Controller that caches and handles movement of one scenario agent
        /// </summary>
        private class AgentPlaybackController
        {
            /// <summary>
            /// A single agent action that will be applied during the playback
            /// </summary>
            private abstract class AgentAction
            {
                /// <summary>
                /// Duration of this playback action
                /// </summary>
                public float Duration { get; protected set; }

                /// <summary>
                /// Playback time when this action starts
                /// </summary>
                public float StartTime { get; }

                /// <summary>
                /// Playback time when this action ends
                /// </summary>
                public float EndTime => StartTime + Duration;

                /// <summary>
                /// Constructor
                /// </summary>
                /// <param name="startTime">Playback time when this action starts</param>
                public AgentAction(float startTime)
                {
                    StartTime = startTime;
                }

                /// <summary>
                /// Performs this action according to the given playback time
                /// </summary>
                /// <param name="time"></param>
                public abstract void Perform(float time);

                /// <summary>
                /// Play the action starting from the initial playback time
                /// </summary>
                /// <param name="coroutinesParent"><see cref="PlaybackPanel"/> that will run the nested coroutines</param>
                /// <param name="initialTime">Playback initial time</param>
                /// <returns>Coroutine IEnumerator</returns>
                public virtual IEnumerator Playback(PlaybackPanel coroutinesParent, float initialTime)
                {
                    for (var t = initialTime; t < EndTime; t += Time.deltaTime)
                    {
                        Perform(t);
                        yield return null;
                    }

                    Perform(EndTime);
                }
            }

            /// <summary>
            /// An agent move action that will translate an agent during the playback
            /// </summary>
            private class AgentMoveAction : AgentAction
            {
                /// <summary>
                /// Agent's rotation speed when traversing a linear path
                /// </summary>
                private const float LinearPathRotationSpeed = 180.0f;

                /// <summary>
                /// Scenario agent that will be moved by this action
                /// </summary>
                private ScenarioAgent Agent { get; }

                /// <summary>
                /// Previous agent's move action used for initial parameters
                /// </summary>
                private AgentMoveAction PreviousAction { get; }

                /// <summary>
                /// Agent's initial position when the action starts
                /// </summary>
                private Vector3 InitialPosition { get; }

                /// <summary>
                /// Agent's destination position applied when the action ends
                /// </summary>
                private Vector3 Destination { get; }

                /// <summary>
                /// Agent's initial rotation when the action starts
                /// </summary>
                private Quaternion InitialRotation { get; }

                /// <summary>
                /// Agent's rotation which will be applied when this action is performed
                /// </summary>
                private Quaternion Rotation { get; }

                /// <summary>
                /// Agent's speed when the action starts
                /// </summary>
                private float InitialSpeed { get; }

                /// <summary>
                /// Agent's destination speed used during the playback movement
                /// </summary>
                public float DestinationSpeed { get; }

                /// <summary>
                /// Agent's acceleration used during the playback movement
                /// </summary>
                private float Acceleration { get; }

                /// <summary>
                /// Time how long the acceleration is used until maximum speed is reached
                /// </summary>
                private float AccelerationDuration { get; }

                /// <summary>
                /// Agent's position after reaching maximum speed
                /// </summary>
                private Vector3 AccelerationDestination { get; }

                /// <summary>
                /// Constructor
                /// </summary>
                /// <param name="startTime">Playback time when this action starts</param>
                /// <param name="agent">Scenario agent that will be moved by this action</param>
                /// <param name="previousAction">Previous agent's move action used for initial parameters</param>
                /// <param name="destination">Agent's destination position applied when the action ends</param>
                /// <param name="rotation">Agent's rotation which will be applied when this action is performed</param>
                /// <param name="destinationSpeed">Agent's destination speed used during the playback movement</param>
                /// <param name="acceleration">Agent's acceleration used during the playback movement</param>
                public AgentMoveAction(float startTime, ScenarioAgent agent, AgentMoveAction previousAction,
                    Vector3 destination, Quaternion rotation, float destinationSpeed, float acceleration) : base(
                    startTime)
                {
                    Agent = agent;
                    InitialPosition = previousAction?.Destination ?? Agent.TransformForPlayback.position;
                    InitialRotation = Agent.TransformForPlayback.rotation;
                    Destination = destination;
                    Rotation = rotation;
                    InitialSpeed = previousAction?.DestinationSpeed ?? 0.0f;
                    DestinationSpeed = destinationSpeed;
                    Acceleration = acceleration;
                    var distance = Vector3.Distance(InitialPosition, destination);
                    if (acceleration > 0)
                    {
                        // If max speed is lower than the initial speed convert acceleration to deceleration
                        if (destinationSpeed < InitialSpeed)
                            Acceleration *= -1;

                        if (!UniformlyAcceleratedMotion.CalculateDuration(acceleration, InitialSpeed,
                            distance, ref destinationSpeed, out var accelerationDuration, out var accelerationDistance))
                        {
                            // Max speed will not be reached with current acceleration
                            AccelerationDestination = destination;
                            DestinationSpeed = destinationSpeed;
                            Duration = AccelerationDuration = accelerationDuration;
                        }
                        else
                        {
                            // Calculate mixed duration of accelerated and linear movements
                            AccelerationDestination = InitialPosition +
                                                      (destination - InitialPosition).normalized * accelerationDistance;
                            var linearDistance = distance - accelerationDistance;
                            AccelerationDuration = accelerationDuration;
                            Duration = AccelerationDuration + linearDistance / DestinationSpeed;
                        }
                    }
                    else
                    {
                        // There is no acceleration - apply max speed for uniform linear movement
                        AccelerationDuration = 0.0f;
                        AccelerationDestination = InitialPosition;
                        Duration = distance / DestinationSpeed;
                    }
                }

                /// <inherit/>
                public override void Perform(float time)
                {
                    var movementTime = Mathf.Clamp(time - StartTime, 0.0f, Duration);
                    if (movementTime <= AccelerationDuration)
                    {
                        // Uniformly accelerated movement
                        var t = 1.0f - (EndTime - time) / (Duration);
                        var passedTime = time - StartTime;
                        Agent.MovementSpeed = Mathf.Lerp(InitialSpeed, DestinationSpeed, t);
                        var moveTranslation =
                            UniformlyAcceleratedMotion.CalculateDistance(Acceleration, InitialSpeed, passedTime) *
                            (Destination - InitialPosition).normalized;
                        Agent.TransformForPlayback.position = InitialPosition + moveTranslation;
                    }
                    else
                    {
                        // Uniform linear movement
                        var t = 1.0f - (EndTime - time) / (Duration - AccelerationDuration);
                        Agent.MovementSpeed = DestinationSpeed;
                        Agent.TransformForPlayback.position = Vector3.Lerp(AccelerationDestination, Destination, t);
                    }

                    // Interpolate the rotation
                    switch (Agent.GetWaypointsPath().PathType)
                    {
                        case WaypointsPathType.Linear:
                            // If it is a linear path just rotate towards with fixed speed
                            var maxRotationDelta = LinearPathRotationSpeed * movementTime;
                            Agent.TransformForPlayback.rotation =
                                Quaternion.RotateTowards(InitialRotation, Rotation, maxRotationDelta);
                            break;

                        case WaypointsPathType.BezierSpline:
                            // For a Bezier path slerp the rotation during the whole duration as it is already preinterpolated during spline generation
                            Agent.TransformForPlayback.rotation =
                                Quaternion.Slerp(InitialRotation, Rotation, movementTime / Duration);
                            break;
                    }
                }
            }

            /// <summary>
            /// An agent trigger action that simulates trigger playback and precache its duration
            /// </summary>
            private class AgentTriggerAction : AgentAction
            {
                /// <summary>
                /// Parent playback panel that will host coroutines
                /// </summary>
                private PlaybackPanel PlaybackPanel { get; }

                /// <summary>
                /// Parent scenario agent that will perform the trigger during the playback
                /// </summary>
                private ScenarioAgent Agent { get; }

                /// <summary>
                /// Trigger that will be performed within this action
                /// </summary>
                private WaypointTrigger Trigger { get; }

                /// <summary>
                /// Constructor
                /// </summary>
                /// <param name="startTime">Playback time when this action starts</param>
                /// <param name="playbackPanel"></param>
                /// <param name="agent"></param>
                /// <param name="trigger"></param>
                public AgentTriggerAction(float startTime, PlaybackPanel playbackPanel, ScenarioAgent agent,
                    WaypointTrigger trigger) : base(
                    startTime)
                {
                    PlaybackPanel = playbackPanel;
                    Agent = agent;
                    Trigger = trigger;
                }

                /// <inherit/>
                public override void Perform(float time)
                {
                    // Ignore performing trigger during the playback
                }

                /// <inherit/>
                public override IEnumerator Playback(PlaybackPanel coroutinesParent, float initialTime)
                {
                    var startTime = Time.time;
                    var coroutines = new Coroutine[Trigger.Effectors.Count];
                    for (var i = 0; i < Trigger.Effectors.Count; i++)
                    {
                        var effector = Trigger.Effectors[i];
                        // Check if the trigger effector is overridden by some custom script
                        var agentsPlayback = PlaybackPanel.GetController<AgentsPlayback>();
                        if (agentsPlayback.triggerEffectorPlaybacks.TryGetValue(effector.GetType(),
                            out var overriddenPlayback))
                        {
                            coroutines[i] =
                                coroutinesParent.StartCoroutine(
                                    overriddenPlayback.Apply(PlaybackPanel, effector, Agent));
                        }
                        else
                        {
                            coroutines[i] = coroutinesParent.StartCoroutine(effector.Apply(Agent));
                        }
                    }

                    for (int i = 0; i < coroutines.Length; i++)
                        yield return coroutines[i];
                    Duration = Time.time - startTime;
                }
            }

            /// <summary>
            /// Scenario agent that will be controlled
            /// </summary>
            public readonly ScenarioAgent agent;

            /// <summary>
            /// Parent <see cref="PlaybackPanel"/> for common precached data
            /// </summary>
            public readonly PlaybackPanel playbackPanel;

            /// <summary>
            /// Position that is applied on reset
            /// </summary>
            private readonly Vector3 initialPosition;

            /// <summary>
            /// Rotation that is applied on reset
            /// </summary>
            private readonly Quaternion initialRotation;

            /// <summary>
            /// Agent's position after playback
            /// </summary>
            private Vector3 destinationPosition;

            /// <summary>
            /// Agent's rotation after playback
            /// </summary>
            private Quaternion destinationRotation;

            /// <summary>
            /// Playback actions that this controller will perform in the order
            /// </summary>
            private readonly List<AgentAction> actions = new List<AgentAction>();

            /// <summary>
            /// Duration of traversing the whole path
            /// </summary>
            public float duration;

            /// <summary>
            /// Decides if the controller will loop actions
            /// </summary>
            public bool IsLooped { get; private set; }
            
            /// <summary>
            /// True if playback is already cached, false otherwise
            /// </summary>
            public bool IsCached { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="parentAgent">Scenario agent that will be controlled</param>
            /// <param name="playbackPanel">Parent <see cref="PlaybackPanel"/> for common precached data</param>
            public AgentPlaybackController(ScenarioAgent parentAgent, PlaybackPanel playbackPanel)
            {
                agent = parentAgent;
                this.playbackPanel = playbackPanel;
                initialPosition = agent.TransformForPlayback.position;
                initialRotation = agent.TransformForPlayback.rotation;
            }

            /// <summary>
            /// Recalculates the agent path that will be traversed
            /// </summary>
            public IEnumerator PrecachePlayback(PlaybackPanel coroutinesParent)
            {
                IsCached = false;
                actions.Clear();
                AgentMoveAction previousMoveAction = null;
                var previousPosition = agent.TransformForPlayback.position;
                var previousRotation = agent.TransformForPlayback.rotation;
                var actionStartTime = 0.0f;
                var playbackStartTime = Time.time;

                // Get the waypoints path for this agent
                var waypointsPath = agent.GetWaypointsPath();

                // If there is a waypoints path, calculate it
                if (waypointsPath == null)
                {
                    duration = 0.0f;
                    yield break;
                }

                switch (waypointsPath.PathType)
                {
                    case WaypointsPathType.Linear:
                        for (var i = 0; i < waypointsPath.Waypoints.Count; i++)
                        {
                            var waypoint = waypointsPath.Waypoints[i];
                            var speed = waypoint.DestinationSpeed;
                            var acceleration = waypoint.Acceleration;
                            // Agent won't move further after stopping
                            if (speed <= 0.0f)
                                break;
                            var position = waypoint.transform.position;
                            var rotation = Quaternion.LookRotation((position - previousPosition).normalized);
                            var action = new AgentMoveAction(actionStartTime, agent, previousMoveAction,
                                position, rotation, speed, acceleration);
                            actions.Add(action);
                            yield return coroutinesParent.StartCoroutine(action.Playback(coroutinesParent,
                                Time.time - playbackStartTime));
                            previousMoveAction = action;
                            previousPosition = position;
                            previousRotation = rotation;
                            actionStartTime += action.Duration;

                            // Precache waypoint trigger
                            if (waypoint is ScenarioAgentWaypoint agentWaypoint &&
                                agentWaypoint.LinkedTrigger.Trigger.Effectors.Count > 0)
                            {
                                var time = Time.time - playbackStartTime;
                                var triggerAction = new AgentTriggerAction(actionStartTime, playbackPanel, agent,
                                    agentWaypoint.LinkedTrigger.Trigger);
                                yield return coroutinesParent.StartCoroutine(
                                    triggerAction.Playback(coroutinesParent, time));
                                actionStartTime += triggerAction.Duration;
                            }
                        }

                        // Create a move action to the waypoint end path as it is not included in the waypoints list
                        if (waypointsPath.EndElement != null && waypointsPath.Waypoints.Count > 0)
                        {
                            var waypoint = waypointsPath.Waypoints[waypointsPath.Waypoints.Count - 1];
                            var speed = waypoint.DestinationSpeed;
                            var acceleration = waypoint.Acceleration;
                            var position = waypointsPath.EndElement.transform.position;
                            var rotation = Quaternion.LookRotation((position - previousPosition).normalized);
                            var action = new AgentMoveAction(actionStartTime, agent, previousMoveAction,
                                position, rotation, speed, acceleration);
                            actions.Add(action);
                            yield return coroutinesParent.StartCoroutine(action.Playback(coroutinesParent,
                                Time.time - playbackStartTime));
                            previousPosition = position;
                            previousRotation = rotation;
                            actionStartTime += action.Duration;
                        }

                        break;
                    case WaypointsPathType.BezierSpline:
                        var waypoints = waypointsPath.CachedBezierSpline.GetBezierWaypoints();
                        for (var i = 1; i < waypoints.Count; i++)
                        {
                            var waypoint = waypoints[i];
                            var speed = waypoint.MaxSpeed;
                            var acceleration = waypoint.Acceleration;
                            //Agent won't move further after stopping
                            if (speed <= 0.0f)
                                break;

                            var position = waypoint.Position + agent.TransformToMove.position;
                            var rotation = Quaternion.Euler(waypoint.Angle);
                            var action = new AgentMoveAction(actionStartTime, agent, previousMoveAction,
                                position, rotation, speed, acceleration);
                            actions.Add(action);
                            yield return coroutinesParent.StartCoroutine(action.Playback(coroutinesParent,
                                Time.time - playbackStartTime));
                            previousMoveAction = action;
                            previousPosition = position;
                            previousRotation = rotation;
                            actionStartTime += action.Duration;

                            // Precache waypoint trigger
                            if (waypoint is AgentWaypointsPath.AgentWaypoint agentWaypoint &&
                                agentWaypoint.Trigger != null &&
                                agentWaypoint.Trigger.Effectors.Count > 0)
                            {
                                var time = Time.time - playbackStartTime;
                                var triggerAction =
                                    new AgentTriggerAction(actionStartTime, playbackPanel, agent,
                                        agentWaypoint.Trigger);
                                yield return coroutinesParent.StartCoroutine(
                                    triggerAction.Playback(coroutinesParent, time));
                                actionStartTime += triggerAction.Duration;
                            }
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                duration = actionStartTime;
                destinationPosition = previousPosition;
                destinationRotation = previousRotation;
                IsLooped = waypointsPath.Loop;
                IsCached = true;
            }

            /// <summary>
            /// Reverts the changes done to agent by this controller
            /// </summary>
            public void RevertChanges()
            {
                agent.TransformForPlayback.position = initialPosition;
                agent.TransformForPlayback.rotation = initialRotation;
            }

            /// <summary>
            /// Apply current playback time to this agent controller
            /// </summary>
            /// <param name="time">Current playback time</param>
            public void ApplyTime(float time)
            {
                if (actions.Count == 0)
                    return;
                if (time >= duration)
                {
                    if (IsLooped)
                    {
                        ApplyTime(time % duration);
                        return;
                    }

                    agent.TransformForPlayback.position = destinationPosition;
                    agent.TransformForPlayback.rotation = destinationRotation;
                }
                else
                {
                    int idx = 0;
                    while (actions[idx].EndTime < time)
                        idx++;
                    actions[idx].Perform(time);
                }
            }
        }

        /// <summary>
        /// Dictionary of all the agent controllers accessed by scenario agent reference
        /// </summary>
        private readonly Dictionary<ScenarioAgent, AgentPlaybackController> agents =
            new Dictionary<ScenarioAgent, AgentPlaybackController>();

        /// <summary>
        /// Dictionary of all <see cref="TriggerEffectorPlayback"/> overriding the trigger effectors
        /// </summary>
        private readonly Dictionary<Type, TriggerEffectorPlayback> triggerEffectorPlaybacks =
            new Dictionary<Type, TriggerEffectorPlayback>();

        /// <inheritdoc/>
        public override void Initialize()
        {
            Duration = 0.0f;
            var triggerPlaybacks =
                ReflectionCache.FindTypes(type => type.IsSubclassOf(typeof(TriggerEffectorPlayback)));
            foreach (var triggerPlayback in triggerPlaybacks)
            {
                if (Activator.CreateInstance(triggerPlayback) is TriggerEffectorPlayback triggerEffectorPlayback)
                    triggerEffectorPlaybacks.Add(triggerEffectorPlayback.OverriddenEffectorType,
                        triggerEffectorPlayback);
            }
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            Reset();
        }

        /// <inheritdoc/>
        public override void PlaybackUpdate(float time)
        {
            foreach (var agentController in agents) agentController.Value.ApplyTime(time);
        }

        /// <inheritdoc/>
        public override IEnumerator PrecachePlayback(PlaybackPanel playbackPanel)
        {
            var agentsManager = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>();
            for (var i = 0; i < agentsManager.Agents.Count; i++)
            {
                var agent = agentsManager.Agents[i];
                var agentController = new AgentPlaybackController(agent, playbackPanel);
                agents.Add(agent, agentController);
            }

            // Start precaching playback on all the controllers
            var coroutines = new List<Coroutine>();
            foreach (var agentController in agents)
            {
                coroutines.Add(
                    playbackPanel.StartCoroutine(agentController.Value.PrecachePlayback(playbackPanel)));
            }

            // Wait for all the coroutines
            var time = 0.0f;
            bool allCoroutinesFinished;
            do
            {
                yield return null;
                time += Time.deltaTime;
                allCoroutinesFinished = true;
                foreach (var agentController in agents)
                {
                    // Lower the flag if this controller did not finish precaching
                    if (!agentController.Value.IsCached)
                    {
                        allCoroutinesFinished = false;
                        continue;
                    }

                    // Update controller if it is precached and looped
                    if (agentController.Value.IsLooped)
                    {
                        agentController.Value.ApplyTime(time);
                    }
                }
            } while (!allCoroutinesFinished);

            // Find the longest playback duration
            Duration = 0.0f;
            foreach (var agentController in agents)
            {
                if (agentController.Value.duration > Duration)
                    Duration = agentController.Value.duration;
            }
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            foreach (var agentController in agents) agentController.Value.RevertChanges();
            agents.Clear();
        }
    }
}