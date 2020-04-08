/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Simulator.PointCloud.Trees;
    using Simulator.Utilities;
    using UnityEngine;

    public class MeshBuilder : ParallelProcessor
    {
        #region member_types

        private static readonly int[] LutClear =
        {
            0x0,
            0x9,
            0x3,
            0x6,
            0xC,
            0x8,
            0x1,
            0x2,
            0x4,
            0xD,
            0xB,
            0x7,
            0xE
        };

        private static readonly int[] LutMask =
        {
            0xF,
            0x96,
            0x3C,
            0x69,
            0xC3,
            0x87,
            0x1E,
            0x2D,
            0x4B,
            0xD2,
            0xB4,
            0x78,
            0xE1
        };

        private int[][] LutTriangles =
        {
            new[] {1, 0, 3, 1, 3, 2},
            new[] {1, 4, 7, 1, 7, 2},
            new[] {2, 5, 4, 2, 4, 3},
            new[] {3, 6, 5, 3, 5, 0},
            new[] {0, 7, 6, 0, 6, 1},
            new[] {1, 0, 7, 1, 7, 2},
            new[] {2, 1, 4, 2, 4, 3},
            new[] {3, 2, 5, 3, 5, 0},
            new[] {0, 3, 6, 0, 6, 1},
            new[] {1, 4, 7, 1, 7, 6},
            new[] {2, 5, 4, 2, 4, 7},
            new[] {3, 6, 5, 3, 5, 4},
            new[] {0, 7, 6, 0, 6, 5}
        };

        private static readonly Vector3Int[] LutOffsets =
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, 1, 1),
            new Vector3Int(0, 1, 1)
        };

        #endregion

        private MeshGenerationData voxelData;

        private readonly Dictionary<Vector3Int, MeshGenerationData> NeighborData = new Dictionary<Vector3Int, MeshGenerationData>();

        private string dataPath;
        private string tmpDataPath;

        private TreeImportSettings settings;

        private TreeImportData importData;

        private Vector3?[] cube = new Vector3?[8];

        public int VoxelsDone { get; private set; }

        public MeshBuilder(string dataPath, TreeImportSettings settings, TreeImportData importData)
        {
            this.dataPath = dataPath;
            this.importData = importData;
            this.settings = settings;
            tmpDataPath = Path.Combine(dataPath, "tmp");

            voxelData = new MeshGenerationData();

            for (var i = 1; i < LutOffsets.Length; ++i)
                NeighborData.Add(LutOffsets[i], new MeshGenerationData());
        }

        private static bool TryGetNeighbourId(string baseId, Vector3Int offset, out string neighbourId)
        {
            var result = baseId.ToCharArray();
            for (var i = 0; i < 3; ++i)
            {
                if (offset[i] <= 0)
                    continue;

                var valid = false;

                for (var j = baseId.Length - 1; j > 0; --j)
                {
                    var index = result[j] - '0';
                    if ((index & (1 << i)) == 0)
                    {
                        index |= 1 << i;
                        result[j] = (char) (index + '0');
                        valid = true;
                        break;
                    }
                    else
                    {
                        index &= ~(1 << i);
                        result[j] = (char) (index + '0');
                    }
                }

                if (valid)
                    continue;

                neighbourId = string.Empty;
                return false;
            }

            neighbourId = new string(result);
            return true;
        }

        protected override void DoWorkInternal(NodeRecord record)
        {
            var mainFilePath = Path.Combine(tmpDataPath, $"{record.Identifier}.meshdata");

            EditorTreeUtility.LoadMeshGenerationData(mainFilePath, voxelData);
            var output = new MeshData();

            for (var i = 1; i < LutOffsets.Length; ++i)
            {
                if (TryGetNeighbourId(record.Identifier, LutOffsets[i], out var neighbourId))
                {
                    var edgePath = Path.Combine(tmpDataPath, $"{neighbourId}.meshedge");
                    if (!File.Exists(edgePath))
                    {
                        NeighborData[LutOffsets[i]].Clear();
                        continue;
                    }

                    EditorTreeUtility.LoadMeshGenerationData(edgePath, NeighborData[LutOffsets[i]]);
                }
                else
                    NeighborData[LutOffsets[i]].Clear();
            }

            var step = Mathf.Min(record.Bounds.size.x, record.Bounds.size.z) /
                       settings.rootNodeSubdivision;

            var alignedBounds = TreeUtility.GetRoundedAlignedBounds(record.Bounds, importData.Bounds.min, step);
            var roadBounds = importData.RoadBounds;
            var centerOffset = Vector3.one * (step * 0.5f);

            for (var x = 0; x < voxelData.gridSize[0]; x++)
            {
                for (var y = 0; y < voxelData.gridSize[1]; y++)
                {
                    for (var z = 0; z < voxelData.gridSize[2]; z++)
                    {
                        if (cancelFlag)
                            return;

                        var coord = new Vector3Int(x, y, z);

                        for (var i = 0; i < LutOffsets.Length; i++)
                        {
                            var pos = coord + LutOffsets[i];
                            var val = GetCellValue(pos);
                            if (val == null)
                                cube[i] = null;
                            else if (!roadBounds.Contains((Vector3) val))
                                cube[i] = null;
                            else
                            {
                                var rel = (Vector3) val - alignedBounds.min;
                                var value = rel - (Vector3) pos * step - centerOffset;
                                cube[i] = value;
                            }
                        }

                        ProcessCube(coord, alignedBounds.min, step, output);
                        VoxelsDone++;
                    }
                }
            }

            var filePath = Path.Combine(dataPath, record.Identifier + TreeUtility.MeshFileExtension);
            output.SaveToFile(filePath);
        }

        private Vector3? GetCellValue(Vector3Int pos)
        {
            var dx = pos.x - voxelData.gridSize[0];
            var dy = pos.y - voxelData.gridSize[1];
            var dz = pos.z - voxelData.gridSize[2];

            if (dx < 0 && dy < 0 && dz < 0)
            {
                var flat = TreeUtility.Flatten(pos, voxelData.gridSize);
                return voxelData.cells.TryGetValue(flat, out var val) ? val : (Vector3?) null;
            }

            var neighborOffset = new Vector3Int(Math.Max(dx + 1, 0), Math.Max(dy + 1, 0), Math.Max(dz + 1, 0));
            var neighborData = NeighborData[neighborOffset];
            if (neighborData.cells.Count == 0)
                return null;

            var nPos = (Vector3Int.one - neighborOffset) * pos;
            var nFlat = TreeUtility.Flatten(nPos, neighborData.gridSize);
            return neighborData.cells.TryGetValue(nFlat, out var nVal) ? nVal : (Vector3?) null;
        }

        private void ProcessCube(Vector3Int coord, Vector3 boundsMin, float step, MeshData output)
        {
            // int j, vert, idx;
            int flagIndex = 0;
            var offset = Vector3.one * (step * 0.5f);

            for (var i = 0; i < 8; i++)
            {
                if (cube[i] != null)
                    flagIndex |= 1 << i;
            }

            for (var i = LutMask.Length - 1; i >= 0; --i)
            {
                var clearedIndex = flagIndex & ~LutClear[i];
                if (clearedIndex == LutMask[i])
                {
                    var tris = LutTriangles[i];
                    var count = output.verts.Count;
                    for (var j = 0; j < tris.Length; ++j)
                    {
                        var gId = tris[j];
                        var localOffset = cube[gId] ?? Vector3.zero;
                        var vPos = boundsMin + offset + localOffset + (coord + (Vector3) LutOffsets[gId]) * step;

                        output.verts.Add(vPos);
                        output.indices.Add(count + j);
                    }

                    break;
                }
            }
        }
    }
}