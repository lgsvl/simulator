namespace Simulator.Components
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.VFX;

    public class VFXRain : MonoBehaviour
    {
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

        private EffectPool pool;

        private readonly List<Vector2Int> toRelease = new List<Vector2Int>();
        private readonly List<Vector2Int> toSpawn = new List<Vector2Int>();
        private readonly Stack<VisualEffect> reusable = new Stack<VisualEffect>();

        private bool killswitch = false;

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
                return;
            }
            
            if (trackedEntities.Count == 0)
                return;

            if (Mathf.Approximately(currentIntensity, 0f))
            {
                foreach (var activeChunk in activeChunks)
                    pool.Release(activeChunk.Value);
                
                activeChunks.Clear();
                return;
            }

            var chunkBoundaryOffsetVector = new Vector2(cullDistance, cullDistance);
            var sqrDist = cullDistance * cullDistance;

            foreach (var entity in trackedEntities)
            {
                var worldPos3d = entity.localPosition;
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
                pool.Release(reusable.Pop());
            }

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