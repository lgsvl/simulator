/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    [MessageType("std_msgs/Time", "builtin_interfaces/Time")]
    public struct Time
    {
        public long secs;
        public uint nsecs;

        private static long StartNanoSecs;
        private static System.Diagnostics.Stopwatch StartStopWatch;

        public static Time Now()
        {
            if (StartStopWatch == null)
            {
                var startTime = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 8, 0, 0, System.DateTimeKind.Utc);
                StartNanoSecs = (long)(startTime.TotalMilliseconds * 1000000);

                StartStopWatch = System.Diagnostics.Stopwatch.StartNew();
            }

            long nanosec = StartNanoSecs;
            nanosec += StartStopWatch.ElapsedTicks * 1000000000L / System.Diagnostics.Stopwatch.Frequency;

            long sec = nanosec / 1000000000;
            long nsec = nanosec - sec * 1000000000;

            return new Time()
            {
                secs = sec,
                nsecs = (uint)nsec,
            };
        }
    }
}
