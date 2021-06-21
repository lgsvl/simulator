/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Components
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.VFX;

    public class VFXRain : MonoBehaviour
    {
        private static class Properties
        {
            public static readonly int SDFTexture = Shader.PropertyToID("_SDF_Tex");
            public static readonly int SDFOffset = Shader.PropertyToID("_SDF_Offset");
            public static readonly int SDFScale = Shader.PropertyToID("_SDF_Scale");
        }

        private class EffectPool
        {
            private readonly VisualEffect prefab;
            private readonly Transform parentTransform;

            private readonly Stack<VisualEffect> stack = new Stack<VisualEffect>();
            private readonly Action<VisualEffect> onGet;
            private readonly Action<VisualEffect> onRelease;

            public EffectPool(VisualEffect prefab, Transform parentTransform, Action<VisualEffect> actionOnGet, Action<VisualEffect> actionOnRelease, int initialCount = 0)
            {
                this.prefab = prefab;
                this.parentTransform = parentTransform;
                onGet = actionOnGet;
                onRelease = actionOnRelease;

                for (var i = 0; i < initialCount; ++i)
                {
                    var element = stack.Count == 0 ? Instantiate(prefab, parentTransform) : stack.Pop();
                    stack.Push(element);
                    onGet?.Invoke(element);
                    onRelease?.Invoke(element);
                }
            }

            public VisualEffect Get()
            {
                var element = stack.Count == 0 ? Instantiate(prefab, parentTransform) : stack.Pop();
                onGet?.Invoke(element);
                return element;
            }

            public void Release(VisualEffect element)
            {
                onRelease?.Invoke(element);
                stack.Push(element);
            }
        }

        private static readonly int IntensityProperty = Shader.PropertyToID("_RainfallAmount");

        public VisualEffect prefab;

        public float chunkSize = 50f;
        public float cullDistance = 100f;
        public float maxParticleCount = 5000;

        private float currentIntensity;

        private readonly List<Transform> trackedEntities = new List<Transform>();
        private readonly Dictionary<Vector2Int, VisualEffect> activeChunks = new Dictionary<Vector2Int, VisualEffect>();
        private readonly Dictionary<VisualEffect, Texture> currentSDFs = new Dictionary<VisualEffect, Texture>();

        private EffectPool pool;

        private RainCollider[] colliders;

        private Texture3D defaultSDF;

        private readonly List<Vector2Int> toRelease = new List<Vector2Int>();
        private readonly List<Vector2Int> toSpawn = new List<Vector2Int>();
        private readonly Stack<VisualEffect> reusable = new Stack<VisualEffect>();

        private bool killswitch;

        private void Start()
        {
            pool = new EffectPool(
                prefab,
                transform,
                effect =>
                {
                    effect.gameObject.SetActive(true);
                    effect.SetFloat(IntensityProperty, currentIntensity);
                },
                effect => effect.gameObject.SetActive(false),
                9);

            colliders = FindObjectsOfType<RainCollider>();

            defaultSDF = new Texture3D(1, 1, 1, TextureFormat.ARGB32, false);
            defaultSDF.name = name;
            defaultSDF.wrapMode = TextureWrapMode.Clamp;
            defaultSDF.SetPixel(0, 0, 0, Color.white, 0);
            defaultSDF.Apply(false);
        }

        public void RegisterTrackedEntity(Transform entity)
        {
            if (trackedEntities.Contains(entity))
            {
                Debug.LogWarning($"Entity {entity.gameObject.name} is already tracked.");
                return;
            }

            trackedEntities.Add(entity);
        }

        public void UnregisterTrackedEntity(Transform entity)
        {
            if (!trackedEntities.Contains(entity))
            {
                Debug.LogWarning($"Entity {entity.gameObject.name} is not tracked.");
                return;
            }

            trackedEntities.Remove(entity);
        }

        public void SetIntensity(float intensity)
        {
            currentIntensity = Mathf.Lerp(0f, maxParticleCount, intensity);
            foreach (var chunk in activeChunks)
                chunk.Value.SetFloat(IntensityProperty, currentIntensity);
        }

        private void Update()
        {
            if (killswitch)
                return;

            if (activeChunks.Count > 64)
            {
                killswitch = true;
                Debug.LogWarning("Unexpectedly high amount of active rain chunks. Disabling rain.");

                foreach (var activeChunk in activeChunks)
                    pool.Release(activeChunk.Value);

                activeChunks.Clear();
                currentSDFs.Clear();
                return;
            }

            if (trackedEntities.Count == 0)
                return;

            if (Mathf.Approximately(currentIntensity, 0f))
            {
                foreach (var activeChunk in activeChunks)
                    pool.Release(activeChunk.Value);

                activeChunks.Clear();
                currentSDFs.Clear();
                return;
            }

            var chunkBoundaryOffsetVector = new Vector2(cullDistance, cullDistance);
            var sqrDist = cullDistance * cullDistance;

            foreach (var entity in trackedEntities)
            {
                var worldPos3d = entity.position;
                var worldPos = new Vector2(worldPos3d.x, worldPos3d.z);
                var min = GetChunkPosition(worldPos - chunkBoundaryOffsetVector);
                var max = GetChunkPosition(worldPos + chunkBoundaryOffsetVector);
                for (var x = min.x; x <= max.x; ++x)
                {
                    for (var y = min.y; y <= max.y; ++y)
                    {
                        var chunkCenter = new Vector2(x + 0.5f, y + 0.5f) * chunkSize;
                        if ((chunkCenter - worldPos).sqrMagnitude > sqrDist)
                            continue;

                        var pos = new Vector2Int(x, y);

                        if (!toSpawn.Contains(pos))
                            toSpawn.Add(pos);
                    }
                }
            }

            foreach (var activeChunk in activeChunks)
            {
                if (toSpawn.Contains(activeChunk.Key))
                    toSpawn.Remove(activeChunk.Key);
                else
                    toRelease.Add(activeChunk.Key);
            }

            foreach (var key in toRelease)
            {
                reusable.Push(activeChunks[key]);
                activeChunks.Remove(key);
            }

            foreach (var key in toSpawn)
            {
                var effect = reusable.Count > 0 ? reusable.Pop() : pool.Get();
                FitToChunk(effect, key);
                activeChunks[key] = effect;
            }

            while (reusable.Count > 0)
            {
                var item = reusable.Pop();
                currentSDFs.Remove(item);
                pool.Release(item);
            }

            UpdateRainCollision();

            toSpawn.Clear();
            toRelease.Clear();
        }

        private Vector2Int GetChunkPosition(Vector2 position)
        {
            return new Vector2Int((int) ((position.x - 0.5f * chunkSize) / chunkSize), (int) ((position.y - 0.5f * chunkSize) / chunkSize));
        }

        private void FitToChunk(VisualEffect effect, Vector2Int chunk)
        {
            effect.transform.localPosition = new Vector3(chunk.x + 0.5f, 0f, chunk.y + 0.5f) * chunkSize;
        }

        private void UpdateRainCollision()
        {
            // This will track first registered tracked entity as a source for rain collision.
            // Since only one SDF is enabled per node, we can't have multiple areas active at once
            RainCollider activeCollider = null;
            var bestDist = float.MaxValue;
            foreach (var rainCollider in colliders)
            {
                if (!rainCollider.IsReady)
                    continue;

                var dist = SqrDistance(rainCollider.Data.bounds, trackedEntities[0].position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    activeCollider = rainCollider;
                }
            }

            var textureToUse = activeCollider == null ? defaultSDF : (Texture) activeCollider.Data.texture;

            foreach (var chunk in activeChunks)
            {
                if (!currentSDFs.TryGetValue(chunk.Value, out var tex) || tex != textureToUse)
                {
                    currentSDFs[chunk.Value] = textureToUse;

                    var effect = chunk.Value;
                    effect.SetTexture(Properties.SDFTexture, textureToUse);

                    if (activeCollider != null)
                    {
                        var offset = activeCollider.Data.bounds.center - effect.transform.position;
                        effect.SetVector3(Properties.SDFOffset, offset);
                        effect.SetVector3(Properties.SDFScale, activeCollider.Data.bounds.size);
                    }
                }
            }
        }

        private float SqrDistance(Bounds rect, Vector3 p)
        {
            var dx = Max(rect.min.x - p.x, 0, p.x - rect.max.x);
            var dy = Max(rect.min.y - p.y, 0, p.y - rect.max.y);
            var dz = Max(rect.min.z - p.z, 0, p.z - rect.max.z);
            return dx * dx + dy * dy + dz * dz;
        }

        private float Max(float f0, float f1, float f2)
        {
            return Mathf.Max(Mathf.Max(f0, f1), f2);
        }

        private void OnDrawGizmosSelected()
        {
            var matrix = Gizmos.matrix;
            var color = Gizmos.color;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;

            foreach (var chunk in activeChunks)
                Gizmos.DrawWireCube(new Vector3(chunk.Key.x + 0.5f, -25f / chunkSize, chunk.Key.y + 0.5f) * chunkSize, Vector3.one * chunkSize);

            Gizmos.matrix = matrix;
            Gizmos.color = color;
        }
    }
}