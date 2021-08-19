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
    /// Trigger effector extension for replacing the <see cref="WaitForDistanceEffector"/> methods with dedicated implementations for VSE playback
    /// </summary>
    public class WaitForDistancePlayback : TriggerEffectorPlayback
    {
        /// <inheritdoc/>
        public override Type OverriddenEffectorType => typeof(WaitForDistanceEffector);

        /// <inheritdoc/>
        public override IEnumerator Apply(PlaybackPanel playbackPanel, TriggerEffector triggerEffector,
            ITriggerAgent triggerAgent)
        {
            if (!(triggerEffector is WaitForDistanceEffector waitForDistanceEffector))
            {
                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Invalid trigger effector passed to the {GetType().Name}.");
                yield break;
            }

            var egos = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().Agents
                .Where(a => a.Source.AgentType == AgentType.Ego).ToArray();
            
            //Make parent npc wait until any ego is closer than the max distance
            float lowestDistance;
            do
            {
                yield return null;
                lowestDistance = float.PositiveInfinity;
                foreach (var ego in egos)
                {
                    var distance = Vector3.Distance(triggerAgent.AgentTransform.position, ego.TransformForPlayback.position);
                    if (distance < lowestDistance)
                        lowestDistance = distance;
                }

                yield return null;
            } while (lowestDistance > waitForDistanceEffector.MaxDistance);
        }
    }
}