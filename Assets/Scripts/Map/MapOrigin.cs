/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEditor;
using System;

namespace Simulator.Map
{
    public struct GpsLocation
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double Northing;
        public double Easting;
    }

    public partial class MapOrigin : MonoBehaviour
    {
        public float OriginEasting;
        public float OriginNorthing;
        public int UTMZoneId;
        public float AltitudeOffset;

        [HideInInspector]
        public string TimeZoneSerialized;

        [HideInInspector]
        public string TimeZoneString;

        public TimeZoneInfo TimeZone => string.IsNullOrEmpty(TimeZoneSerialized) ? TimeZoneInfo.Local : TimeZoneInfo.FromSerializedString(TimeZoneSerialized);

        public static MapOrigin Find()
        {
            var origin = FindObjectOfType<MapOrigin>();
            if (origin == null)
            {
                Debug.LogError("Map is missing MapOrigin component! Adding temporary MapOrigin. Please add to scene and set origin");
                origin = new GameObject("MapOrigin").AddComponent<MapOrigin>();
            }

            return origin;
        }

        public GpsLocation GetGpsLocation(Vector3 position, bool ignoreMapOrigin = false)
        {
            var location = new GpsLocation();

            GetNorthingEasting(position, out location.Northing, out location.Easting, ignoreMapOrigin);
            GetLatitudeLongitude(location.Northing, location.Easting, out location.Latitude, out location.Longitude, ignoreMapOrigin);

            location.Altitude = position.y + AltitudeOffset;

            return location;
        }

        public void GetNorthingEasting(Vector3 position, out double northing, out double easting, bool ignoreMapOrigin = false)
        {
            easting = position.x;
            northing = position.z;

            if (!ignoreMapOrigin)
            {
                easting += OriginEasting - 500000;
                northing += OriginNorthing;
            }
        }

        public Vector3 FromNorthingEasting(double northing, double easting, bool ignoreMapOrigin = false)
        {
            double x = easting;
            double z = northing;
            if (!ignoreMapOrigin)
            {
                x -= OriginEasting - 500000;
                z -= OriginNorthing;
            }

            return new Vector3((float)x, 0, (float)z);
        }        

        public static int GetZoneNumberFromLatLon(double latitude, double longitude)
        {
            int zoneNumber = (int)(Math.Floor((longitude + 180)/6) + 1);
            if (latitude >= 56.0 && latitude < 64.0 && longitude >= 3.0 && longitude < 12.0)
            {
                zoneNumber = 32;
            }

            // Special Zones for Svalbard
            if (latitude >= 72.0 && latitude < 84.0)
            {
                if (longitude >= 0.0 && longitude < 9.0)
                {
                    zoneNumber = 31;
                }
                else if (longitude >= 9.0 && longitude < 21.0)
                {
                    zoneNumber = 33;
                }
                else if (longitude >= 21.0 && longitude < 33.0)
                {
                    zoneNumber = 35;
                }
                else if (longitude >= 33.0 && longitude < 42.0)
                {
                    zoneNumber = 37;
                }
            }

            return zoneNumber;
        }
    }
}
