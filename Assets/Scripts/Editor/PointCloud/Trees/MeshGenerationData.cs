/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System.Collections.Generic;
    using UnityEngine;

    public class MeshGenerationData
    {
        public readonly int[] gridSize = new int[3];
        public readonly Dictionary<int, Vector3> cells = new Dictionary<int, Vector3>();

        public void Clear()
        {
            for (var i = 0; i < gridSize.Length; ++i)
                gridSize[i] = 0;
            
            cells.Clear();
        }
    }
}
