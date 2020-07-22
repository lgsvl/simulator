/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

/// <summary>
/// Agent types available in the Simulator
/// </summary>
public enum AgentType
{
    /// <summary>
    /// Undefined agent type
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Ego agent controlled by the user or ad stack
    /// </summary>
    Ego = 1,
    
    /// <summary>
    /// NPC agent - vehicles controlled by the Simulator AI
    /// </summary>
    Npc = 2,
    
    /// <summary>
    /// Pedestrian agents - agents controlled by the Simulator AI that can't use the road lanes
    /// </summary>
    Pedestrian = 3,
}
