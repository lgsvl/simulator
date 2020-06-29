/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

﻿using System;

namespace Simulator.Bridge.Ros
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageTypeAttribute : Attribute
    {
        public string Type { get; private set; }

        public MessageTypeAttribute(string type)
        {
            Type = type;
        }
    }

    // TODO: this should go to VectorMap exporter
    [AttributeUsage(AttributeTargets.Field)]
    public class VectorMapCSVAttribute : Attribute
    {
        public string FileName { get; private set; }

        public VectorMapCSVAttribute(string filename)
        {
            FileName = filename;
        }
    }
}
