/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Shared.Messages
{
    /// <summary>
    /// Command types used in PedestrianManager
    /// </summary>
    public enum PedestrianManagerCommandType
    {
        /// <summary>
        /// Spawn new pedestrian in the manager
        /// </summary>
        SpawnPedestrian = 0,
        
        /// <summary>
        /// Despawn pedestrian in the manager
        /// </summary>
        DespawnPedestrian = 1
    }
}