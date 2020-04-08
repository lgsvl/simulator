/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System;
    using System.IO.MemoryMappedFiles;
    using UnityEngine;

    /// <summary>
    /// Utility class used to manage node trees.
    /// </summary>
    public static class TreeUtility
    {
        /// <summary>
        /// Identifier used to mark root node of a tree.
        /// </summary>
        public const string RootNodeIdentifier = "r";
        
        /// <summary>
        /// Extension used for node files.
        /// </summary>
        public const string NodeFileExtension = ".pcnode";
        
        /// <summary>
        /// Extension used for index file.
        /// </summary>
        public const string IndexFileExtension = ".pcindex";
        
        /// <summary>
        /// Extension used for mesh file.
        /// </summary>
        public const string MeshFileExtension = ".pcmesh";

        /// <summary>
        /// Returns index of an octree child whose bounds contain given point.
        /// </summary>
        /// <param name="nodeBounds">Bounds of a parent node.</param>
        /// <param name="pointPosition">Position of the point in world space.</param>
        public static byte GetOctreeChildIndex(Bounds nodeBounds, Vector3 pointPosition)
        {
            var nodeCenter = nodeBounds.center;
            byte result = 0;
            
            if (pointPosition.x >= nodeCenter.x)
                result |= 1;
            
            if (pointPosition.y >= nodeCenter.y)
                result |= 1 << 1;
            
            if (pointPosition.z >= nodeCenter.z)
                result |= 1 << 2;

            return result;
        }
        
        /// <summary>
        /// Returns index of a quadtree child whose bounds contain given point.
        /// </summary>
        /// <param name="nodeBounds">Bounds of a parent node.</param>
        /// <param name="pointPosition">Position of the point in world space.</param>
        public static byte GetQuadtreeChildIndex(Bounds nodeBounds, Vector3 pointPosition)
        {
            var nodeCenter = nodeBounds.center;
            byte result = 0;
            
            if (pointPosition.x >= nodeCenter.x)
                result |= 1;
            
            if (pointPosition.z >= nodeCenter.z)
                result |= 1 << 1;

            return result;
        }

        /// <summary>
        /// Returns maximum size of an array of type with given stride.
        /// </summary>
        public static int CalculateMaxArraySize(int dataStride)
        {
            return 2000 * 1024 * 1024 / dataStride;
        }

        /// <summary>
        /// <para>For octree, returns an unscaled direction vector pointing from parent's center to child's center.</para>
        /// <para>This has to be multiplied by a quarter of parent's bounds to represent actual offset.</para> 
        /// </summary>
        /// <param name="childIndex">Index of the child.</param>
        public static Vector3 GetOctreeOffsetVector(byte childIndex)
        {
            if (childIndex >= 1 << 3)
                throw new ArgumentOutOfRangeException(nameof(childIndex), "Child index must be within range 0-7.");
            
            return new Vector3(
                (childIndex & 1) != 0 ? 1 : -1,
                (childIndex & (1 << 1)) != 0 ? 1 : -1,
                (childIndex & (1 << 2)) != 0 ? 1 : -1);
        }
        
        /// <summary>
        /// <para>For quadtree, returns an unscaled direction vector pointing from parent's center to child's center.</para>
        /// <para>This has to be multiplied by a quarter of parent's bounds to represent actual offset.</para> 
        /// </summary>
        /// <param name="childIndex">Index of the child.</param>
        public static Vector3 GetQuadtreeOffsetVector(byte childIndex)
        {
            if (childIndex >= 1 << 2)
                throw new ArgumentOutOfRangeException(nameof(childIndex), "Child index must be within range 0-3.");
            
            return new Vector3(
                (childIndex & 1) != 0 ? 1 : -1,
                0f,
                (childIndex & (1 << 1)) != 0 ? 1 : -1);
        }

        /// <summary>
        /// Rounds given Vector3 so that each of its components is a multiply of given step value.
        /// </summary>
        public static Vector3 RoundToStep(Vector3 vec, float step)
        {
            return new Vector3(Mathf.RoundToInt(vec.x / step) * step,
                Mathf.RoundToInt(vec.y / step) * step,
                Mathf.RoundToInt(vec.z / step) * step);
        }
        
        /// <summary>
        /// Floors given Vector3 so that each of its components is a multiply of given step value.
        /// </summary>
        public static Vector3 FloorToStep(Vector3 vec, float step)
        {
            return new Vector3(Mathf.FloorToInt(vec.x / step) * step,
                Mathf.FloorToInt(vec.y / step) * step,
                Mathf.FloorToInt(vec.z / step) * step);
        }
        
        /// <summary>
        /// Ceils given Vector3 so that each of its components is a multiply of given step value.
        /// </summary>
        public static Vector3 CeilToStep(Vector3 vec, float step)
        {
            return new Vector3(Mathf.CeilToInt(vec.x / step) * step,
                Mathf.CeilToInt(vec.y / step) * step,
                Mathf.CeilToInt(vec.z / step) * step);
        }

        /// <summary>
        /// Aligns given bounds to always tightly encompass its voxelized space.
        /// </summary>
        /// <param name="bounds">Original bounds.</param>
        /// <param name="origin">Origin of the space in which voxelization is performed.</param>
        /// <param name="voxelSize">Edge length of a voxel.</param>
        /// <returns>Aligned bounds.</returns>
        public static Bounds GetRoundedAlignedBounds(Bounds bounds, Vector3 origin, float voxelSize)
        {
            var min = RoundToStep(bounds.min - origin, voxelSize) + origin;
            var max = RoundToStep(bounds.max - origin, voxelSize) + origin;
            
            return new Bounds(min + 0.5f * (max - min), max - min);
        }

        public static int Flatten(Vector3Int vector, int[] gridSize)
        {
            return Flatten(vector.x, vector.y, vector.z, gridSize);
        }

        public static int Flatten(int x, int y, int z, int[] gridSize)
        {
            return x + y * gridSize[0] + z * gridSize[0] * gridSize[1];
        }

        public static Vector3Int Unflatten(int value, int[] gridSize)
        {
            var squared = gridSize[0] * gridSize[1];
            var z = value / squared;
            var remaining = value - z * squared;
            var y = remaining / gridSize[0];
            var x = remaining % gridSize[0];
            
            return new Vector3Int(x, y, z);
        }

        /// <summary>
        /// <para>Writes Vector3 struct through MemoryMappedViewAccessor at given position.</para>
        /// <para>Position is updated to accomodate size of Vector3.</para>
        /// </summary>
        public static void MmvaWriteVector3(Vector3 vector, MemoryMappedViewAccessor accessor, ref long position)
        {
            for (var i = 0; i < 3; ++i)
            {
                accessor.Write(position, vector[i]);
                position += sizeof(float);
            }
        }

        /// <summary>
        /// <para>Reads Vector3 struct through MemoryMappedViewAccessor at given position.</para>
        /// <para>Position is updated to accomodate size of Vector3.</para>
        /// </summary>
        public static Vector3 MmvaReadVector3(MemoryMappedViewAccessor accessor, ref long position)
        {
            var result = new Vector3();

            for (var i = 0; i < 3; ++i)
            {
                accessor.Read(position, out float tmp);
                result[i] = tmp;
                position += sizeof(float);
            }

            return result;
        }
    }
}