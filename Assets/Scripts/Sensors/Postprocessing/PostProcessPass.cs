/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    /// <summary>
    /// Base class for all sensor postprocessing passes.
    /// </summary>
    /// <typeparam name="TData">Type representing used data container.</typeparam>
    public abstract class PostProcessPass<TData> : CustomPass, IPostProcessRenderer where TData : PostProcessData
    {
        /// <summary>
        /// Property defining whether this pass will be executed or not.
        /// </summary>
        protected abstract bool IsActive { get; }

        protected SensorPostProcessSystem PostProcessSystem { get; private set; }

        protected sealed override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            base.Setup(renderContext, cmd);
            PostProcessSystem = SimulatorManager.Instance.Sensors.PostProcessSystem;
            DoSetup();
        }

        protected sealed override void Cleanup()
        {
            DoCleanup();
            PostProcessSystem = null;
            base.Cleanup();
        }

        /// <summary>
        /// <para>Called when pass is initialized, before any rendering happens.</para>
        /// <para>Any resources required by this pass should be allocated here.</para>
        /// </summary>
        protected abstract void DoSetup();

        /// <summary>
        /// <para>Called when pass is about to be disposed.</para>
        /// <para>Any resources allocated in <see cref="DoCleanup"/> should be released here.</para>
        /// </summary>
        protected abstract void DoCleanup();

        protected sealed override void Execute(CustomPassContext customPassContext)
        {
            if (PostProcessSystem == null)
                return;

            var ctx = new PostProcessPassContext(customPassContext);

            var sensor = ctx.hdCamera.camera.GetComponent<CameraSensorBase>();
            if (sensor == null || sensor.Postprocessing == null || sensor.Postprocessing.Count == 0)
                return;

            // Late postprocessing queue is always called directly, never executed through volume
            if (PostProcessSystem.IsLatePostprocess(sensor, typeof(TData)))
                return;

            if (!IsActive)
            {
                PostProcessSystem.Skip(ctx.cmd, ctx.cameraColorBuffer, sensor, true, false);
                return;
            }

            TData data = null;
            foreach (var sensorData in sensor.Postprocessing)
            {
                if (sensorData is TData matchingData)
                {
                    data = matchingData;
                    break;
                }
            }

            if (data == null)
                return;

            PostProcessSystem.GetRTHandles(ctx.cmd, ctx.cameraColorBuffer, sensor, true, false, out var source, out var target);

            Render(ctx, source, target, data);

            PostProcessSystem.RecycleSourceRT(source, ctx.cameraColorBuffer, true);
        }

        ///<inheritdoc/>
        public void Render(PostProcessPassContext ctx, CameraSensorBase sensor, PostProcessData data, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            if (PostProcessSystem == null)
                return;

            var lateQueue = PostProcessSystem.IsLatePostprocess(sensor, typeof(TData));

            if (!IsActive)
            {
                PostProcessSystem.Skip(ctx.cmd, ctx.cameraColorBuffer, sensor, true, lateQueue);
                return;
            }

            if (!(data is TData tData))
            {
                Debug.LogWarning($"Attempting to render postprocess with invalid data type (required {typeof(TData).Name}, got {data.GetType().Name})");
                PostProcessSystem.Skip(ctx.cmd, ctx.cameraColorBuffer, sensor, true, lateQueue);
                return;
            }

            PostProcessSystem.GetRTHandles(ctx.cmd, ctx.cameraColorBuffer, sensor, false, lateQueue, out var source, out var target, cubemapFace);

            Render(ctx, source, target, tData);

            PostProcessSystem.RecycleSourceRT(source, ctx.cameraColorBuffer, false);

            if (cubemapFace != CubemapFace.Unknown)
                PostProcessSystem.TryPerformFinalCubemapPass(ctx.cmd, sensor, target, ctx.cameraColorBuffer, cubemapFace);
        }

        /// <summary>
        /// <para>Called when this postprocessing pass is supposed to be rendered.</para>
        /// <para>All rendering code for this pass should be executed here.</para>
        /// </summary>
        /// <param name="ctx">Context used for this pass execution.</param>
        /// <param name="source"><see cref="RTHandle"/> that should be used as a source (can be sampled).</param>
        /// <param name="destination"><see cref="RTHandle"/> that should be used as a target (can't be sampled).</param>
        /// <param name="data">Data container for postprocessing parameters.</param>
        protected abstract void Render(PostProcessPassContext ctx, RTHandle source, RTHandle destination, TData data);
    }
}