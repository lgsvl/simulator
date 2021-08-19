/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Playback
{
    using System;
    using System.Collections;
    using System.Linq;
    using Elements.Agents;
    using Managers;
    using UI.Playback;
    using UnityEngine;

    /// <summary>
    /// Trigger effector extension for replacing the <see cref="WaitingPointEffector"/> methods with dedicated implementations for VSE playback
    /// </summary>
    public class WaitingPointPlayback : TriggerEffectorPlayback
    {
        /// <inheritdoc/>
        public override Type OverriddenEffectorType => typeof(WaitingPointEffector);

        /// <inheritdoc/>
        public override IEnumerator Apply(PlaybackPanel playbackPanel, TriggerEffector triggerEffector,
            ITriggerAgent triggerAgent)
        {
            if (!(triggerEffector is WaitingPointEffector waitingPointEffector))
            {
                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Invalid trigger effector passed to the {GetType().Name}.");
                yield break;
            }

            var egos = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().Agents
                .Where(a => a.Source.AgentType == AgentType.Ego).ToArray();
            
            //Make parent npc wait until any ego is closer than the max distance
            var lowestDistance = float.PositiveInfinity;
            do
            {
                foreach (var ego in egos)
                {
                    var distance = Vector3.Distance(waitingPointEffector.ActivatorPoint, ego.TransformForPlayback.position);
                    if (distance < lowestDistance)
                        lowestDistance = distance;
                }

                yield return null;
            } while (lowestDistance > waitingPointEffector.PointRadius);
        }
    }
}