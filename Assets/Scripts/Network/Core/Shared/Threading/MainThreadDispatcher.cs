/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Shared.Threading
{
    using UnityEngine;

    /// <summary>
    /// Unity game object which invokes the dispatched events in the ThreadingUtility
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ThreadingUtility.Dispatcher = this;
        }

        /// <summary>
        /// Unity Update method
        /// </summary>
        private void Update()
        {
            ThreadingUtility.InvokeDispatchedEvents();
        }
    }
}