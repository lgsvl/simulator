/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Simulator.Sensors;
using System.Reflection;
using Simulator.Utilities;

namespace Simulator.Api.Commands
{
    class VehicleGetSensors : ICommand
    {
        public string Name => "vehicle/sensors/get";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                List<SensorBase> sensors = obj.GetComponentsInChildren<SensorBase>(true).ToList();

                JSONArray result = new JSONArray();
                for (int i = 0; i < sensors.Count; i++)
                {
                    var sensor = sensors[i];
                    var sensorType = sensor.GetType().GetCustomAttribute<SensorType>();
                    JSONObject j = null;
                    switch (sensorType.Name)
                    {
                        case "Color Camera":
                            j = new JSONObject();
                            j.Add("type", "camera");
                            j.Add("name", sensor.Name);
                            j.Add("frequency", (int)sensor.GetType().GetField("Frequency").GetValue(sensor));
                            j.Add("width", (int)sensor.GetType().GetField("Width").GetValue(sensor));
                            j.Add("height", (int)sensor.GetType().GetField("Height").GetValue(sensor));
                            j.Add("fov", (float)sensor.GetType().GetField("FieldOfView").GetValue(sensor));
                            j.Add("near_plane", (float)sensor.GetType().GetField("MinDistance").GetValue(sensor));
                            j.Add("far_plane", (float)sensor.GetType().GetField("MaxDistance").GetValue(sensor));
                            j.Add("format", "RGB");
                            break;
                        case "Depth Camera":
                            j = new JSONObject();
                            j.Add("type", "camera");
                            j.Add("name", sensorType.Name);
                            j.Add("frequency", (int)sensor.GetType().GetField("Frequency").GetValue(sensor));
                            j.Add("width", (int)sensor.GetType().GetField("Width").GetValue(sensor));
                            j.Add("height", (int)sensor.GetType().GetField("Height").GetValue(sensor));
                            j.Add("fov", (float)sensor.GetType().GetField("FieldOfView").GetValue(sensor));
                            j.Add("near_plane", (float)sensor.GetType().GetField("MinDistance").GetValue(sensor));
                            j.Add("far_plane", (float)sensor.GetType().GetField("MaxDistance").GetValue(sensor));
                            j.Add("format", "DEPTH");
                            break;
                        case "Segmentation Camera":
                            j = new JSONObject();
                            j.Add("type", "camera");
                            j.Add("name", sensorType.Name);
                            j.Add("frequency", (int)sensor.GetType().GetField("Frequency").GetValue(sensor));
                            j.Add("width", (int)sensor.GetType().GetField("Width").GetValue(sensor));
                            j.Add("height", (int)sensor.GetType().GetField("Height").GetValue(sensor));
                            j.Add("fov", (float)sensor.GetType().GetField("FieldOfView").GetValue(sensor));
                            j.Add("near_plane", (float)sensor.GetType().GetField("MinDistance").GetValue(sensor));
                            j.Add("far_plane", (float)sensor.GetType().GetField("MaxDistance").GetValue(sensor));
                            j.Add("format", "SEGMENTATION");
                            break;
                        case "Lidar":
                            j = new JSONObject();
                            j.Add("type", "lidar");
                            j.Add("name", sensorType.Name);
                            j.Add("min_distance", (float)sensor.GetType().GetField("MinDistance").GetValue(sensor));
                            j.Add("max_distance", (float)sensor.GetType().GetField("MaxDistance").GetValue(sensor));
                            j.Add("rays", (int)sensor.GetType().GetField("LaserCount").GetValue(sensor));
                            j.Add("rotations", (float)sensor.GetType().GetField("RotationFrequency").GetValue(sensor));
                            j.Add("measurements", (int)sensor.GetType().GetField("MeasurementsPerRotation").GetValue(sensor));
                            j.Add("fov", (float)sensor.GetType().GetField("FieldOfView").GetValue(sensor));
                            j.Add("angle", (float)sensor.GetType().GetField("CenterAngle").GetValue(sensor));
                            j.Add("compensated", (bool)sensor.GetType().GetField("Compensated").GetValue(sensor));
                            break;
                        case "IMU":
                            j = new JSONObject();
                            j.Add("type", "imu");
                            j.Add("name", sensorType.Name);
                            break;
                        case "GPS":
                            j = new JSONObject();
                            j.Add("type", "gps");
                            j.Add("name", sensor.Name);
                            j.Add("frequency", (int)sensor.GetType().GetField("Frequency").GetValue(sensor));
                            break;
                        case "Radar":
                            j = new JSONObject();
                            j.Add("type", "radar");
                            j.Add("name", sensor.Name);
                            break;
                        case "CanBus":
                            j = new JSONObject();
                            j.Add("type", "canbus");
                            j.Add("name", sensor.Name);
                            j.Add("frequency", (int)sensor.GetType().GetField("Frequency").GetValue(sensor));
                            break;
                        case "Video Recording":
                            j = new JSONObject();
                            j.Add("type", "camera");
                            j.Add("name", sensor.Name);
                            j.Add("width", (int)sensor.GetType().GetField("Width").GetValue(sensor));
                            j.Add("height", (int)sensor.GetType().GetField("Height").GetValue(sensor));
                            j.Add("framerate", (int)sensor.GetType().GetField("Framerate").GetValue(sensor));
                            j.Add("fov", (float)sensor.GetType().GetField("FieldOfView").GetValue(sensor));
                            j.Add("near_plane", (float)sensor.GetType().GetField("MinDistance").GetValue(sensor));
                            j.Add("far_plane", (float)sensor.GetType().GetField("MaxDistance").GetValue(sensor));
                            j.Add("bitrate", (int)sensor.GetType().GetField("Bitrate").GetValue(sensor));
                            j.Add("max_bitrate", (int)sensor.GetType().GetField("MaxBitrate").GetValue(sensor));
                            j.Add("quality", (int)sensor.GetType().GetField("Quality").GetValue(sensor));
                            break;
                        case "Analysis":
                            j = new JSONObject();
                            j.Add("type", "analysis");
                            j.Add("name", sensor.Name);
                            j.Add("stucktravelthreshold", (float)sensor.GetType().GetField("StuckTravelThreshold").GetValue(sensor));
                            j.Add("stucktimethreshold", (float)sensor.GetType().GetField("StuckTimeThreshold").GetValue(sensor));
                            j.Add("stoplinethreshold", (float)sensor.GetType().GetField("StopLineThreshold").GetValue(sensor));
                break;
            }

            if (j != null)
            {
                if (SimulatorManager.InstanceAvailable)
                    j.Add("uid", SimulatorManager.Instance.Sensors.GetSensorUid(sensor));
                result.Add(j);
            }
        }

                api.SendResult(this, result);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
