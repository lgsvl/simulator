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

namespace Api.Commands
{
    class VehicleGetSensors : ICommand
    {
        public string Name { get { return "vehicle/get_sensors"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                List<Component> sensors;

                var setup = obj.GetComponent<AgentSetup>();
                if (setup != null)
                {
                    sensors = setup.GetSensors();
                }
                else
                {
                    ApiManager.Instance.SendError($"Agent '{uid}' does not know its sensors");
                    return;
                }

                JSONArray result = new JSONArray();
                for (int i = 0; i < sensors.Count; i++)
                {
                    var sensor = sensors[i];

                    JSONObject j = null;

                    if (sensor is VideoToROS)
                    {
                        var camera = sensor as VideoToROS;
                        var unityCamera = camera.GetComponent<Camera>();

                        j = new JSONObject();
                        j.Add("type", "camera");
                        j.Add("name", camera.sensorName);
                        j.Add("width", unityCamera.pixelWidth);
                        j.Add("height", unityCamera.pixelHeight);
                        j.Add("fov", unityCamera.fieldOfView);
                        j.Add("near_plane", unityCamera.nearClipPlane);
                        j.Add("far_plane", unityCamera.farClipPlane);
                        if (camera.captureType == VideoToROS.CaptureType.Capture)
                        {
                            j.Add("format", "RGB");
                        }
                        else if (camera.captureType == VideoToROS.CaptureType.Depth)
                        {
                            j.Add("format", "DEPTH");
                        }
                        else if (camera.captureType == VideoToROS.CaptureType.Segmentation)
                        {
                            j.Add("format", "SEMANTIC");
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    else if (sensor is LidarSensor)
                    {
                        var lidar = sensor as LidarSensor;

                        j = new JSONObject();
                        j.Add("type", "lidar");
                        j.Add("name", lidar.FrameName);
                        j.Add("min_distance", lidar.MinDistance);
                        j.Add("max_distance", lidar.MaxDistance);
                        j.Add("rays", lidar.RayCount);
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
                        j.Add("name", imu.SensorName);
                    }

                    if (j != null)
                    {
                        j.Add("uid", ApiManager.Instance.SensorUID[sensor]);
                        result[result.Count] = j;
                    }
                }

                ApiManager.Instance.SendResult(result);
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
