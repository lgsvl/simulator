/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Components
{
    using Simulator.Sensors;
    using UnityEngine;

    public interface IPointCloudGenerator
    {
        public Vector4[] GeneratePoints(Vector3 position);

        public void ApplySettings(LidarTemplate template);

        public void Cleanup();
    }
}
