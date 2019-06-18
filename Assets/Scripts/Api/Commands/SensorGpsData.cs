/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;

namespace Api.Commands
{
    public struct GpsData
    {
        public double Latitude;
        public double Longitude;
        public double Northing;
        public double Easting;
        public double Altitude;
        public double Orientation;
    }

    class SensorGpsData : ICommand
    {
        public string Name { get { return "sensor/gps/data"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = SimulatorManager.Instance.ApiManager;

            if (api.Sensors.TryGetValue(uid, out Component sensor))
            {
                if (sensor is GpsSensor)
                {
                    var gps = sensor as GpsSensor;

                    var data = gps.GetData();

                    var result = new JSONObject();
                    result.Add("latitude", data.Latitude);
                    result.Add("longitude", data.Longitude);
                    result.Add("northing", data.Northing);
                    result.Add("easting", data.Easting);
                    result.Add("altitude", data.Altitude);
                    result.Add("orientation", data.Orientation);

                    api.SendResult(result);
                }
                else
                {
                    api.SendError($"Sensor '{uid}' is not a GPS sensor");
                }
            }
            else
            {
                api.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
