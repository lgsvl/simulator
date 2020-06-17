/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using Unity.EditorCoroutines.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;
    using Object = UnityEngine.Object;

    public static class BundlePreviewRenderer
    {
        public class PreviewTextures
        {
            public readonly Texture2D large = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            public readonly Texture2D medium = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            public readonly Texture2D small = new Texture2D(854, 480, TextureFormat.RGB24, false);

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

        public static IEnumerator RenderScenePreview(Transform origin, PreviewTextures textures)
        {
            var previewRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScenePreviewRoot.prefab");
            var previewRoot = Object.Instantiate(previewRootPrefab, origin);
            var camera = previewRoot.GetComponentInChildren<Camera>();

            // This will trigger HDCamera.Update, which must be done before calling HDCamera.GetOrCreate
            // Otherwise m_AdditionalCameraData will not be set and HDCamera will be discarded after first frame
            camera.Render();
            yield return null;

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

            yield return EditorCoroutineUtility.StartCoroutineOwnerless(Render(hd, textures, volume));

            Object.DestroyImmediate(previewRoot);

            yield return null;
        }

        public static IEnumerator RenderVehiclePreview(string vehicleAssetFile, PreviewTextures textures)
        {
            ReinitializeRenderPipeline();
            var previewRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VehiclePreviewRoot.prefab");
            var vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vehicleAssetFile);
            var previewRoot = Object.Instantiate(previewRootPrefab);
            var vehicle = Object.Instantiate(vehiclePrefab);
            var camera = previewRootPrefab.GetComponentInChildren<Camera>();

            // This will trigger HDCamera.Update, which must be done before calling HDCamera.GetOrCreate
            // Otherwise m_AdditionalCameraData will not be set and HDCamera will be discarded after first frame
            camera.Render();
            yield return null;

            var hdSettings = camera.GetComponent<HDAdditionalCameraData>();
            hdSettings.hasPersistentHistory = true;
            var hd = HDCamera.GetOrCreate(camera);
            var volume = previewRoot.GetComponentInChildren<Volume>();

            yield return EditorCoroutineUtility.StartCoroutineOwnerless(Render(hd, textures, volume));

            Object.DestroyImmediate(previewRoot);
            Object.DestroyImmediate(vehicle);

            yield return null;
        }

        private static bool SkyDone(Volume volume, HDCamera hd)
        {
            var sky = volume.profile.components.FirstOrDefault(x => x is PhysicallyBasedSky) as PhysicallyBasedSky;

            if (sky == null)
                return false;

            var skyUpdateContext = hd.GetType().GetProperty("visualSky", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(hd);
            var skyRenderer = skyUpdateContext?.GetType().GetProperty("skyRenderer", BindingFlags.Public | BindingFlags.Instance)?.GetValue(skyUpdateContext);
            var currentBounces = skyRenderer?.GetType().GetField("m_LastPrecomputedBounce", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(skyRenderer);
            if (currentBounces == null)
                return false;

            var targetBounces = sky.numberOfBounces.value;
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

            var skyRenderer = skyUpdateContext?.GetType().GetProperty("skyRenderer", BindingFlags.Public | BindingFlags.Instance)?.GetValue(skyUpdateContext);
            if (skyRenderer == null)
            {
                Debug.LogError("No sky renderer available");
                return;
            }

            var currentBounces = skyRenderer?.GetType().GetField("m_LastPrecomputedBounce", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(skyRenderer);
            if (currentBounces == null)
            {
                Debug.LogError("No bounce data available.");
                return;
            }

            var targetBounces = sky.numberOfBounces.value;
            var bouncesVal = currentBounces is int bounces ? bounces : 0;
            Debug.LogError($"Current bounces: {bouncesVal} expected: {targetBounces}");
        }

        private static IEnumerator Render(HDCamera hd, PreviewTextures textures, Volume volume)
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
                    var startTime = Time.realtimeSinceStartup;
                    var maxTime = 5f;
                    var timeElapsed = 0f;

                    while (timeElapsed < maxTime && !SkyDone(volume, hd))
                    {
                        camera.Render();
                        rt.IncrementUpdateCount();
                        EditorApplication.QueuePlayerLoopUpdate();
                        timeElapsed = Time.realtimeSinceStartup - startTime;
                        yield return null;
                    }

                    if (!SkyDone(volume, hd))
                    {
                        LogSkyData(volume, hd);
                        Debug.LogError("Preview rendering failed - sky not initialized");
                    }
                }

                camera.Render();
                rt.IncrementUpdateCount();
                RenderTexture.active = rt;
                textures[i].ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0);
                camera.targetTexture = null;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                yield return null;
            }
        }

        private static void ReinitializeRenderPipeline()
        {
            var asset = typeof(RenderPipelineManager).GetField("s_CurrentPipelineAsset", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            typeof(RenderPipelineManager).GetMethod("CleanupRenderPipeline", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
            typeof(RenderPipelineManager).GetMethod("PrepareRenderPipeline", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, new[] {asset});
        }
    }
}