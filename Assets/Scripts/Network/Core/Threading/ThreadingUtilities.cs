/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Utility methods for threading in the Unity
    /// </summary>
    public static class ThreadingUtilities
    {
        /// <summary>
        /// Events dispatched to be called from the Unity main thread
        /// </summary>
        private static readonly List<Action> DispatchedEvents = new List<Action>();

        /// <summary>
        /// Dispatcher that invokes dispatched events on the main thread
        /// </summary>
        public static MainThreadDispatcher Dispatcher { get; internal set; }

        /// <summary>
        /// Last Time.timeScale value checked from Update on main thread.
        /// </summary>
        public static float LastTimeScale => Dispatcher == null ? 0f : Dispatcher.LastTimeScale;

        /// <summary>
        /// Invokes callback after requested milliseconds from a new thread
        /// </summary>
        /// <param name="milliseconds">Milliseconds delaying the callback</param>
        /// <param name="callback">Callback invoked after the delay</param>
        public static void DelayedInvoke(int milliseconds, Action callback)
        {
            var thread = new Thread(
                () =>
                {
                    Thread.Sleep(milliseconds);
                    callback();
                }
            );
            thread.Start();
        }

        /// <summary>
        /// Dispatches the event to be invoked from the Unity main thread
        /// </summary>
        /// <param name="eventToDispatch">Event dispatched to be called from the Unity main thread</param>
        public static void DispatchToMainThread(Action eventToDispatch)
        {
//            if (Dispatcher == null)
//                Log.Warning(
//                    $"There is no dispatcher set in the {typeof(ThreadingUtility).Name}. Dispatched events will be invoked on main thread when {typeof(MainThreadDispatcher).Name} is available on a scene.");
            lock (DispatchedEvents)
                DispatchedEvents.Add(eventToDispatch);
        }

        /// <summary>
        /// Invokes all the dispatched events from the current thread
        /// </summary>
        internal static void InvokeDispatchedEvents()
        {
            lock (DispatchedEvents)
            {
                if (DispatchedEvents.Count == 0) return;
                try
                {
                    for (var i = 0; i < DispatchedEvents.Count; i++)
                    {
                        var action = DispatchedEvents[i];
                        action.Invoke();
                    }
                }
                finally
                {
                    DispatchedEvents.Clear();  
                }
            }
        }
    }
}