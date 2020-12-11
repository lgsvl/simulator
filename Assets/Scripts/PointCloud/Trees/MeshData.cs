/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using UnityEngine;

    [Serializable]
    public class MeshData
    {
        public List<Vector3> verts = new List<Vector3>();
        public List<int> indices = new List<int>();

        public static MeshData LoadFromFile(string filePath, long offset, long size)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Mesh file under {filePath} not found.");

            var data = new MeshData();

            using (var mmf = MemoryMappedFile.CreateFromFile(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read), null, 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, false))
            {
                using (var accessor = mmf.CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read))
                {
                    accessor.Read(0, out int vertCount);
                    accessor.Read(sizeof(int), out int indicesCount);

                    data.verts.Capacity = vertCount;
                    data.indices.Capacity = indicesCount;

                    long pos = sizeof(int) * 2;

                    for (var i = 0; i < vertCount; ++i)
                        data.verts.Add(TreeUtility.MmvaReadVector3(accessor, ref pos));

                    for (var i = 0; i < indicesCount; ++i)
                    {
                        accessor.Read(pos, out int index);
                        data.indices.Add(index);
                        pos += sizeof(int);
                    }
                }
            }

            return data;
        }

        public void SaveToFile(string filePath)
        {
            var size = 2 * sizeof(int) + verts.Count * 3 * sizeof(float) + indices.Count * sizeof(int);

            try
            {
                using (var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Create, null, size))
                {
                    using (var accessor = mmf.CreateViewAccessor(0, size))
                    {
                        accessor.Write(0, verts.Count);
                        accessor.Write(sizeof(int), indices.Count);
                        long pos = 2 * sizeof(int);

                        for (var i = 0; i < verts.Count; ++i)
                            TreeUtility.MmvaWriteVector3(verts[i], accessor, ref pos);

                        for (var i = 0; i < indices.Count; ++i)
                        {
                            accessor.Write(pos, indices[i]);
                            pos += sizeof(int);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception was thrown when saving file {filePath}\n{e.Message}");
                throw;
            }
        }

        public List<Mesh> GenerateMeshes()
        {
            var meshes = new List<Mesh>();
            var vertsPerMesh = 30000;
            var meshCount = verts.Count / vertsPerMesh + 1;

            for (var i = 0; i < meshCount; ++i)
            {
                var tmpVerts = new List<Vector3>();
                var tmpIndices = new List<int>();

                for (var j = 0; j < vertsPerMesh; ++j)
                {
                    var index = vertsPerMesh * i + j;

                    if (index >= verts.Count)
                        break;

                    tmpVerts.Add(verts[index]);
                    tmpIndices.Add(j);
                }

                if (tmpVerts.Count == 0)
                    continue;

                var mesh = new Mesh();
                mesh.SetVertices(tmpVerts);
                mesh.SetTriangles(tmpIndices, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                meshes.Add(mesh);
            }

            return meshes;
        }
    }
}