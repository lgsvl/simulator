/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Map
{
    public class MapLaneBase : MapDataPoints, IMapType
    {
        public string id
        {
            get;
            set;
        }

        [System.NonSerialized]
        public bool Spawnable = false;
        public bool DenySpawn = false; // to deny spawns in odd lanes on ramps etc.
    }
}
