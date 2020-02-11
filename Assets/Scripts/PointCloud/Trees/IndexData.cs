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

    /// <summary>
    /// Class representing index file of a node tree.
    /// </summary>
    [Serializable]
    public class IndexData
    {
        /// <summary>
        /// Type of the tree that this index is describing.
        /// </summary>
        public TreeType TreeType;

        /// <summary>
        /// List of all nodes' meta data.
        /// </summary>
        public NodeMetaData[] Data;

        public IndexData(TreeType treeType, List<NodeRecord> records)
        {
            TreeType = treeType;
            Data = new NodeMetaData[records.Count];

            for (var i = 0; i < records.Count; ++i)
            {
                var record = records[i];
                Data[i] = new NodeMetaData
                {
                    Identifier = record.Identifier,
                    PointCount = record.PointCount,
                    BoundsCenter = record.Bounds.center,
                    BoundsSize = record.Bounds.size
                };
            }
        }

        public IndexData(TreeType treeType, NodeMetaData[] data)
        {
            TreeType = treeType;
            Data = data;
        }

        public void SaveToFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            var size = 2 * sizeof(int);
            foreach (var item in Data) 
                size += item.GetByteSize();

            using (var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Create, "index", size))
            {
                using (var accessor = mmf.CreateViewAccessor(0, size))
                {
                    accessor.Write(0, (int) TreeType);
                    accessor.Write(sizeof(int), Data.Length);
                    long pos = 2 * sizeof(int);
                    
                    for (var i = 0; i < Data.Length; ++i) 
                        Data[i].Write(accessor, ref pos);
                }
            }
        }

        public static IndexData ReadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Index file under {filePath} not found.");

            var size = new FileInfo(filePath).Length;

            int treeType;
            NodeMetaData[] data;

            using (var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open))
            {
                using (var accessor = mmf.CreateViewAccessor(0, size))
                {
                    accessor.Read(0, out treeType);
                    accessor.Read(sizeof(int), out int itemCount);
                    
                    data = new NodeMetaData[itemCount];
                    long pos = sizeof(int) * 2;
                    
                    for (var i = 0; i < itemCount; ++i)
                        data[i] = NodeMetaData.Read(accessor, ref pos);
                }
            }

            return new IndexData((TreeType) treeType, data);
        }
    }
}