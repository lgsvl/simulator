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
                List<SensorBase> sensors = obj.GetComponentsInChildren<SensorBase>().ToList();

                JSONArray result = new JSONArray();
                for (int i = 0; i < sensors.Count; i++)
                {
                    var sensor = sensors[i];

                    JSONObject j = null;

                    if (sensor is ColorCameraSensor)
                    {
                        var camera = sensor as ColorCameraSensor;
                        var unityCamera = camera.GetComponent<Camera>();

                        j = new JSONObject();
                        j.Add("type", "camera");
                        j.Add("name", camera.Name);
                        j.Add("frequency", camera.Frequency);
                        j.Add("width", camera.Width);
                        j.Add("height", camera.Height);
                        j.Add("fov", camera.FieldOfView);
                        j.Add("near_plane", camera.MinDistance);
                        j.Add("far_plane", camera.MaxDistance);
                        j.Add("format", "RGB");
                    }
                    else if (sensor is DepthCameraSensor)
                    {
                        var camera = sensor as DepthCameraSensor;

                        j = new JSONObject();
                        j.Add("type", "camera");
                        j.Add("name", camera.Name);
                        j.Add("frequency", camera.Frequency);
                        j.Add("width", camera.Width);
                        j.Add("height", camera.Height);
                        j.Add("fov", camera.FieldOfView);
                        j.Add("near_plane", camera.MinDistance);
                        j.Add("far_plane", camera.MaxDistance);
                        j.Add("format", "DEPTH");
                    }
                    else if (sensor is SemanticCameraSensor)
                    {
                        var camera = sensor as SemanticCameraSensor;
                        var unityCamera = camera.GetComponent<Camera>();

                        j = new JSONObject();
                        j.Add("type", "camera");
                        j.Add("name", camera.Name);
                        j.Add("frequency", camera.Frequency);
                        j.Add("width", camera.Width);
                        j.Add("height", camera.Height);
                        j.Add("fov", camera.FieldOfView);
                        j.Add("near_plane", camera.MinDistance);
                        j.Add("far_plane", camera.MaxDistance);
                        j.Add("format", "SEMANTIC");
                    }
                    else if (sensor is InstanceSegmentationSensor)
                    {
                        var camera = sensor as InstanceSegmentationSensor;
                        var unityCamera = camera.GetComponent<Camera>();

                        j = new JSONObject();
                        j.Add("type", "camera");
                        j.Add("name", camera.Name);
                        j.Add("frequency", camera.Frequency);
                        j.Add("width", camera.Width);
                        j.Add("height", camera.Height);
                        j.Add("fov", camera.FieldOfView);
                        j.Add("near_plane", camera.MinDistance);
                        j.Add("far_plane", camera.MaxDistance);
                        j.Add("format", "INSTANCESEGMENTATION");
                    }
                    else if (sensor is LidarSensor)
                    {
                        var lidar = sensor as LidarSensor;

                        j = new JSONObject();
                        j.Add("type", "lidar");
                        j.Add("name", lidar.Name);
                        j.Add("min_distance", lidar.MinDistance);
                        j.Add("max_distance", lidar.MaxDistance);
                        j.Add("rays", lidar.LaserCount);
                        j.Add("rotations", lidar.RotationFrequency);
                        j.Add("measurements", lidar.MeasurementsPerRotation);
                        j.Add("fov", lidar.FieldOfView);
                        j.Add("angle", lidar.CenterAngle);
                        j.Add("compensated", lidar.Compensated);
                    }
                    else if (sensor is ImuSensor)
                    {
                        var imu = sensor as ImuSensor;

                        j = new JSONObject();
                        j.Add("type", "imu");
                        j.Add("name", imu.Name);
                    }
                    else if (sensor is GpsSensor)
                    {
                        var gps = sensor as GpsSensor;

                        j = new JSONObject();
                        j.Add("type", "gps");
                        j.Add("name", gps.Name);
                        j.Add("frequency", new JSONNumber(gps.Frequency));
                    }
                    else if (sensor is RadarSensor)
                    {
                       var radar = sensor as RadarSensor;

                       j = new JSONObject();
                       j.Add("type", "radar");
                       j.Add("name", radar.Name);
                    }
                    else if (sensor is CanBusSensor)
                    {
                        var canbus = sensor as CanBusSensor;

                        j = new JSONObject();
                        j.Add("type", "canbus");
                        j.Add("name", canbus.Name);
                        j.Add("frequency", new JSONNumber(canbus.Frequency));
                    }

                    if (j != null)
                    {
                        j.Add("uid", api.SensorUID[sensor]);
                        result[result.Count] = j;
                    }
                }

                api.SendResult(result);
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
