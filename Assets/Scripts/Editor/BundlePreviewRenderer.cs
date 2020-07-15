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
    using System.Threading.Tasks;
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

        public static async Task RenderScenePreview(Transform origin, PreviewTextures textures)
        {
            await ReinitializeRenderPipeline();

            var previewRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScenePreviewRoot.prefab");
            var previewRoot = Object.Instantiate(previewRootPrefab, origin);
            var camera = previewRoot.GetComponentInChildren<Camera>();

            // This will trigger HDCamera.Update, which must be done before calling HDCamera.GetOrCreate
            // Otherwise m_AdditionalCameraData will not be set and HDCamera will be discarded after first frame
            camera.Render();
            await Task.Yield();

            var hdSettings = camera.GetComponent<HDAdditionalCameraData>();
            hdSettings.hasPersistentHistory = true;
            var hd = HDCamera.GetOrCreate(camera);
            var volume = previewRoot.GetComponentInChildren<Volume>();

            var timeOfDayLights = Object.FindObjectsOfType<TimeOfDayLight>();
            var timeOfDayBuildings = Object.FindObjectsOfType<TimeOfDayBuilding>();

            foreach (var light in timeOfDayLights)
                light.Init(TimeOfDayStateTypes.Day);

            foreach (var building in timeOfDayBuildings)
                building.Init(TimeOfDayStateTypes.Day);

            await Render(hd, textures, volume);

            Object.DestroyImmediate(previewRoot);

            await Task.Yield();
        }

        public static async Task RenderVehiclePreview(string vehicleAssetFile, PreviewTextures textures)
        {
            await ReinitializeRenderPipeline();

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
            await Task.Yield();

            var hdSettings = camera.GetComponent<HDAdditionalCameraData>();
            hdSettings.hasPersistentHistory = true;
            var hd = HDCamera.GetOrCreate(camera);

            await Render(hd, textures, volume);

            Object.DestroyImmediate(vehicle);

            await Task.Yield();
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

        private static async Task Render(HDCamera hd, PreviewTextures textures, Volume volume)
        {
            var camera = hd.camera;
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            hdrp?.RequestSkyEnvironmentUpdate();

            for (var i = 0; i < 3; ++i)
            {
                var res = new Vector2Int(textures[i].width, textures[i].height);
                var rt = RenderTexture.GetTemporary(res.x, res.y, 24);
                camera.targetTexture = rt;

                if (i == 0)
                {
                    // Physically based sky renderer builds up bounces over multiple frames, modifying indirect light
                    // Render some frames in advance here so that all lighting is fully calculated for previews
                    const int maxCalls = 20;
                    var calls = 0;

                    do
                    {
                        camera.Render();
                        rt.IncrementUpdateCount();
                        EditorApplication.QueuePlayerLoopUpdate();
                        await Task.Yield();
                    } while (calls++ < maxCalls && !SkyDone(volume, hd));

                    if (!SkyDone(volume, hd))
                    {
                        LogSkyData(volume, hd);
                        Debug.LogError($"Preview rendering failed - sky not initialized");
                    }
                }

                camera.Render();
                rt.IncrementUpdateCount();
                RenderTexture.active = rt;
                textures[i].ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0);
                camera.targetTexture = null;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                await Task.Yield();
            }
        }

        private static async Task ReinitializeRenderPipeline()
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

            const float maxTime = 10f;
            var timeElapsed = 0f;
            var startTime = Time.realtimeSinceStartup;

            while (timeElapsed < maxTime && !(bool) pipelineReadyField.GetValue(hdrp))
            {
                timeElapsed = Time.realtimeSinceStartup - startTime;
                await Task.Yield();
            }

            if (!(bool) pipelineReadyField.GetValue(hdrp))
                Debug.LogError("Failed to reinitialize HDRP");
        }
    }
}