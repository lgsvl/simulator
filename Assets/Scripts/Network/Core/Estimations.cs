/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core
{
    using UnityEngine;

    /// <summary>
    /// Static class counting estimations of the values like using linear interpolation
    /// </summary>
    public static class Estimations
    {
        /// <summary>
        /// Unclamped linear interpolation between two values
        /// </summary>
        /// <param name="startPosition">Start position of the interpolation</param>
        /// <param name="endPosition">End position of the interpolation</param>
        /// <param name="t">Interpolation's fraction, may exceed 1.0 to extrapolate</param>
        /// <returns>Unclamped linear interpolation between start and end value in fraction t</returns>
        public static Vector3 LinearInterpolation(Vector3 startPosition, Vector3 endPosition, float t)
        {
            var result = Vector3.LerpUnclamped(startPosition, endPosition, t);
            return result;
        }

        /// <summary>
        /// Clamped linear interpolation between two angles
        /// </summary>
        /// <param name="startEulerRotation">Start euler rotation of the interpolation</param>
        /// <param name="endEulerRotation">End euler rotation of the interpolation</param>
        /// <param name="t">Interpolation's fraction, may exceed 1.0 to extrapolate</param>
        /// <returns>Linear angles interpolation between start and end rotation in fraction t</returns>
        public static Vector3 LinearAngleInterpolation(Vector3 startEulerRotation, Vector3 endEulerRotation, float t)
        {
            var result = new Vector3(Mathf.LerpAngle(startEulerRotation.x, endEulerRotation.x, t),
                Mathf.LerpAngle(startEulerRotation.y, endEulerRotation.y, t),
                Mathf.LerpAngle(startEulerRotation.z, endEulerRotation.z, t));
            return result;
        }

        /// <summary>
        /// Unclamped spherical interpolation between two values
        /// </summary>
        /// <param name="startPosition">Start position of the interpolation</param>
        /// <param name="endPosition">End position of the interpolation</param>
        /// <param name="t">Interpolation's fraction, may exceed 1.0 to extrapolate</param>
        /// <returns>Unclamped spherical interpolation between start and end value in fraction t</returns>
        public static Vector3 SphericalInterpolation(Vector3 startPosition, Vector3 endPosition, float t)
        {
            var result = Vector3.SlerpUnclamped(startPosition, endPosition, t);
            return result;
        }

        /// <summary>
        /// Unclamped spherical interpolation between two values
        /// </summary>
        /// <param name="startRotation">Start rotation of the interpolation</param>
        /// <param name="endRotation">End rotation of the interpolation</param>
        /// <param name="t">Interpolation's fraction, may exceed 1.0 to extrapolate</param>
        /// <returns>Unclamped spherical interpolation between start and end value in fraction t</returns>
        public static Quaternion SphericalInterpolation(Quaternion startRotation, Quaternion endRotation, float t)
        {
            var result = Quaternion.SlerpUnclamped(startRotation, endRotation, t);
            return result;
        }
    }
}