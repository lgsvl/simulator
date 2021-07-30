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
    using Simulator.Utilities;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;
    using UnityEngine.SceneManagement;
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

        public static void RenderScenePreview(Transform origin, PreviewTextures textures, bool forcePreview)
        {
            var pos = origin.position;
            var rot = origin.rotation;

            ReinitializeRenderPipeline();

            var hasReflectionProbes = Object.FindObjectOfType<ReflectionProbe>() != null;

            var volumes = Object.FindObjectsOfType<Volume>();
            Volume volume = null;
            IndirectLightingController indirectLightingController = null;

            foreach (var vol in volumes)
            {
                if (vol.isGlobal && volume == null)
                {
                    volume = vol;
                    continue;
                }

                var collider = vol.GetComponent<Collider>();
                if (collider.bounds.Contains(pos))
                {
                    volume = vol;
                    break;
                }
            }

            var previewRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScenePreviewRoot.prefab");
            var previewRoot = Object.Instantiate(previewRootPrefab, pos, rot);
            var camera = previewRoot.GetComponentInChildren<Camera>();

            if (forcePreview)
            {
                previewRoot.transform.SetPositionAndRotation(pos, Quaternion.Euler(new Vector3(0f, rot.eulerAngles.y, 0f)));
                camera.transform.SetParent(null);
                camera.transform.SetPositionAndRotation(pos, rot);
            }

            // This will trigger HDCamera.Update, which must be done before calling HDCamera.GetOrCreate
            // Otherwise m_AdditionalCameraData will not be set and HDCamera will be discarded after first frame
            camera.Render();

            var hdSettings = camera.GetComponent<HDAdditionalCameraData>();
            hdSettings.hasPersistentHistory = true;
            var hd = HDCamera.GetOrCreate(camera);

            if (volume == null)
                volume = previewRoot.GetComponentInChildren<Volume>();

            // CullingResults in first frame after loading scene does not contain aby data about reflection probes.
            // Light loop will use further options, which usually means skybox indirect reflections. This ignores
            // occlusion and will break interior lighting. Due to lack of other options, just disable indirect specular
            // lighting for preview rendering.
            indirectLightingController = volume.profile.components.FirstOrDefault(x => x is IndirectLightingController) as IndirectLightingController;
            var indirectControllerAdded = hasReflectionProbes && indirectLightingController == null;

            if (indirectControllerAdded)
                indirectLightingController = volume.profile.Add<IndirectLightingController>();

            var indirectMultiplier = indirectLightingController == null ? 0f : indirectLightingController.reflectionLightingMultiplier.value;

            if (hasReflectionProbes && indirectLightingController != null)
                indirectLightingController.reflectionLightingMultiplier.value = 0f;

            var pointCloudRenderers = Object.FindObjectsOfType<NodeTreeRenderer>();

            foreach (var pointCloudRenderer in pointCloudRenderers)
                pointCloudRenderer.UpdateImmediate(camera);

            Render(hd, textures, volume);

            if (hasReflectionProbes && indirectLightingController != null)
                indirectLightingController.reflectionLightingMultiplier.value = indirectMultiplier;

            if (indirectControllerAdded && indirectLightingController != null)
                volume.profile.Remove<IndirectLightingController>();

            Object.DestroyImmediate(previewRoot);
        }

        public static void RenderVehiclePreview(string vehicleAssetFile, PreviewTextures textures)
        {
            ReinitializeRenderPipeline();

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

            vehicle.transform.localRotation = Quaternion.identity;
            vehicle.transform.localPosition = Vector3.zero;

            // adjust camera distance based on hit distance
            RaycastHit hit;
            var start = cameraObj.transform.position;
            var end = vehicle.transform.position;
            var direction = (end - start);
            Ray cameraRay = new Ray(start, direction);
            if (Physics.Raycast(cameraRay, out hit, LayerMask.GetMask("Agent")))
            {
                cameraObj.transform.position = hit.point + ((cameraObj.transform.position - hit.point).normalized) * 3f;
            }

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
                if (hdriSky != null)
                    return true;
            }

            var skyUpdateContext = hd.GetType().GetProperty("visualSky", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(hd);
            var skyRenderer = skyUpdateContext?.GetType().GetProperty("skyRenderer", BindingFlags.Public | BindingFlags.Instance)?.GetValue(skyUpdateContext);
            var precomputedData = skyRenderer?.GetType().GetField("m_PrecomputedData", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(skyRenderer);
            var currentBounces = precomputedData?.GetType().GetField("m_LastPrecomputedBounce", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(precomputedData);
            if (currentBounces == null)
                return false;

            var baseFrameField = skyRenderer.GetType().BaseType?.GetField("m_LastFrameUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (baseFrameField != null)
            {
                var prev = (int) baseFrameField.GetValue(skyRenderer);
                baseFrameField.SetValue(skyRenderer, prev - 1);
            }

            var frameField = precomputedData.GetType().GetField("m_LastFrameComputation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (frameField != null)
            {
                var prev = (int) frameField.GetValue(precomputedData);
                frameField.SetValue(precomputedData, prev - 1);
            }

            var targetBounces = pbrSky == null ? 8 : pbrSky.numberOfBounces.value;
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
                Debug.LogError("No sky renderer available.");
                return;
            }

            var baseFrameField = skyRenderer.GetType().BaseType?.GetField("m_LastFrameUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (baseFrameField == null)
            {
                Debug.LogError("No frame count field available in sky renderer base class.");
                return;
            }

            var precomputedData = skyRenderer.GetType().GetField("m_PrecomputedData", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(skyRenderer);
            if (precomputedData == null)
            {
                Debug.LogError("No precomputed data field available in sky renderer.");
                return;
            }

            var frameField = precomputedData.GetType().GetField("m_LastFrameComputation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (frameField == null)
            {
                Debug.LogError("No frame count field available in precomputed data.");
                return;
            }

            var currentBounces = precomputedData.GetType().GetField("m_LastPrecomputedBounce", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(precomputedData);
            if (currentBounces == null)
            {
                Debug.LogError("No bounce data available in precomputed data.");
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

        private static void ReinitializeRenderPipeline()
        {
            // NOTE: This is a workaround for Vulkan. Even if HDRP is reinitialized, lighting data and depth buffers
            //       on render targets (even ones created afterwards) will be corrupted. Reloading scene before
            //       forcefully reinitializing HDRP will refresh both lighting and depth data appropriately.
            //       This happens automatically for scene bundles, but is required for prefab ones.
            //       If this is not called for scene bundles, however, command line execution from async method will
            //       not create render pipeline at all when using Vulkan and crash with invalid memory access
            // Last tested on Unity 2019.3.15f1 and HDRP 7.3.1

            const string loaderScenePath = "Assets/Scenes/LoaderScene.unity";
            var openScenePaths = new List<string>();
            var activeScenePath = SceneManager.GetActiveScene().path;

            for (var i = 0; i < EditorSceneManager.loadedSceneCount; ++i)
                openScenePaths.Add(SceneManager.GetSceneAt(i).path);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mainScenePath = string.IsNullOrEmpty(activeScenePath) ? loaderScenePath : activeScenePath;
            EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
            foreach (var scenePath in openScenePaths)
            {
                if (string.Equals(scenePath, activeScenePath) || string.IsNullOrEmpty(scenePath))
                    continue;

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            HDRPUtilities.ReinitializeRenderPipeline();
        }
    }
}