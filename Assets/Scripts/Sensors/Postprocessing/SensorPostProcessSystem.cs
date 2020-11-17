/**
 * Copyright (c) 2020 LG Electronics, Inc.
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
        private readonly Dictionary<Type, CustomPass> postProcessingPasses = new Dictionary<Type, CustomPass>();
        private readonly Dictionary<int, Stack<RTHandle>> rtHandlePools = new Dictionary<int, Stack<RTHandle>>();
        private readonly Dictionary<CameraSensorBase, int> sensorSwaps = new Dictionary<CameraSensorBase, int>();
        private readonly Dictionary<CameraSensorBase, RTHandle> lastTarget = new Dictionary<CameraSensorBase, RTHandle>();

        private int usedHandleCount;

        private static int GetHandleHashCode(RTHandle handle, bool autoSize)
        {
            // HDRP uses auto-scaled RTHandles, but sensors declare exact resolution for each.
            // Make sure these two are not mixed.
            var hashCode = autoSize
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

        private static void RenderPostProcess<T>(CommandBuffer cmd, HDCamera hdCamera, CameraSensorBase sensor, RTHandle sensorColorBuffer, CustomPass pass, T data) where T : PostProcessData
        {
            var postProcessPass = pass as IPostProcessRenderer;
            postProcessPass?.Render(cmd, hdCamera, sensor, sensorColorBuffer, data);
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
                    Debug.LogError($"Unexpected generic argument type count on type {effectType.Name}. Effect won't work.");
                    continue;
                }

                foreach (var dataType in usedDataTypes.Where(dataType => genericArgs[0] == dataType))
                {
                    if (dataToEffectDict.ContainsKey(dataType))
                    {
                        Debug.LogError($"{dataType.Name} is used by multiple effects - this is not supported. Only {dataToEffectDict[dataType].effectType.Name} will be used.");
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
                    Debug.LogError($"Type {type.Name} is not derived from {nameof(CustomPass)}.");
                    continue;
                }

                additionalRequiredPassTypes.Add(type);
            }

            postProcessingPasses.Clear();
            sensorSwaps.Clear();
            lastTarget.Clear();

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
            }

            // Remove all invalid data types from sensors to keep valid swap count
            foreach (var cameraSensor in cameraSensors)
            {
                if (cameraSensor.Postprocessing == null)
                    continue;

                for (var i = 0; i < cameraSensor.Postprocessing.Count; ++i)
                {
                    if (!postProcessingPasses.ContainsKey(cameraSensor.Postprocessing[i].GetType()))
                        cameraSensor.Postprocessing.RemoveAt(i--);
                }

                // Make sure dictionaries have records for all valid sensors
                if (cameraSensor.Postprocessing.Count > 0)
                {
                    sensorSwaps.Add(cameraSensor, 0);
                    lastTarget.Add(cameraSensor, null);
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
            var hashCode = GetHandleHashCode(colorBuffer, autoSize);

            if (rtHandlePools.TryGetValue(hashCode, out var stack) && stack.Count > 0)
                return stack.Pop();

            var rt = autoSize
                ? RTHandles.Alloc(
                    colorBuffer.scaleFactor,
                    TextureXR.slices,
                    DepthBits.None,
                    colorBuffer.rt.graphicsFormat,
                    dimension: colorBuffer.rt.dimension,
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
            var hashCode = GetHandleHashCode(rt, autoSize);

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
        /// <param name="source"><see cref="RTHandle"/> that should be used as a source (can be sampled).</param>
        /// <param name="target"><see cref="RTHandle"/> that should be used as a target (can't be sampled).</param>
        /// <exception cref="Exception">Sensor has no postprocessing effects.</exception>
        public void GetRTHandles(CommandBuffer cmd, RTHandle colorBuffer, CameraSensorBase sensor, bool autoSize, out RTHandle source, out RTHandle target)
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

            var passCount = sensor.Postprocessing?.Count ?? 0;
            if (passCount == 0)
                throw new Exception("Sensor has no postprocessing passes defined.");

            var isLastPass = sensorSwaps[sensor] == passCount - 1;

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

        /// <summary>
        /// Skips rendering of a single pass for given sensor, while keeping valid state of the postprocessing stack.
        /// </summary>
        /// <param name="cmd">Buffer used to queue commands.</param>
        /// <param name="colorBuffer">Original color buffer of the sensor.</param>
        /// <param name="sensor">Sensor that will have postprocessing effects rendered.</param>
        /// <param name="autoSize">Does the <see cref="colorBuffer"/> use auto-scaling?</param>
        /// <exception cref="Exception">Sensor has no postprocessing effects.</exception>
        public void Skip(CommandBuffer cmd, RTHandle colorBuffer, CameraSensorBase sensor, bool autoSize)
        {
            var passCount = sensor.Postprocessing?.Count ?? 0;
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
        /// <para>Renders all postprocessing effects declared for given sensor.</para>
        /// <para>
        /// This should only be used on cameras with custom render method. Cameras using HDRP passes will render all
        /// effects automatically.
        /// </para>
        /// </summary>
        /// <param name="cmd">Buffer used to queue commands.</param>
        /// <param name="hdCamera">HD camera used by the sensor.</param>
        /// <param name="sensor">Sensor that should have postprocessing effects rendered.</param>
        /// <param name="target"><see cref="RTHandle"/> used as target for the sensor.</param>
        public void RenderForSensor(CommandBuffer cmd, HDCamera hdCamera, CameraSensorBase sensor, RTHandle target)
        {
            foreach (var kvp in postProcessingPasses)
            {
                foreach (var data in sensor.Postprocessing)
                {
                    if (data.GetType() == kvp.Key)
                        RenderPostProcess(cmd, hdCamera, sensor, target, kvp.Value, data);
                }
            }
        }
    }
}