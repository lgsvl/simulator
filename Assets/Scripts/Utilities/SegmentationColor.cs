/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;

namespace Simulator.Utilities
{
    [Serializable]
    public class SegmentationColor
    {
        [TagSelector]
        public string Tag;
        public Color Color;
        public bool IsInstanceSegmenation { get; set; } = false;
    }
}
