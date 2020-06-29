/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge
{
    public class TopicData
    {
        public string Topic;
        public string Type;
        public int Count;
        public int StartCount;
        public float ElapsedTime;
        public float Frequency;

        public TopicData(string topic, string type)
        {
            Topic = topic;
            Type = type;
        }
    }
}
