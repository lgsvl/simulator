/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;

namespace Simulator.Map
{
    // Calculations are from
    // http://www.stjarnhimlen.se/comp/ppcomp.html
    // http://www.stjarnhimlen.se/comp/tutorial.html

    public static class SunMoonPosition
    {
        static class Degrees
        {
            public static double ToRadians(double degrees)
            {
                return degrees * Math.PI / 180.0;
            }

            public static double Sin(double x)
            {
                return Math.Sin(ToRadians(x));
            }

            public static double Cos(double x)
            {
                return Math.Cos(ToRadians(x));
            }

            public static double Acos(double x)
            {
                return Radians.ToDegrees(Math.Acos(x));
            }

            public static double Atan2(double y, double x)
            {
                return Normalize(Radians.ToDegrees(Math.Atan2(y, x)));
            }

            public static double Normalize(double x)
            {
                double result = Math.IEEERemainder(x, 360.0);
                if (result < 0.0)
                {
                    result += 360.0;
                }
                return result;
            }
        }

        static class Radians
        {
            public const float PI = (float)Math.PI;

            public static double ToDegrees(double radians)
            {
                return radians * 180.0 / Math.PI;
            }

            public static double Sin(double x)
            {
                return Math.Sin(x);
            }

            public static double Cos(double x)
            {
                return Math.Cos(x);
            }

            public static double Atan2(double y, double x)
            {
                return Math.Atan2(y, x);
            }

            public static double Normalize(double x)
            {
                double result = Math.IEEERemainder(x, Math.PI);
                if (result < 0.0)
                {
                    result += Math.PI;
                }
                return result;
            }

        }

        // Sun Calculations

        static void ConvertRectangularToSpherical(double x, double y, double z, out double rasc, out double decl, out double dist)
        {
            dist = Math.Sqrt(x * x + y * y + z * z);
            rasc = Degrees.Atan2(y, x);
            decl = Degrees.Atan2(z, Math.Sqrt(x * x + y * y));
        }

        static void ConvertEclipticToEquatorial(double jday, double lon, double lat, out double rasc, out double decl)
        {
            double d = jday - 2451543.5;
            double oblecl = 23.4393 - 3.563E-7 * d;

            double x = Degrees.Cos(lon) * Degrees.Cos(lat);
            double y = Degrees.Sin(lon) * Degrees.Cos(lat);
            double z = Degrees.Sin(lat);

            double xe = x;
            double ye = y * Degrees.Cos(oblecl) - z * Degrees.Sin(oblecl);
            double ze = y * Degrees.Sin(oblecl) + z * Degrees.Cos(oblecl);

            double r = Math.Sqrt(xe * xe + ye * ye);
            rasc = Degrees.Atan2(ye, xe);
            decl = Degrees.Atan2(ze, r);
        }

        static void ConvertEquatorialToHorizontal(double jday, double longitude, double latitude, double rasc, double decl, out double azimuth, out double altitude)
        {
            double d = jday - 2451543.5;
            double w = 282.9404 + 4.70935E-5 * d;
            double M = 356.0470 + 0.9856002585 * d;
            double L = w + M;
            double UT = Degrees.Normalize(Math.IEEERemainder(d, 1.0) * 360.0);
            double hourAngle = Degrees.Normalize(longitude + L + 180.0 + UT - rasc);

            double x = Degrees.Cos(hourAngle) * Degrees.Cos(decl);
            double y = Degrees.Sin(hourAngle) * Degrees.Cos(decl);
            double z = Degrees.Sin(decl);

            double xhor = x * Degrees.Sin(latitude) - z * Degrees.Cos(latitude);
            double yhor = y;
            double zhor = x * Degrees.Cos(latitude) + z * Degrees.Sin(latitude);

            azimuth = Degrees.Atan2(yhor, xhor) + 180.0;
            altitude = Degrees.Atan2(zhor, Math.Sqrt(xhor * xhor + yhor * yhor));
        }

        public static Quaternion GetSunPosition(double jday, double longitude, double latitude)
        {
            double d = jday - 2451543.5;

            double w = 282.9404 + 4.70935E-5 * d;
            double e = 0.016709 - 1.151E-9 * d;
            double M = Degrees.Normalize(356.0470 + 0.9856002585 * d);
            double E = Degrees.Normalize(M + Radians.ToDegrees(e) * Degrees.Sin(M) * (1 + e * Degrees.Cos(M)));

            double xv = Degrees.Cos(E) - e;
            double yv = Degrees.Sin(E) * Math.Sqrt(1 - e * e);

            double r = Math.Sqrt(xv * xv + yv * yv);
            double lon = Degrees.Atan2(yv, xv) + w;
            double lat = 0;

            double rasc, decl;
            ConvertEclipticToEquatorial(jday, lon, lat, out rasc, out decl);
            ConvertEquatorialToHorizontal(jday, longitude, latitude, rasc, decl, out double azimuth, out double altitude);

            // convert to unity rotation azim, altitude y then x
            return Quaternion.Euler(0f, (float)azimuth + 180.0f, 0f) * Quaternion.Euler((float)altitude, 0f, 0f);
        }

        public static void GetSunRiseSet(TimeZoneInfo tz, DateTime dt, double longitude, double latitude, out float sunRiseStart, out float sunRiseEnd, out float sunSetStart, out float sunSetEnd)
        {
            // get julian day at noon
            var utcNoon = TimeZoneInfo.ConvertTimeToUtc(new DateTime(dt.Year, dt.Month, dt.Day, 12, 0, 0, DateTimeKind.Unspecified), tz);
            double jdayNoon = GetJulianDayFromGregorianDateTime(utcNoon);
            
            float sunUpperLimb = -0.833f;
            float sunLowerLimb = 3f; // more than lower limb at horizon to look better

            // magic
            double d = jdayNoon - 2451543.5;
            double w = 282.9404 + 4.70935E-5 * d;
            double e = 0.016709 - 1.151E-9 * d;
            double M = Degrees.Normalize(356.0470 + 0.9856002585 * d);
            double E = Degrees.Normalize(M + Radians.ToDegrees(e) * Degrees.Sin(M) * (1 + e * Degrees.Cos(M)));

            double xv = Degrees.Cos(E) - e;
            double yv = Degrees.Sin(E) * Math.Sqrt(1 - e * e);
            double lon = Degrees.Atan2(yv, xv) + w;
            double lat = 0;

            double rasc, decl;
            ConvertEclipticToEquatorial(jdayNoon, lon, lat, out rasc, out decl);
            
            double Ls = Degrees.Normalize(w + M);
            double GMST = Degrees.Normalize(Ls + 180);
            double UTSunInSouth = Degrees.Normalize(rasc - GMST - longitude) / 15.0f;

            var noonUtc = new DateTime(dt.Year, dt.Month, dt.Day, 12, 0, 0, DateTimeKind.Utc);
            var offset = tz.GetUtcOffset(noonUtc);

            double cosLHA = (Degrees.Sin(sunUpperLimb) - Degrees.Sin(latitude) * Degrees.Sin(decl)) / (Degrees.Cos(latitude) * Degrees.Cos(decl));
            if (cosLHA < -1)
            {
                // never set
                sunRiseStart = 0;
                sunSetEnd = 24;
            }
            else if (cosLHA > 1)
            {
                // never rise
                sunRiseStart = 24;
                sunSetEnd = 0;
            }
            else
            {
                double LHA = Degrees.Acos(cosLHA);
                double convert = LHA / 15f;

                sunRiseStart = (float) (UTSunInSouth - convert + offset.TotalHours);
                sunSetEnd = (float) (UTSunInSouth + convert + offset.TotalHours);
            }

            cosLHA = (Degrees.Sin(sunLowerLimb) - Degrees.Sin(latitude) * Degrees.Sin(decl)) / (Degrees.Cos(latitude) * Degrees.Cos(decl));
            if (cosLHA < -1)
            {
                // never set
                sunRiseEnd = 0;
                sunSetStart = 24;
            }
            else if (cosLHA > 1)
            {
                // never rise
                sunRiseEnd = 24;
                sunSetStart = 0;
            }
            else
            {
                double LHA = Degrees.Acos(cosLHA);
                double convert = LHA / 15f;

                sunRiseEnd = (float)(UTSunInSouth - convert + offset.TotalHours);
                sunSetStart = (float)(UTSunInSouth + convert + offset.TotalHours);
            }
        }

        // Time and date calculations

        static int GetJulianDayFromGregorianDate(int year, int month, int day)
        {
            // https://en.wikipedia.org/wiki/Julian_day#Converting_Gregorian_calendar_date_to_Julian_Day_Number
            return (1461 * (year + 4800 + (month - 14) / 12)) / 4 + (367 * (month - 2 - 12 * ((month - 14) / 12))) / 12 - (3 * ((year + 4900 + (month - 14) / 12) / 100)) / 4 + day - 32075;
        }

        public static double GetJulianDayFromGregorianDateTime(DateTime dt)
        {
            int jdn = GetJulianDayFromGregorianDate(dt.Year, dt.Month, dt.Day);

            return jdn + (dt.Hour - 12) / 24.0 + dt.Minute / 1440.0 + dt.Second / 86400.0;
        }

        static double GetJulianDayFromGregorianDateTime(int year, int month, int day, double secondsFromMidnight)
        {
            int jdn = GetJulianDayFromGregorianDate(year, month, day);
            return jdn + secondsFromMidnight / 86400.0 - 0.5;
        }

        static void GetGregorianDateFromJulianDay(int julianDay, out int year, out int month, out int day)
        {
            int J = julianDay;
            int j = J + 32044;
            int g = j / 146097;
            int dg = j % 146097;
            int c = (dg / 36524 + 1) * 3 / 4;
            int dc = dg - c * 36524;
            int b = dc / 1461;
            int db = dc % 1461;
            int a = (db / 365 + 1) * 3 / 4;
            int da = db - a * 365;
            int y = g * 400 + c * 100 + b * 4 + a;
            int m = (da * 5 + 308) / 153 - 2;
            int d = da - (m + 4) * 153 / 5 + 122;

            year = y - 4800 + (m + 2) / 12;
            month = (m + 2) % 12 + 1;
            day = d + 1;
        }

        static void GetGregorianDateTimeFromJulianDay(double julianDay, out int year, out int month, out int day, out int hour, out int minute, out double second)
        {
            int ijd = (int)Math.Floor(julianDay + 0.5);
            GetGregorianDateFromJulianDay(ijd, out year, out month, out day);

            double s = (julianDay + 0.5 - ijd) * 86400.0;
            hour = (int)Math.Floor(s / 3600);
            s -= hour * 3600;
            minute = (int)Math.Floor(s / 60);
            s -= minute * 60;
            second = s;
        }

        static void GetGregorianDateFromJulianDay(double julianDay, out int year, out int month, out int day)
        {
            int hour;
            int minute;
            double second;
            GetGregorianDateTimeFromJulianDay(julianDay, out year, out month, out day, out hour, out minute, out second);
        }
        
        // Moon Calculations

        const double J2000 = 2451545.0;

        static void GetEclipticMoonPosition(double jday, out double lon, out double lat, out double dist)
        {
            double d = jday - 2451543.5;

            double N = 125.1228 - 0.0529538083 * d;   // long asc node
            double i = 5.1454;                        // inclination
            double wm = 318.0634 + 0.1643573223 * d;  // arg of perigee
            double a = 60.2666;                       // mean distance
            double e = 0.054900;                      // eccentricity

            double ws = 282.9404 + 4.70935E-5 * d;

            double Ms = Degrees.Normalize(356.0470 + 0.9856002585 * d); // Sun's mean anomaly
            double Mm = Degrees.Normalize(115.3654 + 13.0649929505 * d); // Moon's mean anomaly
            double Ls = Degrees.Normalize(ws + Ms); // Sun's mean longitude
            double Lm = Degrees.Normalize(N + wm + Mm); // Moon's mean longitude
            double D = Degrees.Normalize(Lm - Ls); // Moon's mean elongation
            double F = Degrees.Normalize(Lm - N); // Moon's argument of latitude

            double E0 = Mm + e * Degrees.Sin(Mm) * (1.0 + e * Degrees.Cos(Mm));
            double diff = 1.0;

            while (diff > 0.005)
            {
                double E1 = E0 - (E0 - e * Degrees.Sin(E0) - Mm) / (1.0 + e * Degrees.Cos(E0));
                diff = Math.Abs(E0 - E1);
                E0 = E1;
            }

            // rectangular coordinates in the plane of lunar orbit
            double x = a * (Degrees.Cos(E0) - e);
            double y = a * Math.Sqrt(1.0 - e * e) * Degrees.Sin(E0);

            // distance and true anomaly
            double r = Math.Sqrt(x * x + y * y);
            double v = Degrees.Atan2(y, x);

            // position in ecliptic coordinates
            double xe = Degrees.Cos(N) * Degrees.Cos(v + wm) - Degrees.Sin(N) * Degrees.Sin(v + wm) * Degrees.Cos(i);
            double ye = Degrees.Sin(N) * Degrees.Cos(v + wm) + Degrees.Cos(N) * Degrees.Sin(v + wm) * Degrees.Cos(i);
            double ze = Degrees.Sin(v + wm) * Degrees.Sin(i);

            double longitude = Degrees.Atan2(ye, xe);
            double latitude = Degrees.Atan2(ze, Math.Sqrt(xe * xe + ye * ye));

            lon = longitude
                - 1.274 * Degrees.Sin(Mm - 2 * D)      // Evection
                + 0.658 * Degrees.Sin(2 * D)           // Variation
                - 0.186 * Degrees.Sin(Ms)              // Yearly equation
                - 0.059 * Degrees.Sin(2 * Mm - 2 * D)
                - 0.057 * Degrees.Sin(Mm - 2 * D + Ms)
                + 0.053 * Degrees.Sin(Mm + 2 * D)
                + 0.046 * Degrees.Sin(2 * D - Ms)
                + 0.041 * Degrees.Sin(Mm - Ms)
                - 0.035 * Degrees.Sin(D)               // Parallactic equation
                - 0.031 * Degrees.Sin(Mm + Ms)
                - 0.015 * Degrees.Sin(2 * F - 2 * D)
                + 0.011 * Degrees.Sin(Mm - 4 * D);

            lat = latitude
                - 0.173 * Degrees.Sin(F - 2 * D)
                - 0.055 * Degrees.Sin(Mm - F - 2 * D)
                - 0.046 * Degrees.Sin(Mm + F - 2 * D)
                + 0.033 * Degrees.Sin(F + 2 * D)
                + 0.017 * Degrees.Sin(2 * Mm + F);

            dist = r
                - 0.58 * Degrees.Cos(Mm - 2 * D)
                - 0.46 * Degrees.Cos(2 * D);

            lon = Degrees.Normalize(lon);
            lat = Degrees.Normalize(lat);
        }

        public static Quaternion GetMoonPosition(double jday, double longitude, double latitude)
        {
            double lonecl, latecl;
            double distance; // TODO: you can use this to change moon size - as it moves closer & further from Earth
            GetEclipticMoonPosition(jday, out lonecl, out latecl, out distance);

            double rasc, decl;
            ConvertEclipticToEquatorial(jday, lonecl, latecl, out rasc, out decl);
            ConvertEquatorialToHorizontal(jday, longitude, latitude, rasc, decl, out double azimuth, out double altitude);

            return Quaternion.Euler(0f, (float)azimuth, 0f) * Quaternion.Euler((float)altitude, 0f, 0f);
        }
    }
}
