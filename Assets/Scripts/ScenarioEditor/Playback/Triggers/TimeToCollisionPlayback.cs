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
    /// Trigger effector extension for replacing the <see cref="TimeToCollisionEffector"/> methods with dedicated implementations for VSE playback
    /// </summary>
    public class TimeToCollisionPlayback : TriggerEffectorPlayback
    {
        /// <inheritdoc/>
        public override Type OverriddenEffectorType => typeof(TimeToCollisionEffector);

        /// <inheritdoc/>
        public override IEnumerator Apply(PlaybackPanel playbackPanel, TriggerEffector triggerEffector,
            ITriggerAgent triggerAgent)
        {
            if (!(triggerEffector is TimeToCollisionEffector ttcEffector))
            {
                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Invalid trigger effector passed to the {GetType().Name}.");
                yield break;
            }

            var egos = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().Agents
                .Where(a => a.Source.AgentType == AgentType.Ego);
            ScenarioAgent collisionEgo = null;
            var lowestTTC = TimeToCollisionEffector.TimeToCollisionLimit;
            foreach (var ego in egos)
            {
                var ttc = ttcEffector.CalculateTTC(ego, triggerAgent, triggerAgent.MovementSpeed);
                if (ttc >= lowestTTC || ttc < 0.0f) continue;

                lowestTTC = ttc;
                collisionEgo = ego;
            }

            yield return playbackPanel.StartCoroutine(ttcEffector.Apply(lowestTTC, collisionEgo, triggerAgent));
        }
    }
}