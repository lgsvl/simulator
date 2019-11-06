/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Map
{
    public class MapDataPoints : MapData
    {
        public bool displayHandles = false;

        public List<Vector3> mapLocalPositions = new List<Vector3>();
        [System.NonSerialized]
        public List<Vector3> mapWorldPositions = new List<Vector3>();
    }

    public interface IMapType
    {
        string id
        {
            get;
            set;
        }
    }
}
