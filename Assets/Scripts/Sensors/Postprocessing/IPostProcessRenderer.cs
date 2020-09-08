/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    /// <summary>
    /// Interface used to call <see cref="PostProcessPass{TData}"/> with no generic type constraints.
    /// </summary>
    public interface IPostProcessRenderer
    {
        /// <summary>
        /// <para>Render this pass with given <see cref="data"/>.</para>
        /// <para>Data type must match generic type argument in <see cref="PostProcessPass{TData}"/> implementing this interface.</para>
        /// </summary>
        /// <param name="cmd">Buffer used to queue commands.</param>
        /// <param name="hdCamera">HD camera used by the sensor.</param>
        /// <param name="sensor">Sensor that should have postprocessing effects rendered.</param>
        /// <param name="sensorColorBuffer">Color buffer of the sensor's render target.</param>
        /// <param name="data">Data container for postprocessing parameters.</param>
        void Render(CommandBuffer cmd, HDCamera hdCamera, CameraSensorBase sensor, RTHandle sensorColorBuffer, PostProcessData data);
    }
}