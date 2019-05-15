/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Bridge
{
    public interface IWriter<T> where T : class
    {
        void Write(T message, Action completed = null);
    }
}
