/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    using UnityEngine;
    using Utilities;

    public abstract class FrequencySensorBase : SensorBase
    {
        /// <summary>
        /// Defines frequency at which sensor will be updated.
        /// </summary>
        [SensorParameter]
        [Range(1, 100)]
        public float Frequency = 10;

        private double nextCaptureTime;

        /// <summary>
        /// If true, sensor update will be done in sync with FixedUpdate event method. Otherwise, Update will be used.
        /// </summary>
        protected abstract bool UseFixedUpdate { get; } 

        /// <summary>
        /// <para>Method that will be called with defined <see cref="Frequency"/>.</para>
        /// <para>If <see cref="UseFixedUpdate"/> is set to true, this will be synchronized with FixedUpdate loop.</para>
        /// <para>If <see cref="UseFixedUpdate"/> is set to false, this will be synchronized with Update loop.</para>
        /// </summary>
        protected virtual void SensorUpdate()
        {
        }

        protected virtual void Update()
        {
            if (UseFixedUpdate)
                return;

            var currentTime = SimulatorManager.Instance.CurrentTime; 

            if (currentTime < nextCaptureTime)
                return;

            SensorUpdate();

            if (nextCaptureTime < currentTime - Time.deltaTime)
                nextCaptureTime = currentTime + 1.0f / Frequency;
            else
                nextCaptureTime += 1.0f / Frequency;
        }

        protected void FixedUpdate()
        {
            if (!UseFixedUpdate)
                return;

            var currentTime = SimulatorManager.Instance.CurrentTime; 

            if (currentTime < nextCaptureTime)
                return;

            SensorUpdate();

            if (nextCaptureTime < currentTime - Time.fixedDeltaTime)
                nextCaptureTime = currentTime + 1.0f / Frequency;
            else
                nextCaptureTime += 1.0f / Frequency;
        }
    }
}