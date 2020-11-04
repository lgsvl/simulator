/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Map.LineDetection
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class LaneLineOverrideData
    {
        public List<Vector3> leftLineWorldPositions;
        public List<Vector3> rightLineWorldPositions;
    }
}