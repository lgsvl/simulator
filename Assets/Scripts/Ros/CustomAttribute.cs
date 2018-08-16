/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System;

namespace Ros
{
    [AttributeUsage(AttributeTargets.Struct)]
    class MessageTypeAttribute : Attribute
    {
        public string Type { get; private set; }
        public string Type2 { get; private set; }

        public MessageTypeAttribute(string type)
        {
            Type = Type2 = type;
        }

        public MessageTypeAttribute(string type, string type2)
        {
            Type = type;
            Type2 = type2;
        }
    }
}

namespace Apollo
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Class)]
    public class RequiredAttribute : System.Attribute { }
}

namespace Autoware
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class VectorMapCSVAttribute : System.Attribute
    {
        public string FileName { get; private set; }

        public VectorMapCSVAttribute(string filename)
        {
            FileName = filename;
        }
    }
}
