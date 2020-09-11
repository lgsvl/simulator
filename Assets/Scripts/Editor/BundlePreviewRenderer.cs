/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Simulator.PointCloud.Trees;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;
    using UnityEngine.SceneManagement;
    using Utilities;
    using Object = UnityEngine.Object;

    public static class BundlePreviewRenderer
    {
        public class PreviewTextures
        {
            public Texture2D large;
            public Texture2D medium;
            public Texture2D small;

            public PreviewTextures()
            {
                large = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                medium = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                small = new Texture2D(854, 480, TextureFormat.RGB24, false);

                for (var i = 0; i < 3; ++i)
                    this[i].hideFlags = HideFlags.HideAndDontSave;
            }

            public void Release()
            {
                CoreUtils.Destroy(large);
                CoreUtils.Destroy(medium);
                CoreUtils.Destroy(small);
            }

            public Texture2D this[int i]
            {
                get
                {
                    switch (i)
                    {
                        case 0:
                            return large;
                        case 1:
                            return medium;
                        case 2:
                            return small;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(i));
                    }
                }
            }
        }

        public static void RenderScenePreview(Transform origin, PreviewTextures textures)
        {
            var pos = origin.position;
            var rot = origin.rotation;

            HDRPUtilities.ReinitializeRenderPipeline();

            var previewRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScenePreviewRoot.prefab");
            var previewRoot = Object.Instantiate(previewRootPrefab);
            previewRoot.transform.rotation = rot;
            previewRoot.transform.position = pos;
            var camera = previewRoot.GetComponentInChildren<Camera>();

            // This will trigger HDCamera.Update, which must be done before calling HDCamera.GetOrCreate
            // Otherwise m_AdditionalCameraData will not be set and HDCamera will be discarded after first frame
            camera.Render();

            var hdSettings = camera.GetComponent<HDAdditionalCameraData>();
            hdSettings.hasPersistentHistory = true;
            var hd = HDCamera.GetOrCreate(camera);
            var volume = previewRoot.GetComponentInChildren<Volume>();
            var pointCloudRenderers = Object.FindObjectsOfType<NodeTreeRenderer>();

            foreach (var pointCloudRenderer in pointCloudRenderers)
                pointCloudRenderer.UpdateImmediate(camera);

            Render(hd, textures, volume);

            Object.DestroyImmediate(previewRoot);
        }

        public static void RenderVehiclePreview(string vehicleAssetFile, PreviewTextures textures)
        {
            HDRPUtilities.ReinitializeRenderPipeline();

            var cameraObj = GameObject.Find("PreviewCamera");
            var camera = cameraObj == null ? null : cameraObj.GetComponent<Camera>();
            if (camera == null)
            {
                Debug.LogError("Camera for vehicle preview was not found. Preview won't be available.");
                return;
            }

            var volume = Object.FindObjectOfType<Volume>();
            if (volume == null)
            {
                Debug.LogError("Volume for vehicle preview was not found. Preview won't be available.");
                return;
            }

            var vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vehicleAssetFile);
            var vehicleParent = GameObject.Find("VehicleParent");
            var vehicle = vehicleParent != null
                ? Object.Instantiate(vehiclePrefab, vehicleParent.transform)
                : Object.Instantiate(vehiclePrefab);

            // This will trigger HDCamera.Update, which must be done before calling HDCamera.GetOrCreate
            // Otherwise m_AdditionalCameraData will not be set and HDCamera will be discarded after first frame
            camera.Render();

            var hdSettings = camera.GetComponent<HDAdditionalCameraData>();
            hdSettings.hasPersistentHistory = true;
            var hd = HDCamera.GetOrCreate(camera);

            Render(hd, textures, volume);

            Object.DestroyImmediate(vehicle);
        }

        private static bool SkyDone(Volume volume, HDCamera hd)
        {
            var pbrSky = volume.profile.components.FirstOrDefault(x => x is PhysicallyBasedSky) as PhysicallyBasedSky;

            if (pbrSky == null)
            {
                var hdriSky = volume.profile.components.FirstOrDefault(x => x is HDRISky) as HDRISky;
                return hdriSky != null;
            }

            var skyUpdateContext = hd.GetType().GetProperty("visualSky", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(hd);
            var skyRenderer = skyUpdateContext?.GetType().GetProperty("skyRenderer", BindingFlags.Public | BindingFlags.Instance)?.GetValue(skyUpdateContext);
            var currentBounces = skyRenderer?.GetType().GetField("m_LastPrecomputedBounce", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(skyRenderer);
            if (currentBounces == null)
                return false;

            var frameField = skyRenderer.GetType().BaseType?.GetField("m_LastFrameUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (frameField != null)
            {
                var prev = (int) frameField.GetValue(skyRenderer);
                frameField.SetValue(skyRenderer, prev - 1);
            }

            var targetBounces = pbrSky.numberOfBounces.value;
            var bouncesVal = currentBounces is int bounces ? bounces : 0;
            return bouncesVal >= targetBounces;
        }

        private static void LogSkyData(Volume volume, HDCamera hd)
        {
            PhysicallyBasedSky sky = null;

            foreach (var component in volume.profile.components)
            {
                if (component is PhysicallyBasedSky bps)
                {
                    sky = bps;
                    break;
                }
            }

            if (sky == null)
            {
                Debug.LogError("No sky component available.");
                return;
            }

            var skyUpdateContext = hd.GetType().GetProperty("visualSky", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(hd);
            if (skyUpdateContext == null)
            {
                Debug.LogError("No sky context available.");
                return;
            }

            var skyRenderer = skyUpdateContext.GetType().GetProperty("skyRenderer", BindingFlags.Public | BindingFlags.Instance)?.GetValue(skyUpdateContext);
            if (skyRenderer == null)
            {
                Debug.LogError("No sky renderer available");
                return;
            }

            var frameField = skyRenderer.GetType().BaseType?.GetField("m_LastFrameUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (frameField == null)
            {
                Debug.LogError("No frame count field available in sky renderer");
                return;
            }

            var currentBounces = skyRenderer.GetType().GetField("m_LastPrecomputedBounce", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(skyRenderer);
            if (currentBounces == null)
            {
                Debug.LogError("No bounce data available.");
                return;
            }

            var targetBounces = sky.numberOfBounces.value;
            var bouncesVal = currentBounces is int bounces ? bounces : 0;
            Debug.LogError($"Current bounces: {bouncesVal} expected: {targetBounces}");
        }

        private static void Render(HDCamera hd, PreviewTextures textures, Volume volume)
        {
            var camera = hd.camera;
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            hdrp?.RequestSkyEnvironmentUpdate();

            var largeRes = new Vector2Int(textures[0].width, textures[0].height);
            var largeRt = RenderTexture.GetTemporary(largeRes.x, largeRes.y, 24);
            camera.targetTexture = largeRt;

            // Physically based sky renderer builds up bounces over multiple frames, modifying indirect light
            // Render some frames in advance here so that all lighting is fully calculated for previews
            const int maxCalls = 20;
            var calls = 0;

            do
            {
                camera.Render();
                largeRt.IncrementUpdateCount();
            } while (calls++ < maxCalls && !SkyDone(volume, hd));

            if (!SkyDone(volume, hd))
            {
                LogSkyData(volume, hd);
                Debug.LogError($"Preview rendering failed - sky not initialized");
            }

            camera.Render();
            largeRt.IncrementUpdateCount();

            for (var i = 2; i >= 0; i--)
            {
                var res = new Vector2Int(textures[i].width, textures[i].height);
                var rt = i == 0 ? largeRt : RenderTexture.GetTemporary(res.x, res.y, 24);

                if (i != 0)
                {
                    Graphics.Blit(largeRt, rt);
                    rt.IncrementUpdateCount();
                }

                RenderTexture.active = rt;
                textures[i].ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}