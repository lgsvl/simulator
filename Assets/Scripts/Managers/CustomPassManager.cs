/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class CustomPassManager : MonoBehaviour
{
    private Dictionary<CustomPassInjectionPoint, CustomPassVolume> volumes = new Dictionary<CustomPassInjectionPoint, CustomPassVolume>();
    private Dictionary<CustomPass, int> order = new Dictionary<CustomPass, int>();
    private Queue<CommandBuffer> queuedBuffers = new Queue<CommandBuffer>();
    private Camera cam;

    private CustomPassVolume GetVolume(CustomPassInjectionPoint injectionPoint)
    {
        if (volumes.ContainsKey(injectionPoint))
        {
            return volumes[injectionPoint];
        }

        var volume = gameObject.AddComponent<CustomPassVolume>();
        volume.injectionPoint = injectionPoint;
        volume.isGlobal = true;
            
        volumes.Add(injectionPoint, volume);
        return volume;
    }

    public T AddPass<T>(CustomPassInjectionPoint injectionPoint, int priority = 0,
        CustomPass.TargetBuffer targetColorBuffer = CustomPass.TargetBuffer.Camera,
        CustomPass.TargetBuffer targetDepthBuffer = CustomPass.TargetBuffer.Camera) where T : CustomPass
    {
        return AddPass(typeof(T), injectionPoint, priority, targetColorBuffer, targetDepthBuffer) as T;
    }

    public CustomPass AddPass(Type passType, CustomPassInjectionPoint injectionPoint, int priority = 0,
        CustomPass.TargetBuffer targetColorBuffer = CustomPass.TargetBuffer.Camera,
        CustomPass.TargetBuffer targetDepthBuffer = CustomPass.TargetBuffer.Camera)
    {
        var volume = GetVolume(injectionPoint);
        var passes = volume.customPasses;

        if (!typeof(CustomPass).IsAssignableFrom(passType))
        {
            throw new Exception($"Can't add pass type {passType} to the list because it does not inherit from {nameof(CustomPass)}.");
        }

        var instance = Activator.CreateInstance(passType) as CustomPass;
        if (instance == null)
        {
            throw new Exception($"Failed instance creation of type {passType}");
        }

        instance.name = passType.Name;
        instance.targetColorBuffer = targetColorBuffer;
        instance.targetDepthBuffer = targetDepthBuffer;
        order.Add(instance, priority);

        for (var i = 0; i < passes.Count; ++i)
        {
            if (order[passes[i]] > priority)
            {
                passes.Insert(i, instance);
                return instance;
            }
        }

        passes.Add(instance);
        return instance;
    }

    public void ExecuteAndClearCommandBuffer(CommandBuffer buffer)
    {
        if (cam == null)
        {
            cam = gameObject.AddComponent<Camera>();
            var hdData = gameObject.AddComponent<HDAdditionalCameraData>();
            hdData.hasPersistentHistory = true;
            hdData.customRender += CustomRender;
        }
        
        queuedBuffers.Enqueue(buffer);
        cam.Render();
    }

    private void CustomRender(ScriptableRenderContext context, HDCamera hdCamera)
    {
        while (queuedBuffers.Count > 0)
        {
            var buffer = queuedBuffers.Dequeue();
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
    }
}