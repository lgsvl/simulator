/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System.Threading.Tasks;
namespace Simulator.Api
{
    public interface ICommand
    {
        string Name { get; }
        void Execute(JSONNode args);
    }
}
