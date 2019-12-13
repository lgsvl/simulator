namespace Simulator.PointCloud.Trees
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;
    using Utilities;

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
        /// Maximum amount of points that can be put in a single array.
        /// </summary>
        public static long MaxPointCountPerArray = 2000 * 1024 * 1024 / UnsafeUtility.SizeOf<PointCloudPoint>();

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
            ;
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
            ;
        }

        /// <summary>
        /// <para>Creates and returns binary formatter that can be used to deserialize tree data.</para>
        /// <para>Contains surrogate to serialize and deserialize Vector3.</para>
        /// </summary>
        public static BinaryFormatter GetBinaryFormatterWithVector3Surrogate()
        {
            var binaryFormatter = new BinaryFormatter();
            var surrogateSelector = new SurrogateSelector();
            var vector3SerializationSurrogate = new Vector3SerializationSurrogate();

            surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All),
                vector3SerializationSurrogate);

            binaryFormatter.SurrogateSelector = surrogateSelector;

            return binaryFormatter;
        }
    }
}