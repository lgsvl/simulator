/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Components
{
    using System;
    using Utilities;
    using UnityEngine;

    public class RainCollider : MonoBehaviour
    {
        public enum Resolution
        {
            Res32 = 32,
            Res64 = 64,
            Res128 = 128,
            Res256 = 256,
            Res512 = 512,
            Res1024 = 1024
        }

        /// <summary>
        /// Resolution along the longest AABB axis.
        /// </summary>
        [Tooltip("Resolution along the longest AABB axis.")]
        public Resolution resolution = Resolution.Res32;

        /// <summary>
        /// Maximum allowed size (in megabytes) of 3D texture used by this collider.
        /// </summary>
        [Tooltip("Maximum allowed size (in megabytes) of 3D texture used by this collider.")]
        public int memoryLimit = 64;

        [NonSerialized]
        public Vector3Int textureSize;

        public SignedDistanceFieldGenerator.SignedDistanceFieldData Data { get; private set; }

        public bool IsReady { get; private set; }

        private void Start()
        {
            SignedDistanceFieldGenerator.CalculateSize(gameObject, (int) resolution, out textureSize, out var bounds, out var step);
            var size = textureSize.x * textureSize.y * textureSize.z;
            var limit = memoryLimit * 1024 * 1024;

            if (size > limit)
            {
                IsReady = false;
                return;
            }

            var cs = RuntimeSettings.Instance.SignedDistanceFieldShader;
            Data = SignedDistanceFieldGenerator.Generate(gameObject, cs, textureSize, bounds, step);

            IsReady = true;
        }

        public void RecalculateSize()
        {
            SignedDistanceFieldGenerator.CalculateSize(gameObject, (int) resolution, out textureSize, out _, out _);
        }

        private void OnDestroy()
        {
            Data.texture.Release();
        }
    }
}