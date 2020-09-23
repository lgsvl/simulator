/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Utilities
{
    using Simulator.Utilities;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    /// <summary>
    /// Manager which changes the graphics quality settings for the VSE
    /// </summary>
    public class QualityChanger : MonoBehaviour
    {
        /// <summary>
        /// How many times maximum distance of the lod and shadows will be multiplied in the VSE
        /// </summary>
        private const float QualityDistanceMultiplier = 8.0f;

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Rendering volume for the scenario maps
        /// </summary>
        [SerializeField]
        private Volume volume;
        
        /// <summary>
        /// Default settings of the HDRP
        /// </summary>
        [SerializeField]
        private HDRenderPipelineAsset defaultHdrpSettings;
        
        /// <summary>
        /// VSE settings of the HDRP
        /// </summary>
        [SerializeField]
        private HDRenderPipelineAsset vseHdrpSettings;
#pragma warning restore 0649

        /// <summary>
        /// Is initialized
        /// </summary>
        private bool initialized;
        
        /// <summary>
        /// Unity Start method
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            Deinitialize(true);
        }

        /// <summary>
        /// Unity OnApplicationQuit method
        /// </summary>
        private void OnApplicationQuit()
        {
            Deinitialize(false);
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        private void Initialize()
        {
            if (initialized)
                return;
            
            //Increase shadows distance
            var shadows =
                volume.profile.components.Find(component => component is HDShadowSettings) as HDShadowSettings;
            if (shadows != null)
                shadows.maxShadowDistance.value *= QualityDistanceMultiplier;

            QualitySettings.renderPipeline = vseHdrpSettings;
            HDRPUtilities.ReinitializeRenderPipeline();
            
            initialized = true;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        /// <param name="reinitializeRenderPipeline">Should reinitialize render pipeline after changes</param>
        private void Deinitialize(bool reinitializeRenderPipeline)
        {
            if (!initialized)
                return;
            
            //Revert shadows distance
            var shadows =
                volume.profile.components.Find(component => component is HDShadowSettings) as HDShadowSettings;
            if (shadows != null)
                shadows.maxShadowDistance.value /= QualityDistanceMultiplier;
            
            QualitySettings.renderPipeline = defaultHdrpSettings;
            if (reinitializeRenderPipeline)
                HDRPUtilities.ReinitializeRenderPipeline();
            
            initialized = false;
        }
    }
}