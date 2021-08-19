/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities
{
    using UnityEngine;

    public static class UniformlyAcceleratedMotion
    {
        /// <summary>
        /// Calculates the acceleration duration
        /// </summary>
        /// <param name="acceleration">Acceleration</param>
        /// <param name="initialSpeed">Initial movement speed</param>
        /// <param name="distance">Maximum distance of the movement</param>
        /// <param name="maxSpeed">Maximum speed that can be reached, can be limited if object will not accelerate enough in the given distance</param>
        /// <param name="accelerationDuration">Required time for acceleration</param>
        /// <param name="accelerationDistance">Required distance for acceleration</param>
        /// <returns>True, if maximum speed will be reached, false otherwise</returns>
        public static bool CalculateDuration(float acceleration, float initialSpeed, float distance,
            ref float maxSpeed, out float accelerationDuration, out float accelerationDistance)
        {
            // If max speed is lower than the initial speed convert acceleration to deceleration
            if (maxSpeed < initialSpeed && acceleration > 0.0f)
                acceleration *= -1.0f;
            
            accelerationDuration = (maxSpeed - initialSpeed) / acceleration;
            accelerationDistance = initialSpeed * accelerationDuration +
                                       acceleration * accelerationDuration * accelerationDuration / 2.0f;
            
            // Check if ego vehicle can reach maximum speed when accelerating
            if (accelerationDistance >= distance)
            {
                // Limit acceleration according to the distance
                var sqrtDelta =
                    Mathf.Sqrt(4.0f * initialSpeed * initialSpeed + 8.0f * acceleration * distance);
                accelerationDuration = (-2.0f * initialSpeed + sqrtDelta) / (2.0f * acceleration);
                accelerationDistance = distance;
                maxSpeed = initialSpeed + acceleration * accelerationDuration;
                return false;
            }

            return true;
        }

        public static float CalculateDistance(float acceleration, float initialSpeed, float time)
        {
            return (initialSpeed * time + acceleration * time * time / 2.0f);
        }
    }
}