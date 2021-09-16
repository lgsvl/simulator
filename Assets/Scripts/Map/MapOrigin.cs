/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System;
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
        Bicycle = 1 << 12,
        F1Tenth = 1 << 13,
    };

    public partial class MapOrigin : MonoBehaviour
    {
        public double OriginEasting;
        public double OriginNorthing;
        public int UTMZoneId;
        public float AltitudeOffset = 0f;

        [HideInInspector]
        public string TimeZoneSerialized;

        [HideInInspector]
        public string TimeZoneString;
        public TimeZoneInfo TimeZone => string.IsNullOrEmpty(TimeZoneSerialized) ? TimeZoneInfo.Local : TimeZoneInfo.FromSerializedString(TimeZoneSerialized);

        [HideInInspector]
        public bool IgnoreNPCVisible = false; // TODO fix this disabled for now in SpawnManager
        public bool IgnoreNPCSpawnable = false;
        public bool IgnoreNPCBounds = false;
        public bool IgnorePedBounds = false;
        [HideInInspector]
        public bool IgnorePedVisible = false; // TODO fix this disabled for now in SpawnManager
        public int NPCSizeMask = 1<<0 | 1<<1 | 1<<2 | 1<<3| 1<<4 | 1<<5 | 1<<6 | 1<<7 | 1<<8 | 1<<9 | 1<<11 | 1<<12;
        public int NPCMaxCount = 10;
        public int NPCSpawnBoundSize = 200;

        public int PedMaxCount = 10;
        public int PedSpawnBoundSize = 200;

        public string Description;

        public static MapOrigin Find()
        {
            var origin = FindObjectOfType<MapOrigin>();
            if (origin == null)
            {
                Debug.LogWarning("Map is missing MapOrigin component! Adding temporary MapOrigin. Please add to scene and set origin");
                origin = new GameObject("MapOrigin").AddComponent<MapOrigin>();
                origin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(new Vector3(0f, -90f, 0f)));
                origin.OriginEasting = 592720;
                origin.OriginNorthing = 4134479;
                origin.UTMZoneId = 10;
            }

            return origin;
        }

        public GpsLocation PositionToGpsLocation(Vector3 position, bool ignoreMapOrigin = false)
        {
            return PositionToGpsLocation((double3)(float3)position, ignoreMapOrigin);
        }

        public GpsLocation PositionToGpsLocation(double3 position, bool ignoreMapOrigin = false)
        {
            var location = new GpsLocation();

            PositionToNorthingEasting(position, out location.Northing, out location.Easting, ignoreMapOrigin);
            NorthingEastingToLatLong(location.Northing, location.Easting, out location.Latitude, out location.Longitude, ignoreMapOrigin);

            location.Altitude = position.y + AltitudeOffset;

            return location;
        }

        public Vector3 LatLongToPosition(double latitude, double longitude)
        {
            LatLongToNorthingEasting(latitude, longitude, out var northing, out var easting);
            return NorthingEastingToPosition(northing, easting);
        }

        public void PositionToNorthingEasting(double3 position, out double northing, out double easting, bool ignoreMapOrigin = false)
        {
            var mapOriginRelative = transform.InverseTransformPoint(new Vector3((float)position.x, (float)position.y, (float)position.z));

            northing = mapOriginRelative.z;
            easting = mapOriginRelative.x;

            if (!ignoreMapOrigin)
            {
                northing += OriginNorthing;
                easting += OriginEasting;
            }
        }

        public Vector3 NorthingEastingToPosition(double northing, double easting, bool ignoreMapOrigin = false)
        {
            if (!ignoreMapOrigin)
            {
                northing -= OriginNorthing;
                easting -= OriginEasting;
            }

            var worldPosition = transform.TransformPoint(new Vector3((float)easting, 0, (float)northing));

            return new Vector3(worldPosition.x, 0, worldPosition.z);
        }
    }
}
