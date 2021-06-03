/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System.Collections.Generic;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    public class MeshGenerationData
    {
        public class Vert
        {
            public Vector3 position;
            public List<Vector3Int> links = new List<Vector3Int>();
            public List<int> indices = new List<int>();
            public int assignedIndex;
            public int connectedCount = 0;
            public bool needsLeveling = false;
            public bool chunkEdge = false;

            public void AddLink(Vector3Int coord)
            {
                if (!links.Contains(coord))
                    links.Add(coord);
            }

            public void AddIndex(int index)
            {
                if (!indices.Contains(index))
                    indices.Add(index);
            }
        }

        public readonly int[] gridSize = new int[3];
        public readonly Dictionary<int, Vector3> cells = new Dictionary<int, Vector3>();
        public readonly Dictionary<Vector3Int, Vert> builderVerts = new Dictionary<Vector3Int, Vert>();

        public void Clear()
        {
            for (var i = 0; i < gridSize.Length; ++i)
                gridSize[i] = 0;

            cells.Clear();
            builderVerts.Clear();
        }

        public void UpdateVolatileData()
        {
            builderVerts.Clear();

            foreach (var cell in cells)
            {
                var coord = TreeUtility.Unflatten(cell.Key, gridSize);
                builderVerts.Add(coord, new Vert()
                {
                    position = cell.Value
                });
            }
        }
    }
}
