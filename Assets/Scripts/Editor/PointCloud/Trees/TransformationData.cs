/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using UnityEngine;

    /// <summary>
    /// Class storing values necessary for point transformations.
    /// </summary>
    public class TransformationData
    {
        public Matrix4x4 TransformationMatrix;

        public readonly double OutputScaleX;
        public readonly double OutputScaleY;
        public readonly double OutputScaleZ;

        public readonly double OutputCenterX;
        public readonly double OutputCenterY;
        public readonly double OutputCenterZ;
        
        public readonly bool LasRGB8BitWorkaround = true;
        
        public TransformationData(PointCloudBounds bounds, TreeImportSettings settings)
        {
            TransformationMatrix = GetTransformationMatrix(settings);
            LasRGB8BitWorkaround = settings.lasRGB8BitWorkaround;
            
            if (settings.normalize)
            {
                OutputCenterX = -0.5 * (bounds.MaxX + bounds.MinX);
                OutputCenterY = -0.5 * (bounds.MaxY + bounds.MinY);
                OutputCenterZ = -0.5 * (bounds.MaxZ + bounds.MinZ);

                OutputScaleX = 2.0 / (bounds.MaxX - bounds.MinX);
                OutputScaleY = 2.0 / (bounds.MaxY - bounds.MinY);
                OutputScaleZ = 2.0 / (bounds.MaxZ - bounds.MinZ);
            }
            else if (settings.center)
            {
                OutputCenterX = -0.5 * (bounds.MaxX + bounds.MinX);
                OutputCenterY = -0.5 * (bounds.MaxY + bounds.MinY);
                OutputCenterZ = -0.5 * (bounds.MaxZ + bounds.MinZ);

                OutputScaleX = OutputScaleY = OutputScaleZ = 1.0;
            }
            else
            {
                OutputCenterX = OutputCenterY = OutputCenterZ = 0.0;
                OutputScaleX = OutputScaleY = OutputScaleZ = 1.0;
            }
        }

        private static Matrix4x4 GetTransformationMatrix(TreeImportSettings settings)
        {
            switch (settings.axes)
            {
                case PointCloudImportAxes.X_Right_Y_Up_Z_Forward:
                    return Matrix4x4.identity;
                case PointCloudImportAxes.X_Right_Z_Up_Y_Forward:
                    return new Matrix4x4(new Vector4(1, 0, 0), new Vector4(0, 0, 1), new Vector4(0, 1, 0),
                        Vector4.zero);
                default:
                    Debug.Assert(false);
                    return Matrix4x4.identity;
            }
        }
    }
}