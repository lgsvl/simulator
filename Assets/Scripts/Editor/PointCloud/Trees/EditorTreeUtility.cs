/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    public static class EditorTreeUtility
    {
        public static void SaveMeshGenerationData(string filePath, MeshGenerationData data)
        {
            SaveMeshGenerationData(filePath, data.gridSize, data.cells);
        }

        public static void SaveMeshGenerationData(string filePath, int[] gridSize, Dictionary<int, Vector3> cells)
        {
            var stride = sizeof(int) + 3 * sizeof(float);
            var size = 3 * sizeof(int) + stride * cells.Count;

            using (var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Create, null, size))
            {
                using (var accessor = mmf.CreateViewAccessor(0, size))
                {
                    long pos = 0;

                    for (var i = 0; i < 3; ++i)
                    {
                        accessor.Write(pos, gridSize[i]);
                        pos += sizeof(int);
                    }

                    foreach (var cell in cells)
                    {
                        accessor.Write(pos, cell.Key);
                        pos += sizeof(int);
                        TreeUtility.MmvaWriteVector3(cell.Value, accessor, ref pos);
                    }
                }
            }
        }

        public static void LoadMeshGenerationData(string filePath, MeshGenerationData output)
        {
            output.Clear();

            var size = new FileInfo(filePath).Length;
            var stride = sizeof(int) + 3 * sizeof(float);
            var count = (size - 3 * sizeof(int)) / stride;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var mmf = MemoryMappedFile.CreateFromFile(fs, null, size, MemoryMappedFileAccess.Read, HandleInheritability.None, false))
                {
                    using (var accessor = mmf.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read))
                    {
                        long pos = 0;

                        for (var i = 0; i < 3; ++i)
                        {
                            accessor.Read(pos, out int tmp);
                            output.gridSize[i] = tmp;
                            pos += sizeof(int);
                        }

                        for (var i = 0; i < count; ++i)
                        {
                            accessor.Read(pos, out int key);
                            pos += sizeof(int);
                            var val = TreeUtility.MmvaReadVector3(accessor, ref pos);
                            output.cells.Add(key, val);
                        }
                    }
                }
            }
        }

        public static float GetStepAtTreeLevel(Bounds rootBounds, TreeImportSettings settings, int level)
        {
            var step = Mathf.Min(rootBounds.size.x, rootBounds.size.z) / settings.rootNodeSubdivision;
            for (var i = 0; i < level - 1; ++i)
                step *= 0.5f;

            return step;
        }

        public static void CalculateGridSize(Bounds bounds, float step, int[] outGridSize)
        {
            outGridSize[0] = Mathf.Max(1, Mathf.RoundToInt(bounds.size.x / step));
            outGridSize[1] = Mathf.Max(1, Mathf.RoundToInt(bounds.size.y / step));
            outGridSize[2] = Mathf.Max(1, Mathf.RoundToInt(bounds.size.z / step));
        }
    }
}