/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Shared.Messages
{
    /// <summary>
    /// Command types used in NPCManager
    /// </summary>
    public enum NPCManagerCommandType
    {
        /// <summary>
        /// Spawn new NPC in the manager
        /// </summary>
        SpawnNPC = 0,
        
        /// <summary>
        /// Despawn NPC in the manager
        /// </summary>
        DespawnNPC = 1,
    }
}