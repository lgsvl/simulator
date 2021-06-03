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
    using UnityEngine;
    using Utilities;
    using Vert = MeshGenerationData.Vert;

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

        private readonly Dictionary<Vector3Int, MeshGenerationData> neighborData = new Dictionary<Vector3Int, MeshGenerationData>();

        private string dataPath;
        private string tmpDataPath;

        private TreeImportSettings settings;

        private TreeImportData importData;

        private Vector3?[] cube = new Vector3?[8];

        private Vector3Int[] cubeCoords = new Vector3Int[8];

        // private Vector3[] cubePos = new Vector3[8];
        private Vert[] cubeVerts = new Vert[8];
        private Vert[] triVerts = new Vert[3];
        private Vector3Int[] triCoords = new Vector3Int[3];

        public int VoxelsDone { get; private set; }

        public MeshBuilder(string dataPath, TreeImportSettings settings, TreeImportData importData)
        {
            this.dataPath = dataPath;
            this.importData = importData;
            this.settings = settings;
            tmpDataPath = Path.Combine(dataPath, "tmp");

            voxelData = new MeshGenerationData();

            for (var i = 1; i < LutOffsets.Length; ++i)
                neighborData.Add(LutOffsets[i], new MeshGenerationData());
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
            var intermediateMesh = new MeshData();

            for (var i = 1; i < LutOffsets.Length; ++i)
            {
                if (TryGetNeighbourId(record.Identifier, LutOffsets[i], out var neighbourId))
                {
                    var edgePath = Path.Combine(tmpDataPath, $"{neighbourId}.meshedge");
                    if (!File.Exists(edgePath))
                    {
                        neighborData[LutOffsets[i]].Clear();
                        continue;
                    }

                    EditorTreeUtility.LoadMeshGenerationData(edgePath, neighborData[LutOffsets[i]]);
                }
                else
                    neighborData[LutOffsets[i]].Clear();
            }

            for (var i = 0; i < settings.erosionPasses; ++i)
                ProcessErode(record);

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
                            if (!TryGetBuilderVert(pos, out var vert))
                                cube[i] = null;
                            else if (!roadBounds.Contains(vert.position))
                                cube[i] = null;
                            else
                            {
                                var rel = vert.position - alignedBounds.min;
                                var value = rel - (Vector3) pos * step - centerOffset;
                                cube[i] = value;
                            }
                        }

                        ProcessCube(coord, intermediateMesh);
                        VoxelsDone++;
                    }
                }
            }

            for (var i = 0; i < settings.erosionPasses; ++i)
                LevelPoints();

            var finalMesh = ProcessFinalMesh(intermediateMesh);

            var filePath = Path.Combine(dataPath, record.Identifier + TreeUtility.MeshFileExtension);
            finalMesh.SaveToFile(filePath);
        }

        private void ProcessErode(NodeRecord record)
        {
            var step = Mathf.Min(record.Bounds.size.x, record.Bounds.size.z) /
                       settings.rootNodeSubdivision;

            var roadBounds = importData.RoadBounds;

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
                            var cubeCoord = coord + LutOffsets[i];
                            if (!TryGetBuilderVert(cubeCoord, out var val))
                                cubeVerts[i] = null;
                            else if (!roadBounds.Contains(val.position))
                                cubeVerts[i] = null;
                            else
                            {
                                cubeVerts[i] = val;
                                cubeCoords[i] = cubeCoord;
                            }
                        }

                        ProcessCubeErode(step);
                    }
                }
            }
        }

        private void LevelPoints()
        {
            foreach (var vert in voxelData.builderVerts)
                LevelVert(vert.Value);

            foreach (var neighbor in neighborData)
            {
                foreach (var neighborVert in neighbor.Value.builderVerts)
                    LevelVert(neighborVert.Value);
            }
        }

        private void LevelVert(Vert vert)
        {
            if (!vert.needsLeveling)
                return;

            var sum = 0f;
            var count = 0;
            foreach (var link in vert.links)
            {
                if (TryGetBuilderVert(link, out var linkedVert))
                {
                    if (linkedVert.needsLeveling)
                        continue;

                    sum += linkedVert.position.y;
                    count++;
                }
            }

            if (count == 0)
                return;

            var pos = vert.position;
            pos.y = sum / count;
            vert.position = pos;
            vert.needsLeveling = false;
        }

        private MeshData ProcessFinalMesh(MeshData intermediateMesh)
        {
            var doRemove = settings.removeSmallSurfaces;
            var threshold = settings.smallSurfaceTriangleThreshold;
            if (threshold < 1)
                doRemove = false;

            var result = new MeshData();

            var indexReplacementDict = new Dictionary<int, int>();
            var vertDict = new Dictionary<int, Vert>();
            var count = 0;

            var tmpVerts = new List<Vert>();
            var stack = new Stack<Vert>();

            foreach (var vert in voxelData.builderVerts)
            {
                var i = count++;
                vert.Value.assignedIndex = i;

                var pos = vert.Value.position;
                if (pos.x == 0 || pos.z == 0)
                    vert.Value.chunkEdge = true;

                vertDict.Add(i, vert.Value);
                foreach (var index in vert.Value.indices)
                    indexReplacementDict[index] = vert.Value.assignedIndex;
            }

            foreach (var neighbor in neighborData)
            {
                foreach (var vert in neighbor.Value.builderVerts)
                {
                    var i = count++;
                    vert.Value.assignedIndex = i;
                    vert.Value.chunkEdge = true;
                    vertDict.Add(i, vert.Value);
                    foreach (var index in vert.Value.indices)
                        indexReplacementDict[index] = vert.Value.assignedIndex;
                }
            }

            if (doRemove)
            {
                var safety = 1024;
                while (safety-- > 0)
                {
                    Vert start = null;

                    foreach (var vert in vertDict)
                    {
                        if (vert.Value.connectedCount == 0)
                        {
                            start = vert.Value;
                            break;
                        }
                    }

                    if (start == null)
                        break;

                    tmpVerts.Clear();
                    stack.Clear();
                    stack.Push(start);
                    var chunkEdge = false;

                    while (stack.Count > 0)
                    {
                        var vert = stack.Pop();
                        tmpVerts.Add(vert);
                        vert.connectedCount = -1;
                        if (vert.chunkEdge)
                            chunkEdge = true;

                        foreach (var link in vert.links)
                        {
                            if (TryGetBuilderVert(link, out var linkedVert))
                            {
                                if (linkedVert.connectedCount == 0)
                                    stack.Push(linkedVert);
                            }
                        }
                    }

                    // Verts from neighboring chunks are not counted - always assume that triangles adjacent to chunk
                    // edges are above removal threshold.
                    var vertCount = tmpVerts.Count;
                    if (chunkEdge)
                        vertCount += threshold;

                    foreach (var vert in tmpVerts)
                        vert.connectedCount = vertCount;
                }

                if (safety == 0)
                    Debug.LogWarning("Failed to remove small meshes, emergency break.");
            }

            foreach (var index in intermediateMesh.indices)
            {
                var vert = vertDict[indexReplacementDict[index]];
                if (doRemove && vert.connectedCount < threshold)
                    continue;

                result.verts.Add(vert.position);
                result.indices.Add(index);
            }

            return result;
        }

        private bool TryGetBuilderVert(Vector3Int pos, out Vert vert)
        {
            vert = null;

            var dx = pos.x - voxelData.gridSize[0];
            var dy = pos.y - voxelData.gridSize[1];
            var dz = pos.z - voxelData.gridSize[2];

            if (dx < 0 && dy < 0 && dz < 0)
            {
                return voxelData.builderVerts.TryGetValue(pos, out vert);
            }

            var neighborOffset = new Vector3Int(Math.Max(dx + 1, 0), Math.Max(dy + 1, 0), Math.Max(dz + 1, 0));
            var lNeighborData = neighborData[neighborOffset];
            if (lNeighborData.builderVerts.Count == 0)
                return false;

            var nPos = (Vector3Int.one - neighborOffset) * pos;
            return lNeighborData.builderVerts.TryGetValue(nPos, out vert);
        }

        private void ClearCellValue(Vector3Int pos)
        {
            var dx = pos.x - voxelData.gridSize[0];
            var dy = pos.y - voxelData.gridSize[1];
            var dz = pos.z - voxelData.gridSize[2];

            if (dx < 0 && dy < 0 && dz < 0)
            {
                if (voxelData.builderVerts.ContainsKey(pos))
                    voxelData.builderVerts.Remove(pos);
                return;
            }

            var neighborOffset = new Vector3Int(Math.Max(dx + 1, 0), Math.Max(dy + 1, 0), Math.Max(dz + 1, 0));
            var lNeighborData = neighborData[neighborOffset];
            if (lNeighborData.builderVerts.Count == 0)
                return;

            var nPos = (Vector3Int.one - neighborOffset) * pos;
            if (lNeighborData.builderVerts.ContainsKey(nPos))
                lNeighborData.builderVerts.Remove(nPos);
        }

        private void ProcessCube(Vector3Int coord, MeshData output)
        {
            int flagIndex = 0;
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
                        var vCoord = coord + LutOffsets[gId];
                        TryGetBuilderVert(vCoord, out var vertData);
                        var vPos = vertData.position;
                        var index = count + j;

                        output.verts.Add(vPos);
                        output.indices.Add(index);
                        vertData.AddIndex(index);
                        triCoords[j % 3] = vCoord;
                        triVerts[j % 3] = vertData;
                        if (j % 3 == 2)
                        {
                            for (var k = 0; k < 3; ++k)
                            {
                                triVerts[k].AddLink(triCoords[Utility.LoopIndex(k + 1, 3)]);
                                triVerts[k].AddLink(triCoords[Utility.LoopIndex(k + 2, 3)]);
                            }
                        }
                    }

                    break;
                }
            }
        }

        private void ProcessCubeErode(float step)
        {
            int flagIndex = 0;

            for (var i = 0; i < 8; i++)
            {
                if (cubeVerts[i] != null)
                    flagIndex |= 1 << i;
            }

            for (var i = LutMask.Length - 1; i >= 0; --i)
            {
                var clearedIndex = flagIndex & ~LutClear[i];
                if (clearedIndex == LutMask[i])
                {
                    var btmMin = float.MaxValue;
                    var btmMax = float.MinValue;
                    var topMax = float.MinValue;

                    for (var j = 0; j < 4; ++j)
                    {
                        var btm = cubeVerts[j];
                        if (btm != null)
                        {
                            btmMin = Mathf.Min(btmMin, btm.position.y);
                            btmMax = Mathf.Max(btmMax, btm.position.y);
                        }

                        var top = cubeVerts[j + 4];
                        if (top != null)
                        {
                            topMax = Mathf.Max(topMax, top.position.y);
                        }
                    }

                    var diff = topMax - btmMin;
                    var angle = Mathf.Atan2(diff, step) * Mathf.Rad2Deg;
                    var botDiff = btmMax - btmMin;
                    var botAngle = Mathf.Atan2(botDiff, step) * Mathf.Rad2Deg;

                    if (i == 0 && botAngle > settings.erosionAngleThreshold)
                    {
                        for (var j = 0; j < 4; ++j)
                            cubeVerts[j].needsLeveling = true;
                    }
                    else if (angle > settings.erosionAngleThreshold)
                    {
                        for (var j = 4; j < 8; ++j)
                        {
                            if (cubeVerts[j] != null)
                            {
                                if (TryGetBuilderVert(cubeCoords[j] + Vector3Int.down, out var below))
                                {
                                    ClearCellValue(cubeCoords[j]);
                                    below.needsLeveling = true;
                                }
                                else
                                    cubeVerts[j].needsLeveling = true;
                            }
                        }
                    }

                    break;
                }
            }
        }
    }
}