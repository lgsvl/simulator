/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Map.LineDetection
{
    using System.Collections.Generic;
    using UnityEngine;

    public class LineDataHolder : MonoBehaviour
    {
        // TODO: keep segments instead
        public List<SegmentedLine3D> segments;
    }
}