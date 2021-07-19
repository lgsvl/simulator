/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    /// <summary>
    /// Manager class responsible for initializing custom pass components and synchronizing their settings with active
    /// point cloud renderers.
    /// </summary>
    [ExecuteInEditMode]
    public class PointCloudManager : MonoBehaviour
    {
        public const CustomPassInjectionPoint UnlitInjectionPoint = CustomPassInjectionPoint.BeforeTransparent;

        private PointCloudRenderer[] renderers;

        private PointCloudRenderPass unlitPass;

        private PointCloudResources resources;

        private static PointCloudManager activeInstance;

        public static PointCloudResources Resources
        {
            get
            {
                var instance = activeInstance.resources ?? (activeInstance.resources = new PointCloudResources());
                instance.VerifyResolution();
                return instance;
            }
        }

        private void VerifyPassVolume()
        {
            var isPlayMode = Application.isPlaying;
            var isVse = Loader.IsInScenarioEditor;
            if (isPlayMode && !isVse)
                InitializeGlobalVolume();
            else
                InitializeLocalVolume();
        }

        private void InitializeLocalVolume()
        {
            // CustomPassManager is either marked for destruction or wasn't yet spawned - no need for cleanup
            unlitPass = null;

            var customPassVolumes = gameObject.GetComponents<CustomPassVolume>();
            if (customPassVolumes.Length == 1)
            {
                customPassVolumes[0].enabled = true;
                TryGetLocalUnlitPass(customPassVolumes[0], ref unlitPass);
            }

            if (unlitPass == null)
            {
                foreach (var pass in customPassVolumes)
                    CoreUtils.Destroy(pass);

                var unlitVolume = gameObject.AddComponent<CustomPassVolume>();
                unlitVolume.injectionPoint = UnlitInjectionPoint;
                unlitVolume.AddPassOfType(typeof(PointCloudRenderPass));
                TryGetLocalUnlitPass(unlitVolume, ref unlitPass);
            }

            if (unlitPass == null)
            {
                Debug.LogWarning("Unable to initialize custom pass volumes for point clouds.");
            }

            RefreshRenderers();
        }

        private void InitializeGlobalVolume()
        {
            // PointCloudManager must be present on scene to allow rendering in editor, but during simulation it's
            // initialized sooner than SimulatorManager because scene loads before simulation start.
            // Coroutine is used to wait until SimulatorManager instance is available, then attaches to global custom
            // pass volume.
            StartCoroutine(MigrateToGlobalVolumeCoroutine());
        }

        private IEnumerator MigrateToGlobalVolumeCoroutine()
        {
            while (!SimulatorManager.InstanceAvailable)
                yield return null;
            
            // Disable all local volumes, add passes to global ones
            var localVolumes = gameObject.GetComponents<CustomPassVolume>();
            if (localVolumes != null)
            {
                foreach (var localVolume in localVolumes) 
                    localVolume.enabled = false;
            }

            var customPassManager = SimulatorManager.Instance.CustomPassManager;
            unlitPass = customPassManager.AddPass<PointCloudRenderPass>(UnlitInjectionPoint, -200);

            RefreshRenderers();
        }

        private static bool TryGetLocalUnlitPass(CustomPassVolume volume, ref PointCloudRenderPass pass)
        {
            if (volume.injectionPoint != UnlitInjectionPoint || volume.customPasses.Count != 1 ||
                !(volume.customPasses[0] is PointCloudRenderPass p))
                return false;

            pass = p;
            pass.targetColorBuffer = CustomPass.TargetBuffer.Camera;
            pass.targetDepthBuffer = CustomPass.TargetBuffer.Camera;

            return true;
        }

        public static void RenderLidar(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hdCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            RenderLidar(context, cmd, hdCamera, colorBuffer, depthBuffer, CubemapFace.Unknown);
        }

        public static void RenderLidar(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hdCamera, RTHandle colorBuffer, RTHandle depthBuffer, CubemapFace cubemapFace)
        {
            if (activeInstance == null || activeInstance.renderers == null)
                return;
            
            foreach (var pointCloudRenderer in activeInstance.renderers)
                pointCloudRenderer.RenderLidar(cmd, hdCamera, colorBuffer, depthBuffer, cubemapFace);
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public static void RenderDepth(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hdCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            RenderDepth(context, cmd, hdCamera, colorBuffer, depthBuffer, CubemapFace.Unknown);
        }

        public static void RenderDepth(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hdCamera, RTHandle colorBuffer, RTHandle depthBuffer, CubemapFace cubemapFace)
        {
            if (activeInstance == null || activeInstance.renderers == null)
                return;

            foreach (var pointCloudRenderer in activeInstance.renderers)
                pointCloudRenderer.RenderDepth(cmd, hdCamera, colorBuffer, depthBuffer, cubemapFace);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public static void HandleRendererAdded(PointCloudRenderer renderer)
        {
            if (activeInstance == null || activeInstance.renderers == null)
                return;
            
            foreach (var pcr in activeInstance.renderers)
            {
                if (pcr == renderer)
                    return;
            }
            
            activeInstance.RefreshRenderers();
        }
        
        public static void HandleRendererRemoved(PointCloudRenderer renderer)
        {
            if (activeInstance == null || activeInstance.renderers == null)
                return;
            
            activeInstance.RefreshRenderers();
        }
        
        private void OnEnable()
        {
            if (activeInstance != null)
                throw new Exception($"Multiple instances of {nameof(PointCloudManager)} seem to be active - this is invalid.");
                
            activeInstance = this;

            VerifyPassVolume();
        }

        private void OnDisable()
        {
            renderers = null;
            unlitPass?.UpdateRenderers(null);
            
            activeInstance.resources?.ReleaseAll();
            activeInstance.resources = null;
            
            activeInstance = null;
        }

        private void RefreshRenderers()
        {
            renderers = FindObjectsOfType<PointCloudRenderer>();
            unlitPass?.UpdateRenderers(renderers);
        }
    }
}