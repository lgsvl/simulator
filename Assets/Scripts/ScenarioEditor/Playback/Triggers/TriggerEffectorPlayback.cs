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
    using UI.Playback;

    /// <summary>
    /// Trigger effector extension for replacing the effector methods with dedicated implementations for VSE playback
    /// </summary>
    public abstract class TriggerEffectorPlayback
    {
        /// <summary>
        /// Trigger effector type that will be overridden in the VSE playback by this script
        /// </summary>
        public abstract Type OverriddenEffectorType { get; }

        /// <summary>
        /// Overridden apply method for the playback mode
        /// </summary>
        /// <param name="playbackPanel">Playback panel that will run coroutines</param>
        /// <param name="triggerEffector">Trigger effector that was overridden by this script</param>
        /// <param name="triggerAgent">Trigger agent affected by the effector</param>
        /// <returns>Coroutine IEnumerator</returns>
        public abstract IEnumerator Apply(PlaybackPanel playbackPanel, TriggerEffector triggerEffector, ITriggerAgent triggerAgent);
    }
}