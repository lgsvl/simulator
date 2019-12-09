namespace Simulator.Editor.PointCloud
{
    using System;

    public enum PointElementType
    {
        Byte,
        Float,
        Double,
    }

    public enum PointElementName
    {
        X, Y, Z,
        R, G, B,
        I,
    }
    
    public struct PointElement
    {
        public PointElementType Type;
        public PointElementName Name;
        public int Offset;

        public static int GetSize(PointElementType type)
        {
            switch (type)
            {
                case PointElementType.Byte: return 1;
                case PointElementType.Float: return 4;
                case PointElementType.Double: return 8;
            }
            throw new Exception($"Unsupported point element type {type}");
        }
    }
}