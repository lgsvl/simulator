/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Identification
{
    /// <summary>
    /// Object identified by the unique key
    /// </summary>
    public interface IIdentifiedObject
    {
        /// <summary>
        /// Unique key that is used to bind ids to the same distributed objects
        /// </summary>
        string Key { get; }
    }
}
