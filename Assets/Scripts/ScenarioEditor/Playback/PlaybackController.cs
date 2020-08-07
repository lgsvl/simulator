/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Playback
{
    using UnityEngine;

    /// <summary>
    /// Playback controller that handles predefined scenario elements
    /// </summary>
    public abstract class PlaybackController : MonoBehaviour
    {
        /// <summary>
        /// Duration of this controller playback
        /// </summary>
        public float Duration { get; protected set; }

        /// <summary>
        /// Initializes the controller for the playbacks
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Deinitializes the controller
        /// </summary>
        public abstract void Deinitialize();

        /// <summary>
        /// Updates the controller according to the current playback time
        /// </summary>
        /// <param name="time">Current playback time</param>
        public abstract void PlaybackUpdate(float time);
        
        /// <summary>
        /// Reset changes done by this controller
        /// </summary>
        public abstract void Reset();
    }
}