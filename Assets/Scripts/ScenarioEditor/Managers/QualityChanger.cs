/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public class QualityChanger : MonoBehaviour
    {
        /// <summary>
        /// How many times maximum distance of the lod and shadows will be multiplied in the VSE
        /// </summary>
        private const float QualityDistanceMultiplier = 2.0f;

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Rendering volume for the scenario maps
        /// </summary>
        [SerializeField]
        private Volume volume;
#pragma warning restore 0649

        private bool initialized;
        
        void Start()
        {
            Initialize();
        }

        void OnDestroy()
        {
            Deinitialize();
        }

        private void OnApplicationQuit()
        {
            Deinitialize();
        }

        private void Initialize()
        {
            if (initialized)
                return;
            
            //Increase shadows distance
            var shadows =
                volume.profile.components.Find(component => component is HDShadowSettings) as HDShadowSettings;
            if (shadows != null)
                shadows.maxShadowDistance.value *= QualityDistanceMultiplier;
            
            initialized = true;
        }

        private void Deinitialize()
        {
            if (!initialized)
                return;
            
            //Revert shadows distance
            var shadows =
                volume.profile.components.Find(component => component is HDShadowSettings) as HDShadowSettings;
            if (shadows != null)
                shadows.maxShadowDistance.value /= QualityDistanceMultiplier;
            
            initialized = false;
        }
    }
}