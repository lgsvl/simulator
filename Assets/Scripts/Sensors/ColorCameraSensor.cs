/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    using System.Linq;
    using Simulator.Bridge.Data;
    using Simulator.Utilities;
    using UnityEngine;
    using UnityEngine.Rendering.HighDefinition;

    [SensorType("Color Camera", new[] { typeof(ImageData) })]
    public class ColorCameraSensor : CameraSensorBase
    {
        // fix for 1000+ ms lag spike that appears approx. 1 s after starting
        // simulation from quickscript will now happen after sensor
        // initialization instead of mid-simulation
        // NOTE this fix is dependent on HDRP version
        
        private int renderedFrames;
        private int requiredFrames;
        
        public override void Start()
        {
            base.Start();
            SetupSkyWarmup();
        }
        
        protected override void Update()
        {
            base.Update();
            CheckSkyWarmup();
        }

        private void SetupSkyWarmup()
        {
            renderedFrames = 0;
            requiredFrames = 0;

            var activeProfile = SimulatorManager.Instance.EnvironmentEffectsManager.ActiveProfile;
            var pbrSky = activeProfile.components.FirstOrDefault(x => x is PhysicallyBasedSky) as PhysicallyBasedSky;
            if (pbrSky == null)
                return;

            requiredFrames = pbrSky.numberOfBounces.value;
        }
        
        private void CheckSkyWarmup()
        {
            if (renderedFrames > requiredFrames)
                return;

            renderedFrames++;
            RenderCamera();
        }
    }
}
