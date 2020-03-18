/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;

namespace Simulator.Api.Commands
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

    class SensorGpsData : SensorCommand
    {
        public override string Name => "sensor/gps/data";

        public override void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            SensorBase sensor = null;
            if (SimulatorManager.InstanceAvailable)
                sensor = SimulatorManager.Instance.Sensors.GetSensor(uid);
            if (sensor!=null)
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

                    api.SendResult(this, result);
                }
                else
                {
                    api.SendError(this, $"Sensor '{uid}' is not a GPS sensor");
                }
            }
            else
            {
                api.SendError(this, $"Sensor '{uid}' not found");
            }
        }
    }
}
