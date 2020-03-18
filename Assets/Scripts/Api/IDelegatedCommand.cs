/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Net;
using SimpleJSON;

namespace Simulator.Api
{
    public interface IDelegatedCommand : ICommand
    {
        IPEndPoint TargetNodeEndPoint(JSONNode args);
    }
}