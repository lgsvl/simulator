/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System.Collections.Generic;
    using UnityEngine;
    
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class SceneViewBufferBuilder : BufferBuilder
    {
        private const float TimeStep = 0.0333f;
        
        private float lastEditorUpdateTime;
        private bool updateQueued;
        
        public SceneViewBufferBuilder(NodeLoader nodeLoader, int maxBufferElements, int rebuildSteps) : base(nodeLoader, maxBufferElements, rebuildSteps)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorApplication.update += OnEditorUpdate;
#endif
        }

#if UNITY_EDITOR
        private void OnEditorUpdate()
        {
            var timeSinceLastUpdate = Time.unscaledTime - lastEditorUpdateTime;
            if (timeSinceLastUpdate > TimeStep && busy && !updateQueued)
            {
                var prePointCount = readyBufferItemsCount;
                PerformBuildStep();
                if (!busy && readyBufferItemsCount != prePointCount)
                {
                    updateQueued = true;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }
#endif

        public override ComputeBuffer GetPopulatedBuffer(List<string> requiredNodes, out int validPointCount)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                lastEditorUpdateTime = 0f;
                if (updateQueued)
                {
                    updateQueued = false;
                    validPointCount = readyBufferItemsCount;
                    return ReadyBuffer;
                }
                return base.GetPopulatedBuffer(requiredNodes, out validPointCount);
            }
#endif
            return base.GetPopulatedBuffer(requiredNodes, out validPointCount);
        }

        public override void Dispose()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorApplication.update -= OnEditorUpdate;
#endif

            base.Dispose();
        }
    }
}