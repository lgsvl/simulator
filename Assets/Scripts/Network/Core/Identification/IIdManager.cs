/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Identification
{
    /// <summary>
    /// The identifiers manager interface
    /// </summary>
    public interface IIdManager
    {
        /// <summary>
        /// Get a unique identifier within this manager, following natural numbers required, first identifier has to same for all instances
        /// </summary>
        /// <returns>Unique identifier within this manager</returns>
        int GetId();

        /// <summary>
        /// Return the used identifiers to the manager so it can be reused
        /// </summary>
        /// <param name="id">Identifier to be returned</param>
        void ReturnId(int id);
    }
}