/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

﻿using System;

namespace Simulator.Bridge.Ros2
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class MessageTypeAttribute : Attribute
    {
        public string Type { get; private set; }

        public MessageTypeAttribute(string type)
        {
            Type = type;
        }
    }
}
