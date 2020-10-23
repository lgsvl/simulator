/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using System;

    /// <summary>
    /// Attribute usable on classes derived from <see cref="SensorBase"/> to indicate their requirement of a custom
    /// pass in after postprocess injection point.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RequireCustomPassAttribute : Attribute
    {
        /// <summary>
        /// <para>Pass type required by this sensor.</para>
        /// </summary>
        public Type RequiredPassType { get; }

        /// <summary>
        /// Attribute usable on classes derived from <see cref="SensorBase"/> to indicate their requirement of a custom
        /// pass in after postprocess injection point.
        /// </summary>
        /// <param name="requiredPassType">
        /// <para>Pass type required by this sensor.</para>
        /// </param>
        public RequireCustomPassAttribute(Type requiredPassType)
        {
            RequiredPassType = requiredPassType;
        }
    }
}