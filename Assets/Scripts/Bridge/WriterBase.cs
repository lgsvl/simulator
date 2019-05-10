/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Bridge
{
    public interface WriterBase<T>
    {
        void Publish(T message, Action completed = null);
    }
}