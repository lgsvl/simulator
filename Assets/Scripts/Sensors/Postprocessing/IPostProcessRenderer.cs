/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using UnityEngine;

    /// <summary>
    /// Interface used to call <see cref="PostProcessPass{TData}"/> with no generic type constraints.
    /// </summary>
    public interface IPostProcessRenderer
    {
        /// <summary>
        /// <para>Render this pass with given <see cref="data"/>.</para>
        /// <para>Data type must match generic type argument in <see cref="PostProcessPass{TData}"/> implementing this interface.</para>
        /// </summary>
        /// <param name="ctx">Context used for this pass execution.</param>
        /// <param name="sensor">Sensor that should have postprocessing effects rendered.</param>
        /// <param name="data">Data container for postprocessing parameters.</param>
        /// <param name="cubemapFace">Specifies target face if cubemap is used.</param>
        void Render(PostProcessPassContext ctx, CameraSensorBase sensor, PostProcessData data, CubemapFace cubemapFace = CubemapFace.Unknown);
    }
}