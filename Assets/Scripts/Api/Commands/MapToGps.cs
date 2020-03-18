/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Map;
using Unity.Mathematics;

namespace Simulator.Api.Commands
{
    class MapToGps : ICommand
    {
        public string Name => "map/to_gps";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            var map = MapOrigin.Find();
            if (map == null)
            {
                api.SendError(this, "MapOrigin not found. Is the scene loaded?");
                return;
            }

            var position = args["transform"]["position"].ReadDouble3();
            var rotation = args["transform"]["rotation"].ReadDouble3();

            double northing, easting;
            map.GetNorthingEasting(position, out northing, out easting);

            double latitude, longitude;
            map.GetLatitudeLongitude(northing, easting, out latitude, out longitude);

            var result = new JSONObject();
            result.Add("latitude", new JSONNumber(latitude));
            result.Add("longitude", new JSONNumber(longitude));
            result.Add("northing", new JSONNumber(northing));
            result.Add("easting", new JSONNumber(easting));
            result.Add("altitude", new JSONNumber(position.y + map.AltitudeOffset));
            result.Add("orientation", new JSONNumber(-rotation.y));

            api.SendResult(this, result);
        }
    }

    public static class JSONNodeExtensionMethods
    {
        public static double3 ReadDouble3(this JSONNode node)
        {
            return new double3(node["x"].AsDouble, node["y"].AsDouble, node["z"].AsDouble);
        }
    }

}
