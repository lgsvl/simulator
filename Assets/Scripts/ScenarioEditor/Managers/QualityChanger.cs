/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    /// <summary>
    /// Manager which changes the graphics quality settings for the VSE
    /// </summary>
    public class QualityChanger : MonoBehaviour
    {
        /// <summary>
        /// How many times maximum distance of the lod and shadows will be multiplied in the VSE
        /// </summary>
        private const float QualityDistanceMultiplier = 2.0f;

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Rendering volume for the scenario maps
        /// </summary>
        [SerializeField]
        private Volume volume;
        
        /// <summary>
        /// Default settings of the HDRP
        /// </summary>
        [SerializeField]
        private HDRenderPipelineAsset defaultHdrpSettings;
        
        /// <summary>
        /// VSE settings of the HDRP
        /// </summary>
        [SerializeField]
        private HDRenderPipelineAsset vseHdrpSettings;
#pragma warning restore 0649

        /// <summary>
        /// Is initialized
        /// </summary>
        private bool initialized;
        
        /// <summary>
        /// Unity Start method
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            Deinitialize(true);
        }

        /// <summary>
        /// Unity OnApplicationQuit method
        /// </summary>
        private void OnApplicationQuit()
        {
            Deinitialize(false);
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        private void Initialize()
        {
            if (initialized)
                return;
            
            //Increase shadows distance
            var shadows =
                volume.profile.components.Find(component => component is HDShadowSettings) as HDShadowSettings;
            if (shadows != null)
                shadows.maxShadowDistance.value *= QualityDistanceMultiplier;

            QualitySettings.renderPipeline = vseHdrpSettings;
            ReinitializeRenderPipeline();
            
            initialized = true;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        /// <param name="reinitializeRenderPipeline">Should reinitialize render pipeline after changes</param>
        private void Deinitialize(bool reinitializeRenderPipeline)
        {
            if (!initialized)
                return;
            
            //Revert shadows distance
            var shadows =
                volume.profile.components.Find(component => component is HDShadowSettings) as HDShadowSettings;
            if (shadows != null)
                shadows.maxShadowDistance.value /= QualityDistanceMultiplier;
            
            QualitySettings.renderPipeline = defaultHdrpSettings;
            if (reinitializeRenderPipeline)
                ReinitializeRenderPipeline();
            
            initialized = false;
        }
        
        /// <summary>
        /// Reinitializes the HDRP after making changes in it
        /// </summary>
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

            var pipelineReadyField = typeof(HDRenderPipeline).GetField("m_ResourcesInitialized", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pipelineReadyField == null)
            {
                Debug.LogError($"No ready flag in {nameof(HDRenderPipeline)}. Did you update HDRP?");
                return;
            }

            if (!(bool) pipelineReadyField.GetValue(hdrp))
                Debug.LogError("Failed to reinitialize HDRP");
        }
    }
}