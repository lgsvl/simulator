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
    using Managers;
    using UI.Playback;

    /// <summary>
    /// Trigger effector extension for replacing the <see cref="ControlTriggerEffector"/> methods with dedicated implementations for VSE playback
    /// </summary>
    public class ControlTriggerPlayback : TriggerEffectorPlayback
    {
        /// <inheritdoc/>
        public override Type OverriddenEffectorType => typeof(ControlTriggerEffector);

        /// <inheritdoc/>
        public override IEnumerator Apply(PlaybackPanel playbackPanel, TriggerEffector triggerEffector,
            ITriggerAgent triggerAgent)
        {
            ScenarioManager.Instance.logPanel.EnqueueInfo(
                $"{OverriddenEffectorType.Name} is omitted in the playback mode.");
            yield break;
        }
    }
}