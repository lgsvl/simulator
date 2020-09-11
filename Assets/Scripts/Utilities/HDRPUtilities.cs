/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities
{
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public static class HDRPUtilities
    {
        public static void ReinitializeRenderPipeline()
        {
            // NOTE: This is a workaround for Vulkan. Even if HDRP is reinitialized, lighting data and depth buffers
            //       on render targets (even ones created afterwards) will be corrupted. Reloading scene before
            //       forcefully reinitializing HDRP will refresh both lighting and depth data appropriately.
            //       This happens automatically for scene bundles, but is required for prefab ones.
            //       If this is not called for scene bundles, however, command line execution from async method will
            //       not create render pipeline at all when using Vulkan and crash with invalid memory access
            // Last tested on Unity 2019.3.15f1 and HDRP 7.3.1

            var assetField = typeof(RenderPipelineManager).GetField("s_CurrentPipelineAsset", BindingFlags.NonPublic | BindingFlags.Static);
            if (assetField == null)
            {
                Debug.LogError($"No asset field in {nameof(RenderPipelineManager)}. Did you update HDRP?");
                return;
            }

            var asset = assetField.GetValue(null);
            var cleanupMethod = typeof(RenderPipelineManager).GetMethod("CleanupRenderPipeline", BindingFlags.NonPublic | BindingFlags.Static);
            if (cleanupMethod == null)
            {
                Debug.LogError($"No cleanup method in {nameof(RenderPipelineManager)}. Did you update HDRP?");
                return;
            }

            cleanupMethod.Invoke(null, null);

            var prepareMethod = typeof(RenderPipelineManager).GetMethod("PrepareRenderPipeline", BindingFlags.NonPublic | BindingFlags.Static);
            if (prepareMethod == null)
            {
                Debug.LogError($"No prepare method in {nameof(RenderPipelineManager)}. Did you update HDRP?");
                return;
            }

            prepareMethod.Invoke(null, new[] {asset});

            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp == null)
            {
                var pipelineType = RenderPipelineManager.currentPipeline == null ? "null" : $"{RenderPipelineManager.currentPipeline.GetType().Name}";
                Debug.LogError($"HDRP not available for preview. (type: {pipelineType}))");
                return;
            }

#if UNITY_EDITOR
            var pipelineReadyField = typeof(HDRenderPipeline).GetField("m_ResourcesInitialized", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pipelineReadyField == null)
            {
                Debug.LogError($"No ready flag in {nameof(HDRenderPipeline)}. Did you update HDRP?");
                return;
            }

            if (!(bool) pipelineReadyField.GetValue(hdrp))
                Debug.LogError("Failed to reinitialize HDRP");
#endif
        }
    }
}