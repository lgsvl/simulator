/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud
{
    using System;
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
        public const CustomPassInjectionPoint UnlitInjectionPoint = CustomPassInjectionPoint.AfterPostProcess;
        public const CustomPassInjectionPoint LitInjectionPoint = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal;

        private PointCloudRenderer[] renderers;

        private PointCloudRenderPass litPass;
        private PointCloudRenderPass unlitPass;

        private PointCloudResources resources;

        private static PointCloudManager activeInstance;

        public static PointCloudResources Resources
        {
            get { return activeInstance.resources ?? (activeInstance.resources = new PointCloudResources()); }
        }

        private static bool TryGetUnlitPass(CustomPassVolume volume, ref PointCloudRenderPass pass)
        {
            if (volume.injectionPoint != UnlitInjectionPoint || volume.customPasses.Count != 1 ||
                !(volume.customPasses[0] is PointCloudRenderPass p))
                return false;

            pass = p;
            pass.targetColorBuffer = CustomPass.TargetBuffer.Camera;
            pass.targetDepthBuffer = CustomPass.TargetBuffer.Camera;

            return true;
        }

        private static bool TryGetLitPass(CustomPassVolume volume, ref PointCloudRenderPass pass)
        {
            if (volume.injectionPoint != LitInjectionPoint || volume.customPasses.Count != 1 ||
                !(volume.customPasses[0] is PointCloudRenderPass p))
                return false;

            pass = p;
            // Don't bind camera color buffer when using lighting - it's going to use GBuffer instead anyway
            pass.targetColorBuffer = CustomPass.TargetBuffer.None;
            pass.targetDepthBuffer = CustomPass.TargetBuffer.Camera;
            
            return true;
        }
        
        public static void RenderLidar(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hdCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            if (activeInstance == null)
                return;
            
            foreach (var pointCloudRenderer in activeInstance.renderers)
                pointCloudRenderer.RenderLidar(cmd, hdCamera, colorBuffer, depthBuffer);
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        
        public static void RenderDepth(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hdCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            if (activeInstance == null)
                return;

            foreach (var pointCloudRenderer in activeInstance.renderers)
                pointCloudRenderer.RenderDepth(cmd, hdCamera, colorBuffer, depthBuffer);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public static void HandleRendererAdded(PointCloudRenderer renderer)
        {
            if (activeInstance == null)
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
            if (activeInstance == null)
                return;
            
            activeInstance.RefreshRenderers();
        }
        
        private void OnEnable()
        {
            if (activeInstance != null)
                throw new Exception($"Multiple instances of {nameof(PointCloudManager)} seem to be active - this is invalid.");
                
            activeInstance = this;

            InitializeCustomPassVolumes();
            RefreshRenderers();
        }

        private void OnDisable()
        {
            renderers = null;
            
            var customPassVolume = gameObject.GetComponent<CustomPassVolume>();
            if (customPassVolume != null && customPassVolume.customPasses.Count == 1 &&
                customPassVolume.customPasses[0] is PointCloudRenderPass pass)
            {
                pass.UpdateRenderers(null);
            }
            
            activeInstance.resources?.ReleaseAll();
            activeInstance.resources = null;
            
            activeInstance = null;
        }

        private void InitializeCustomPassVolumes()
        {
            litPass = null;
            unlitPass = null;
            
            var customPassVolumes = gameObject.GetComponents<CustomPassVolume>();
            if (customPassVolumes.Length == 2)
            {
                for (var i = 0; i < 2; ++i)
                {
                    if (TryGetUnlitPass(customPassVolumes[i], ref unlitPass))
                        continue;
                    TryGetLitPass(customPassVolumes[i], ref litPass);
                }
            }

            if (litPass == null || unlitPass == null)
            {
                foreach (var pass in customPassVolumes)
                    CoreUtils.Destroy(pass);

                var unlitVolume = gameObject.AddComponent<CustomPassVolume>();
                unlitVolume.injectionPoint = UnlitInjectionPoint;
                unlitVolume.AddPassOfType(typeof(PointCloudRenderPass));
                TryGetUnlitPass(unlitVolume, ref unlitPass);
                
                var litVolume = gameObject.AddComponent<CustomPassVolume>();
                litVolume.injectionPoint = LitInjectionPoint;
                litVolume.AddPassOfType(typeof(PointCloudRenderPass));
                TryGetLitPass(litVolume, ref litPass);
            }

            if (litPass == null || unlitPass == null)
                Debug.LogError("Unable to initialize custom pass volumes for point clouds.");
        }

        private void RefreshRenderers()
        {
            renderers = FindObjectsOfType<PointCloudRenderer>();
            
            litPass?.UpdateRenderers(renderers);
            unlitPass?.UpdateRenderers(renderers);
        }
    }
}