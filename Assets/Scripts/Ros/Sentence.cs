/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    [MessageType("nmea_msgs/Sentence")]
    public struct Sentence
    {
        public Header header;
        public string sentence;
    }
}
