/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    /// <summary>
    /// Class overseeing and managing all sensor postprocessing rendering.
    /// </summary>
    public class SensorPostProcessSystem
    {
        private const int LatePostprocessOrder = 1000;
        
        private readonly Dictionary<Type, CustomPass> postProcessingPasses = new Dictionary<Type, CustomPass>();
        private readonly Dictionary<Type, int> postProcessingOrders = new Dictionary<Type, int>();
        private readonly Dictionary<int, Stack<RTHandle>> rtHandlePools = new Dictionary<int, Stack<RTHandle>>();

        private readonly Dictionary<CameraSensorBase, int> preDistortionSensorSwaps = new Dictionary<CameraSensorBase, int>();
        private readonly Dictionary<CameraSensorBase, RTHandle> preDistortionLastTarget = new Dictionary<CameraSensorBase, RTHandle>();
        
        private readonly Dictionary<CameraSensorBase, int> postDistortionSensorSwaps = new Dictionary<CameraSensorBase, int>();
        private readonly Dictionary<CameraSensorBase, RTHandle> postDistortionLastTarget = new Dictionary<CameraSensorBase, RTHandle>();

        private int usedHandleCount;

        private static int GetHandleHashCode(RTHandle handle, bool autoSize, bool cube)
        {
            // HDRP uses auto-scaled RTHandles, but sensors declare exact resolution for each.
            // Make sure these two are not mixed.
            var hashCode = autoSize && !cube
                ? GetHandleHashCode(
                    handle.scaleFactor.x,
                    handle.scaleFactor.y,
                    (int) handle.rt.graphicsFormat,
                    (int) handle.rt.dimension,
                    handle.rt.useMipMap)
                : GetHandleHashCode(handle.referenceSize.x,
                    handle.referenceSize.y,
                    (int) handle.rt.graphicsFormat,
                    (int) TextureXR.dimension,
                    handle.rt.useMipMap);

            return hashCode;
        }

        private static int GetHandleHashCode(float scaleX, float scaleY, int format, int dimension, bool mipmap)
        {
            var hashCode = 17;

            unchecked
            {
                unsafe
                {
                    hashCode = hashCode * 23 + *(int*) &scaleX;
                    hashCode = hashCode * 23 + *(int*) &scaleY;
                }

                hashCode = hashCode * 23 + format;
                hashCode = hashCode * 23 + (mipmap ? 1 : 0);
                hashCode = hashCode * 23 + dimension;
                hashCode = hashCode * 23;
            }

            return hashCode;
        }

        private static int GetHandleHashCode(int sizeX, int sizeY, int format, int dimension, bool mipmap)
        {
            var hashCode = 17;

            unchecked
            {
                hashCode = hashCode * 23 + sizeX;
                hashCode = hashCode * 23 + sizeY;

                hashCode = hashCode * 23 + format;
                hashCode = hashCode * 23 + (mipmap ? 1 : 0);
                hashCode = hashCode * 23 + dimension;
                hashCode = hashCode * 23 + 1;
            }

            return hashCode;
        }

        private static void RenderPostProcess<T>(PostProcessPassContext ctx, CameraSensorBase sensor, CustomPass pass, T data, CubemapFace cubemapFace = CubemapFace.Unknown) where T : PostProcessData
        {
            var postProcessPass = pass as IPostProcessRenderer;
            postProcessPass?.Render(ctx, sensor, data, cubemapFace);
        }

        public void Initialize()
        {
            var cameraSensors = UnityEngine.Object.FindObjectsOfType<CameraSensorBase>();
            var usedDataTypes = new List<Type>();

            // Aggregate all used postprocess data types
            foreach (var cameraSensor in cameraSensors)
            {
                if (cameraSensor.Postprocessing == null)
                    continue;

                foreach (var postProcessData in cameraSensor.Postprocessing)
                {
                    var type = postProcessData.GetType();
                    if (!usedDataTypes.Contains(type))
                        usedDataTypes.Add(type);
                }
            }

            // Find all postprocess passes in assembly
            var postProcessBaseType = typeof(PostProcessPass<>);
            var assignableTypes = postProcessBaseType.Assembly.GetTypes()
                .Where(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == postProcessBaseType)
                .ToList();

            // Create 1 to 1 dictionary of data type to postprocess pass
            var dataToEffectDict = new Dictionary<Type, (Type effectType, int order)>();
            foreach (var effectType in assignableTypes)
            {
                if (effectType.BaseType is null)
                    continue;

                var genericArgs = effectType.BaseType.GetGenericArguments();
                if (genericArgs.Length != 1)
                {
                    Debug.LogWarning($"Unexpected generic argument type count on type {effectType.Name}. Effect won't work.");
                    continue;
                }

                foreach (var dataType in usedDataTypes.Where(dataType => genericArgs[0] == dataType))
                {
                    if (dataToEffectDict.ContainsKey(dataType))
                    {
                        Debug.LogWarning($"{dataType.Name} is used by multiple effects - this is not supported. Only {dataToEffectDict[dataType].effectType.Name} will be used.");
                        continue;
                    }

                    var attr = effectType.GetCustomAttribute(typeof(PostProcessOrderAttribute)) as PostProcessOrderAttribute;
                    var order = attr?.Order ?? 0;
                    dataToEffectDict.Add(dataType, (effectType, order));
                }
            }

            // Order passes by their declared priority
            var sorted = dataToEffectDict.OrderBy(kvp => kvp.Value.order);

            var allSensors = UnityEngine.Object.FindObjectsOfType<SensorBase>();
            var additionalRequiredPassTypes = new List<Type>();

            foreach (var sensor in allSensors)
            {
                var attr = sensor.GetType().GetCustomAttribute(typeof(RequireCustomPassAttribute)) as RequireCustomPassAttribute;
                if (attr == null)
                    continue;

                var type = attr.RequiredPassType;

                if (type == null || additionalRequiredPassTypes.Contains(type))
                    continue;

                if (!typeof(CustomPass).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"Type {type.Name} is not derived from {nameof(CustomPass)}.");
                    continue;
                }

                additionalRequiredPassTypes.Add(type);
            }

            postProcessingPasses.Clear();
            preDistortionSensorSwaps.Clear();
            preDistortionLastTarget.Clear();
            postDistortionSensorSwaps.Clear();
            postDistortionLastTarget.Clear();

            var customPassManager = SimulatorManager.Instance.CustomPassManager;

            // Add all passes explicitly requested by sensors
            foreach (var type in additionalRequiredPassTypes)
                customPassManager.AddPass(type, CustomPassInjectionPoint.AfterPostProcess, -100);

            // Add all used passes to volume
            foreach (var kvp in sorted)
            {
                var pass = customPassManager.AddPass(kvp.Value.effectType, CustomPassInjectionPoint.AfterPostProcess,
                    kvp.Value.order, CustomPass.TargetBuffer.Camera, CustomPass.TargetBuffer.None);
                postProcessingPasses.Add(kvp.Key, pass);
                postProcessingOrders.Add(kvp.Key, kvp.Value.order);
            }

            // Remove all invalid data types from sensors to keep valid swap count
            foreach (var cameraSensor in cameraSensors)
            {
                if (cameraSensor.Postprocessing == null)
                    continue;

                for (var i = 0; i < cameraSensor.Postprocessing.Count; ++i)
                {
                    var postprocessType = cameraSensor.Postprocessing[i].GetType();
                    if (!postProcessingPasses.ContainsKey(postprocessType))
                        cameraSensor.Postprocessing.RemoveAt(i--);

                    if (!cameraSensor.Distorted)
                        continue;

                    // Move all post-distortion postprocessing passes to separate queue 
                    if (postProcessingOrders[postprocessType] >= LatePostprocessOrder)
                    {
                        if (cameraSensor.LatePostprocessing == null)
                            cameraSensor.LatePostprocessing = new List<PostProcessData>();
                        
                        cameraSensor.LatePostprocessing.Add(cameraSensor.Postprocessing[i]);
                        cameraSensor.Postprocessing.RemoveAt(i--);
                    }
                }

                // Make sure dictionaries have records for all valid sensors
                if (cameraSensor.Postprocessing.Count > 0 || cameraSensor.LatePostprocessing != null && cameraSensor.LatePostprocessing.Count > 0)
                {
                    preDistortionSensorSwaps.Add(cameraSensor, 0);
                    preDistortionLastTarget.Add(cameraSensor, null);
                    postDistortionSensorSwaps.Add(cameraSensor, 0);
                    postDistortionLastTarget.Add(cameraSensor, null);
                }
            }
        }

        public void Deinitialize()
        {
            foreach (var stack in rtHandlePools.Select(kvp => kvp.Value).Where(stack => stack != null))
            {
                while (stack.Count > 0)
                    RTHandles.Release(stack.Pop());
            }

            rtHandlePools.Clear();
        }

        private RTHandle GetPooledHandle(RTHandle colorBuffer, bool autoSize)
        {
            var cube = colorBuffer.rt.dimension == TextureDimension.Cube;
            var hashCode = GetHandleHashCode(colorBuffer, autoSize, cube);

            if (rtHandlePools.TryGetValue(hashCode, out var stack) && stack.Count > 0)
                return stack.Pop();

            var rt = autoSize && !cube
                ? RTHandles.Alloc(
                    colorBuffer.scaleFactor,
                    TextureXR.slices,
                    DepthBits.None,
                    colorBuffer.rt.graphicsFormat,
                    dimension: TextureXR.dimension,
                    useMipMap: colorBuffer.rt.useMipMap,
                    enableRandomWrite: true,
                    useDynamicScale: true,
                    name: "Sensor PP RT Pool " + usedHandleCount)
                : RTHandles.Alloc(
                    colorBuffer.referenceSize.x,
                    colorBuffer.referenceSize.y,
                    TextureXR.slices,
                    DepthBits.None,
                    colorBuffer.rt.graphicsFormat,
                    dimension: TextureXR.dimension,
                    useMipMap: colorBuffer.rt.useMipMap,
                    enableRandomWrite: true,
                    useDynamicScale: true,
                    wrapMode: TextureWrapMode.Clamp,
                    name: "Sensor PP RT Pool " + usedHandleCount
                );

            usedHandleCount++;
            return rt;
        }

        private void ReturnHandleToPool(RTHandle rt, bool autoSize)
        {
            Assert.IsNotNull(rt);
            var hashCode = GetHandleHashCode(rt, autoSize, rt.rt.dimension == TextureDimension.Cube);

            if (!rtHandlePools.TryGetValue(hashCode, out var stack))
            {
                stack = new Stack<RTHandle>();
                rtHandlePools.Add(hashCode, stack);
            }

            stack.Push(rt);
        }

        /// <summary>
        /// Prepares and returns source and target RT handles for a single sensor postprocessing pass.
        /// </summary>
        /// <param name="cmd">Buffer used to queue commands.</param>
        /// <param name="colorBuffer">Original color buffer of the sensor.</param>
        /// <param name="sensor">Sensor that will have postprocessing effects rendered.</param>
        /// <param name="autoSize">Does the <see cref="colorBuffer"/> use auto-scaling?</param>
        /// <param name="lateQueue">True if executed after distortion, false otherwise.</param>
        /// <param name="source"><see cref="RTHandle"/> that should be used as a source (can be sampled).</param>
        /// <param name="target"><see cref="RTHandle"/> that should be used as a target (can't be sampled).</param>
        /// <param name="cubemapFace">Specifies target face if cubemap is used.</param>
        /// <exception cref="Exception">Sensor has no postprocessing effects.</exception>
        public void GetRTHandles(CommandBuffer cmd, RTHandle colorBuffer, CameraSensorBase sensor, bool autoSize, bool lateQueue, out RTHandle source, out RTHandle target, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            /* NOTE:
             * Each pass requires source texture (to sample from) and target (to render to). Since texture bound as a
             * target can't be sampled, temporary one is needed.
             * It's possible to only use two textures for rendering - original color buffer and temporary texture will
             * be swapped back and forth as source and target. If amount of passes is odd, at least one copy is needed
             * to make sure that final pass is always rendered to color buffer. This copy happens before the last pass.
             * Multiple sensors will use one temporary texture pool, assuming that their color buffers have the same
             * properties (size, format, dimension etc.).
             */

            var sensorSwaps = lateQueue ? postDistortionSensorSwaps : preDistortionSensorSwaps;
            var lastTarget = lateQueue ? postDistortionLastTarget : preDistortionLastTarget;
            var passCount = lateQueue ? sensor.LatePostprocessing?.Count ?? 0 : sensor.Postprocessing?.Count ?? 0;

            if (passCount == 0)
                throw new Exception("Sensor has no postprocessing passes defined.");

            var isLastPass = sensorSwaps[sensor] == passCount - 1;

            // Rendering to cubemap is a special case. We never want to force explicit face binding in Render() method,
            // so postprocessing only exposes intermediate, non-cubemap textures. In the final step, result will be
            // blit to desired cubemap face, reducing complexity of te Render() method. 
            if (cubemapFace != CubemapFace.Unknown)
            {
                // First pass - copy specific face to target with TextureXR format, prepare target with same format
                if (lastTarget[sensor] == null)
                {
                    source = GetPooledHandle(colorBuffer, autoSize);
                    cmd.CopyTexture(colorBuffer, (int) cubemapFace, 0, 0, 0, colorBuffer.rt.width,
                        colorBuffer.rt.height, source, 0, 0, 0, 0);
                }
                else
                {
                    source = lastTarget[sensor];
                }

                target = GetPooledHandle(colorBuffer, autoSize);
            }
            // Rendering to non-cubemap texture can use color buffer as intermediate target - just make sure that final
            // pass will be rendered to color buffer, not pooled RT (happens for odd number of passes).
            else
            {
                if (lastTarget[sensor] == colorBuffer)
                {
                    // Last pass has to be rendered to color buffer - copy is needed
                    if (isLastPass)
                    {
                        source = GetPooledHandle(colorBuffer, autoSize);
                        target = colorBuffer;
                        cmd.CopyTexture(colorBuffer, 0, source, 0);
                    }
                    else
                    {
                        source = colorBuffer;
                        target = GetPooledHandle(colorBuffer, autoSize);
                    }
                }
                else if (lastTarget[sensor] == null)
                {
                    // Last pass has to be rendered to color buffer - copy is needed
                    if (isLastPass)
                    {
                        source = GetPooledHandle(colorBuffer, autoSize);
                        target = colorBuffer;
                        cmd.CopyTexture(colorBuffer, 0, source, 0);
                    }
                    else
                    {
                        source = colorBuffer;
                        target = GetPooledHandle(colorBuffer, autoSize);
                    }
                }
                else
                {
                    source = lastTarget[sensor];
                    target = colorBuffer;
                }
            }

            // This was last pass in this frame - reset counters
            if (isLastPass)
            {
                lastTarget[sensor] = null;
                sensorSwaps[sensor] = 0;
            }
            // Keep track of last target and amount of passes completed (for further passes) 
            else
            {
                lastTarget[sensor] = target;
                sensorSwaps[sensor]++;
            }
        }

        /// <summary>
        /// Returns given <see cref="RTHandle"/> to its pool, unless it's the original color buffer for the sensor.
        /// </summary>
        /// <param name="source">Texture to return to pool.</param>
        /// <param name="colorBuffer">Original color buffer of the sensor.</param>
        /// <param name="autoSize">Does the <see cref="source"/> use auto-scaling?</param>
        public void RecycleSourceRT(RTHandle source, RTHandle colorBuffer, bool autoSize)
        {
            // Original color buffer doesn't belong to pool, so don't return it
            if (source == colorBuffer)
                return;

            ReturnHandleToPool(source, autoSize);
        }

        public void TryPerformFinalCubemapPass(CommandBuffer cmd, CameraSensorBase sensor, RTHandle source, RTHandle colorBuffer, CubemapFace cubemapFace)
        {
            // Cubemap is only used pre-distortion, hence the usage of pre-distortion queue.
            // This method is always executed after actual render and RT swapping, so if last target is null, the
            // postprocessing stack for this sensor was finished - this means that we have to copy intermediate texture
            // to final cubemap face in color buffer.
            if (preDistortionLastTarget[sensor] == null)
            {
                cmd.CopyTexture(source, 0, 0, 0, 0, source.rt.width, source.rt.height, colorBuffer,
                    (int) cubemapFace, 0, 0, 0);
                RecycleSourceRT(source, colorBuffer, false);
            }
        }

        /// <summary>
        /// Skips rendering of a single pass for given sensor, while keeping valid state of the postprocessing stack.
        /// </summary>
        /// <param name="cmd">Buffer used to queue commands.</param>
        /// <param name="colorBuffer">Original color buffer of the sensor.</param>
        /// <param name="sensor">Sensor that will have postprocessing effects rendered.</param>
        /// <param name="autoSize">Does the <see cref="colorBuffer"/> use auto-scaling?</param>
        /// <param name="lateQueue">True if executed after distortion, false otherwise.</param>
        /// <exception cref="Exception">Sensor has no postprocessing effects.</exception>
        public void Skip(CommandBuffer cmd, RTHandle colorBuffer, CameraSensorBase sensor, bool autoSize, bool lateQueue)
        {
            var sensorSwaps = lateQueue ? postDistortionSensorSwaps : preDistortionSensorSwaps;
            var lastTarget = lateQueue ? postDistortionLastTarget : preDistortionLastTarget;
            var passCount = lateQueue ? sensor.LatePostprocessing?.Count ?? 0 : sensor.Postprocessing?.Count ?? 0;

            if (passCount == 0)
                throw new Exception("Sensor has no postprocessing passes defined.");

            var isLastPass = sensorSwaps[sensor] == passCount - 1;

            // This was last pass in this frame - reset counters
            if (isLastPass)
            {
                // Last render was done to temporary texture - it has to be copied to color buffer (it's last pass)
                if (lastTarget[sensor] != colorBuffer && lastTarget[sensor] != null)
                {
                    cmd.CopyTexture(lastTarget[sensor], 0, colorBuffer, 0);
                    RecycleSourceRT(lastTarget[sensor], colorBuffer, autoSize);
                }

                lastTarget[sensor] = null;
                sensorSwaps[sensor] = 0;
            }
            // Keep track of amount of passes completed (or skipped), but don't change last target with valid image 
            else
            {
                sensorSwaps[sensor]++;
            }
        }

        /// <summary>
        /// <para>Renders all pre-distortion postprocessing effects declared for given sensor.</para>
        /// <para>
        /// This should only be used on cameras with custom render method. Cameras using HDRP passes will render all
        /// effects automatically.
        /// </para>
        /// </summary>
        /// <param name="ctx">Context used for this pass execution.</param>
        /// <param name="sensor">Sensor that should have postprocessing effects rendered.</param>
        /// <param name="cubemapFace">Specifies target face if cubemap is used.</param>
        public void RenderForSensor(PostProcessPassContext ctx, CameraSensorBase sensor, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            if (sensor.Postprocessing == null || sensor.Postprocessing.Count == 0)
                return;

            foreach (var data in sensor.Postprocessing)
            {
                foreach (var kvp in postProcessingPasses)
                {
                    if (data.GetType() == kvp.Key)
                        RenderPostProcess(ctx, sensor, kvp.Value, data, cubemapFace);
                }
            }
        }

        /// <summary>
        /// <para>Renders all post-distortion postprocessing effects declared for given sensor.</para>
        /// </summary>
        /// <param name="ctx">Context used for this pass execution.</param>
        /// <param name="sensor">Sensor that should have postprocessing effects rendered.</param>
        public void RenderLateForSensor(PostProcessPassContext ctx, CameraSensorBase sensor)
        {
            if (sensor.LatePostprocessing == null || sensor.LatePostprocessing.Count == 0)
                return;

            foreach (var data in sensor.LatePostprocessing)
            {
                foreach (var kvp in postProcessingPasses)
                {
                    if (data.GetType() == kvp.Key)
                        RenderPostProcess(ctx, sensor, kvp.Value, data);
                }
            }
        }

        /// <summary>
        /// Returns true if postprocessing effect for given data type is in late queue, false otherwise.
        /// </summary>
        /// <param name="sensor">Sensor for which check is performed.</param>
        /// <param name="type">Data type derived from <see cref="PostProcessData"/>.</param>
        public bool IsLatePostprocess(CameraSensorBase sensor, Type type)
        {
            if (!typeof(PostProcessData).IsAssignableFrom(type))
                throw new Exception($"Order is only stored for types derived from {nameof(PostProcessData)}.");

            // Late queue might be unused even for late-order effects if distortion is not enabled
            if (sensor.LatePostprocessing == null)
                return false;

            return postProcessingOrders[type] >= LatePostprocessOrder;
        }
    }
}