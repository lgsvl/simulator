/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;

namespace Api
{
    interface ICommand
    {
        string Name { get; }
        void Execute(JSONNode args);
    }
}
