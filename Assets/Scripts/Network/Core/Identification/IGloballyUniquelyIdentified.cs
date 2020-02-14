/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Identification
{
    /// <summary>
    /// Object that has assigned globally unique identifier
    /// </summary>
    public interface IGloballyUniquelyIdentified
    {
        /// <summary>
        /// Globally unique identifier assigned to this object
        /// </summary>
        string GUID { get; }
    }
}
