/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using Simulator.Map;

namespace Api.Commands
{
    class MapToGps : ICommand
    {
        public string Name { get { return "map/to_gps"; } }

        public void Execute(JSONNode args)
        {
            var map = GameObject.Find("MapOrigin")?.GetComponent<MapOrigin>();
            var api = SimulatorManager.Instance.ApiManager;

            if (map == null)
            {
                api.SendError("MapOrigin not found. Is the scene loaded?");
                return;
            }

            var position = args["transform"]["position"].ReadVector3();
            var rotation = args["transform"]["rotation"].ReadVector3();

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
            // TODO removed, needed?
            //result.Add("orientation", new JSONNumber(-rotation.y - map.Angle));

            api.SendResult(result);
        }
    }
}
