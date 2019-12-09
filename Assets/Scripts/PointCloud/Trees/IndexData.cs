namespace Simulator.PointCloud.Trees
{
    using System;
    using System.Collections.Generic;

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
        public List<NodeMetaData> Data;

        private IndexData()
        {
            Data = new List<NodeMetaData>();
        }

        public IndexData(TreeType treeType, IEnumerable<NodeRecord> records) : this()
        {
            TreeType = treeType;
            
            foreach (var record in records)
            {
                Data.Add(new NodeMetaData
                {
                    Identifier = record.Identifier,
                    PointCount = record.PointCount,
                    BoundsCenter = record.Bounds.center,
                    BoundsSize = record.Bounds.size
                });
            }
        }
    }
}