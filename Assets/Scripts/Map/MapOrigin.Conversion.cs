/**
 *
    MIT License

    Copyright (c) 2012-2017 Tobias Bieniek <Tobias.Bieniek@gmx.de>

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
 *
 */

using System;
using UnityEngine;

namespace Simulator.Map
{
    public partial class MapOrigin : MonoBehaviour
    {
        double R = 6378137;

        const double K0 = 0.9996;

        const double E = 0.00669438;
        const double E2 = E * E;
        const double E3 = E2 * E;
        const double E_P2 = E / (1.0 - E);

        static readonly double SQRT_E = Math.Sqrt(1 - E);
        static readonly double _E = (1 - SQRT_E) / (1 + SQRT_E);
        static readonly double _E2 = _E * _E;
        static readonly double _E3 = _E2 * _E;
        static readonly double _E4 = _E3 * _E;
        static readonly double _E5 = _E4 * _E;

        static readonly double M1 = 1 - E / 4 - 3 * E2 / 64 - 5 * E3 / 256;
        static readonly double M2 = 3 * E / 8 + 3 * E2 / 32 + 45 * E3 / 1024;
        static readonly double M3 = 15 * E2 / 256 + 45 * E3 / 1024;
        static readonly double M4 = 35 * E3 / 3072;

        static readonly double P2 = 3.0 / 2 * _E - 27.0 / 32 * _E3 + 269.0 / 512 * _E5;
        static readonly double P3 = 21.0 / 16 * _E2 - 55.0 / 32 * _E4;
        static readonly double P4 = 151.0 / 96 * _E3 - 417.0 / 128 * _E5;
        static readonly double P5 = 1097.0 / 512 * _E4;

        //Expects Easting values where the central meridian is 500000
        public void GetLatitudeLongitude(double northing, double easting, out double latitude, out double longitude, bool ignoreMapOrigin = false)
        {
            double x = easting - 500000d;
            double y = northing;

            double m = y / K0;
            double mu = m / (R * M1);

            double p_rad = mu +
                     P2 * Math.Sin(2 * mu) +
                     P3 * Math.Sin(4 * mu) +
                     P4 * Math.Sin(6 * mu) +
                     P5 * Math.Sin(8 * mu);

            double p_sin = Math.Sin(p_rad);
            double p_sin2 = p_sin * p_sin;

            double p_cos = Math.Cos(p_rad);

            double p_tan = p_sin / p_cos;
            double p_tan2 = p_tan * p_tan;
            double p_tan4 = p_tan2 * p_tan2;

            double ep_sin = 1 - E * p_sin2;
            double ep_sin_sqrt = Math.Sqrt(1 - E * p_sin2);

            double n = R / ep_sin_sqrt;
            double r = (1 - E) / ep_sin;

            double c = _E * p_cos * p_cos;
            double c2 = c * c;

            double d = x / (n * K0);
            double d2 = d * d;
            double d3 = d2 * d;
            double d4 = d3 * d;
            double d5 = d4 * d;
            double d6 = d5 * d;

            double lat = p_rad - (p_tan / r) *
                (d2 / 2 -
                d4 / 24 * (5 + 3 * p_tan2 + 10 * c - 4 * c2 - 9 * E_P2)) +
                d6 / 720 * (61 + 90 * p_tan2 + 298 * c + 45 * p_tan4 - 252 * E_P2 - 3 * c2);

            double lon = (d -
                d3 / 6 * (1 + 2 * p_tan2 + c) +
                d5 / 120 * (5 - 2 * c + 28 * p_tan2 - 3 * c2 + 8 * E_P2 + 24 * p_tan4)) / p_cos;

            latitude = lat * 180.0 / Math.PI;
            longitude = lon * 180.0 / Math.PI;

            if (!ignoreMapOrigin && UTMZoneId > 0)
            {
                longitude += (UTMZoneId - 1) * 6 - 180 + 3;
            }
        }

        //Returns Easting where the central meridian is 500000
        public void FromLatitudeLongitude(double latitude, double longitude, out double northing, out double easting, bool ignoreMapOrigin = false)
        {
            double lat_rad = latitude * Math.PI / 180.0;
            double lat_sin = Math.Sin(lat_rad);
            double lat_cos = Math.Cos(lat_rad);

            double lat_tan = lat_sin / lat_cos;
            double lat_tan2 = lat_tan * lat_tan;
            double lat_tan4 = lat_tan2 * lat_tan2;

            double lon_rad = longitude * Math.PI / 180.0;
            double central_lon = (UTMZoneId - 1) * 6 - 180 + 3;
            double central_lon_rad = ignoreMapOrigin ? 0 : central_lon * Math.PI / 180.0;

            double n = R / Math.Sqrt(1 - E * lat_sin * lat_sin);
            double c = E_P2 * lat_cos * lat_cos;

            double a = lat_cos * (lon_rad - central_lon_rad);
            double a2 = a * a;
            double a3 = a2 * a;
            double a4 = a3 * a;
            double a5 = a4 * a;
            double a6 = a5 * a;

            double m = R * (M1 * lat_rad -
                M2 * Math.Sin(2 * lat_rad) +
                M3 * Math.Sin(4 * lat_rad) -
                M4 * Math.Sin(6 * lat_rad));

            easting = K0 * n * (a +
                a3 / 6 * (1 - lat_tan2 + c) +
                a5 / 120 * (5 - 18 * lat_tan2 + lat_tan4 + 72 * c - 58 * E_P2));
            easting += 500000;

            northing = K0 * (m + n * lat_tan * (a2 / 2 +
                a4 / 24 * (5 - lat_tan2 + 9 * c + 4 * c * c) +
                a6 / 720 * (61 - 58 * lat_tan2 + lat_tan4 + 600 * c - 330 * E_P2)));
        }
    }
}
