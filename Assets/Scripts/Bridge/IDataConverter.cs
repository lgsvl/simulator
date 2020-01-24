/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Bridge
{
    public interface IDataConverter<T> : IDataConverter where T : class
    {
        Func<T, object> GetConverter(IBridge bridge);
        Type GetOutputType(IBridge bridge);
    }

    public interface IDataConverter
    {
    }
}