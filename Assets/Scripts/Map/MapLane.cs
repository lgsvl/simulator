/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Map
{
    public class MapLane : MapDataPoints, IMapType, ISpawnable
    {
        public string id
        {
            get;
            set;
        }

        public bool Spawnable { get; set; } = false;
        public bool DenySpawn = false; // to deny spawns in odd lanes on ramps etc.

    }

    public interface ISpawnable
    {
        bool Spawnable { get; set; }
    }
}
