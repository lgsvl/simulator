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
    /// Attribute usable on classes derived from <see cref="PostProcessPass{TData}"/> to define their execution order
    /// on postprocessing stack.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PostProcessOrderAttribute : Attribute
    {
        /// <summary>
        /// <para>Defines execution order of the <see cref="PostProcessPass{TData}"/> this attribute is attached to.</para>
        /// <para>Lower values are executed sooner.</para>
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Attribute usable on classes derived from <see cref="PostProcessPass{TData}"/> to define their execution order
        /// on postprocessing stack.
        /// </summary>
        /// <param name="order">
        /// <para>Defines execution order of the <see cref="PostProcessPass{TData}"/> this attribute is attached to.</para>
        /// <para>Lower values are executed sooner.</para>
        /// </param>
        public PostProcessOrderAttribute(int order)
        {
            Order = order;
        }
    }
}