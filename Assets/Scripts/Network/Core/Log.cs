/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core
{
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Class that logs infos, warnings and errors. Provides configuration of logged messages.
    /// </summary>
    public static class Log
    {

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public static void Info(object message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// Logs an info message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        /// <param name="context">Context of the message </param>
        public static void Info(object message, Object context)
        {
            Debug.Log(message, context);
        }

        /// <summary>
        /// Logs a warning message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public static void Warning(object message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>
        /// Logs a warning message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        /// <param name="context">Context of the message </param>
        public static void Warning(object message, Object context)
        {
            Debug.LogWarning(message, context);
        }

        /// <summary>
        /// Logs an error message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public static void Error(object message)
        {
            Debug.LogError(message);
        }

        /// <summary>
        /// Logs an error message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        /// <param name="context">Context of the message </param>
        public static void Error(object message, Object context)
        {
            Debug.LogError(message, context);
        }
    }
}