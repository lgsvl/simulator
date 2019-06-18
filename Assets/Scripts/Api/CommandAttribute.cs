/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Api
{
    [AttributeUsage(AttributeTargets.Class)]
    class CommandAttribute : Attribute
    {
        public string Name { get; private set; }

        public CommandAttribute(string name)
        {
            Name = name;
        }
    }
}
