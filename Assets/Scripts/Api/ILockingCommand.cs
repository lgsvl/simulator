/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Api
{
    public interface ILockingCommand : ICommand
    {
        event Action<ILockingCommand> Executed;
    }
}
