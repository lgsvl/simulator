/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging.Data
{
    /// <summary>
    /// Distributed animator command type
    /// </summary>
    public enum AnimatorCommandType
    {
        /// <summary>
        /// Sets animator float value using parameter id
        /// </summary>
        SetFloatById = 0,
        
        /// <summary>
        /// Sets animator float value using parameter name
        /// </summary>
        SetFloatByName = 1
    }
}