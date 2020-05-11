/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

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

    public enum NPCSizeType
    {
        Compact = 1 << 0,
        MidSize = 1 << 1,
        Luxury = 1 << 2,
        Sport = 1 << 3,
        LightTruck = 1 << 4,
        SUV = 1 << 5,
        MiniVan = 1 << 6,
        Large = 1 << 7,
        Emergency = 1 << 8,
        Bus = 1 << 9,
        Trailer = 1 << 10,
        Motorcycle = 1 << 11,
    };

    public partial class MapOrigin : MonoBehaviour
    {
        public double OriginEasting;
        public double OriginNorthing;
        public int UTMZoneId;
        public float AltitudeOffset;

        [HideInInspector]
        public string TimeZoneSerialized;

        [HideInInspector]
        public string TimeZoneString;
        public TimeZoneInfo TimeZone => string.IsNullOrEmpty(TimeZoneSerialized) ? TimeZoneInfo.Local : TimeZoneInfo.FromSerializedString(TimeZoneSerialized);

        public int NPCSizeMask = 1;
        public int NPCMaxCount = 10;
        public int NPCSpawnBoundSize = 200;

        public int PedMaxCount = 10;
        public int PedSpawnBoundSize = 200;

        public string Description;
        public string LicenseName;

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
            return GetGpsLocation((double3)(float3)position, ignoreMapOrigin);
        }

        public GpsLocation GetGpsLocation(double3 position, bool ignoreMapOrigin = false)
        {
            var location = new GpsLocation();

            GetNorthingEasting(position, out location.Northing, out location.Easting, ignoreMapOrigin);
            GetLatitudeLongitude(location.Northing, location.Easting, out location.Latitude, out location.Longitude, ignoreMapOrigin);

            location.Altitude = position.y + AltitudeOffset;

            return location;
        }

        public void GetNorthingEasting(double3 position, out double northing, out double easting, bool ignoreMapOrigin = false)
        {
            easting = position.x;
            northing = position.z;

            if (!ignoreMapOrigin)
            {
                easting += OriginEasting;
                northing += OriginNorthing;
            }
        }

        public Vector3 FromNorthingEasting(double northing, double easting, bool ignoreMapOrigin = false)
        {
            double x = easting;
            double z = northing;
            if (!ignoreMapOrigin)
            {
                x -= OriginEasting;
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
