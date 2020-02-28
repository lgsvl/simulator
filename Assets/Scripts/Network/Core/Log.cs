/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core
{
    using UnityEngine;

    /// <summary>
    /// Class that logs infos, warnings and errors. Provides configuration of logged messages.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Are infos enabled for logging system
        /// </summary>
        public static bool InfosEnabled = false;
        
        /// <summary>
        /// Are warnings enabled for logging system
        /// </summary>
        public static bool WarningsEnabled = true;
        
        /// <summary>
        /// Are errors enabled for logging system
        /// </summary>
        public static bool ErrorsEnabled = true;

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public static void Info(object message)
        {
            if (InfosEnabled)
                Debug.Log(message);
        }

        /// <summary>
        /// Logs an info message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        /// <param name="context">Context of the message </param>
        public static void Info(object message, Object context)
        {
            if (InfosEnabled)
                Debug.Log(message, context);
        }

        /// <summary>
        /// Logs a warning message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public static void Warning(object message)
        {
            if (WarningsEnabled)
                Debug.LogWarning(message);
        }

        /// <summary>
        /// Logs a warning message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        /// <param name="context">Context of the message </param>
        public static void Warning(object message, Object context)
        {
            if (WarningsEnabled)
                Debug.LogWarning(message, context);
        }

        /// <summary>
        /// Logs an error message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public static void Error(object message)
        {
            if (ErrorsEnabled)
                Debug.LogError(message);
        }

        /// <summary>
        /// Logs an error message with given context
        /// </summary>
        /// <param name="message">Message to be logged</param>
        /// <param name="context">Context of the message </param>
        public static void Error(object message, Object context)
        {
            if (ErrorsEnabled)
                Debug.LogError(message, context);
        }
    }
}