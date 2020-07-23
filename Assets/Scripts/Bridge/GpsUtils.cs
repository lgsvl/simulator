/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Bridge
{
    public static class GpsUtils
    {
        public static readonly DateTime GpsEpoch = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);

        // GPS Time requires leap seconds which is updated every several years.
        // https://racelogic.support/01VBOX_Automotive/01General_Information/Knowledge_Base/What_are_GPS_Leap_Seconds%3F
        // We keep a single copy of this value here for easy update later.
        public const float GpsLeapSeconds = 18.0f;

        public static double UtcSecondsToGpsSeconds(double utsSeconds)
        {
            var utc = DateTimeOffset.FromUnixTimeMilliseconds((long)(utsSeconds * 1000.0)).UtcDateTime;
            return UtcToGpsSeconds(utc);
        }

        public static double UtcToGpsSeconds(DateTime utc)
        {
            return (utc - GpsEpoch).TotalSeconds + GpsLeapSeconds;
        }
    }
}
