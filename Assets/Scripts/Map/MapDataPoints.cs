/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Simulator.Map
{
    public class MapDataPoints : MapData
    {
        public bool DisplayHandles = false;

        public List<Vector3> mapLocalPositions = new List<Vector3>();
        [System.NonSerialized]
        public List<Vector3> mapWorldPositions = new List<Vector3>();

        public void RefreshWorldPositions()
        {
            mapWorldPositions = mapLocalPositions.Select(s => transform.TransformPoint(s)).ToList();
        }
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
