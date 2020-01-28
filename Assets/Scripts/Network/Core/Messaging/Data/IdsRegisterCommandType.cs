/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging.Data
{
    /// <summary>
    /// Types of the internal commands
    /// </summary>
    public enum IdsRegisterCommandType
    {
        /// <summary>
        /// Binds the identifier with addressed key
        /// </summary>
        BindIdAndKey = 0,
        
        /// <summary>
        /// Unbinds the identifier with addressed key
        /// </summary>
        UnbindIdAndKey = 1,
    }
}
