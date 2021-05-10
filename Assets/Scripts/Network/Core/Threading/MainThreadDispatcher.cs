/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Threading
{
    using UnityEngine;

    /// <summary>
    /// Unity game object which invokes the dispatched events in the ThreadingUtility
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private readonly object timeLock = new object();
        private float lastTimeScale;

        /// <summary>
        /// Last Time.timeScale value checked from Update on main thread.
        /// </summary>
        internal float LastTimeScale
        {
            get
            {
                lock (timeLock)
                    return lastTimeScale;
            }
            private set
            {
                lock (timeLock)
                    lastTimeScale = value;
            }
        }

        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            ThreadingUtilities.Dispatcher = this;
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            if (ThreadingUtilities.Dispatcher == this)
                ThreadingUtilities.Dispatcher = null;
        }

        /// <summary>
        /// Unity Update method
        /// </summary>
        private void Update()
        {
            LastTimeScale = Time.timeScale;
            ThreadingUtilities.InvokeDispatchedEvents();
        }
    }
}