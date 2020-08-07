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
                List<SensorBase> sensors = obj.GetComponentsInChildren<SensorBase>(true).ToList();

                JSONArray result = new JSONArray();
                for (int i = 0; i < sensors.Count; i++)
                {
                    var sensor = sensors[i];

                    JSONObject j = null;

                    if (sensor is ColorCameraSensor colorCameraSensor)
                    {
                        j = new JSONObject();
                        j.Add("type", "camera");
                        j.Add("name", colorCameraSensor.Name);
                        j.Add("frequency", colorCameraSensor.Frequency);
                        j.Add("width", colorCameraSensor.Width);
                        j.Add("height", colorCameraSensor.Height);
                        j.Add("fov", colorCameraSensor.FieldOfView);
                        j.Add("near_plane", colorCameraSensor.MinDistance);
                        j.Add("far_plane", colorCameraSensor.MaxDistance);
                        j.Add("format", "RGB");
                    }
                    else if (sensor is DepthCameraSensor depthCameraSensor)
                    {
                        j = new JSONObject();
                        j.Add("type", "camera");
                        j.Add("name", depthCameraSensor.Name);
                        j.Add("frequency", depthCameraSensor.Frequency);
                        j.Add("width", depthCameraSensor.Width);
                        j.Add("height", depthCameraSensor.Height);
                        j.Add("fov", depthCameraSensor.FieldOfView);
                        j.Add("near_plane", depthCameraSensor.MinDistance);
                        j.Add("far_plane", depthCameraSensor.MaxDistance);
                        j.Add("format", "DEPTH");
                    }
                    else if (sensor is SegmentationCameraSensor segmentationCameraSensor)
                    {
                        j = new JSONObject();
                        j.Add("type", "camera");
                        j.Add("name", segmentationCameraSensor.Name);
                        j.Add("frequency", segmentationCameraSensor.Frequency);
                        j.Add("width", segmentationCameraSensor.Width);
                        j.Add("height", segmentationCameraSensor.Height);
                        j.Add("fov", segmentationCameraSensor.FieldOfView);
                        j.Add("near_plane", segmentationCameraSensor.MinDistance);
                        j.Add("far_plane", segmentationCameraSensor.MaxDistance);
                        j.Add("format", "SEGMENTATION");
                    }
                    else if (sensor is LidarSensor lidar)
                    {
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
                    else if (sensor is ImuSensor imu)
                    {
                        j = new JSONObject();
                        j.Add("type", "imu");
                        j.Add("name", imu.Name);
                    }
                    else if (sensor is GpsSensor gps)
                    {
                        j = new JSONObject();
                        j.Add("type", "gps");
                        j.Add("name", gps.Name);
                        j.Add("frequency", new JSONNumber(gps.Frequency));
                    }
                    else if (sensor is RadarSensor radar)
                    {
                        j = new JSONObject();
                        j.Add("type", "radar");
                        j.Add("name", radar.Name);
                    }
                    else if (sensor is CanBusSensor canbus)
                    {
                        j = new JSONObject();
                        j.Add("type", "canbus");
                        j.Add("name", canbus.Name);
                        j.Add("frequency", new JSONNumber(canbus.Frequency));
                    }
                    else if (sensor is VideoRecordingSensor recorder)
                    {
                        j = new JSONObject();
                        j.Add("type", "recorder");
                        j.Add("name", recorder.Name);
                        j.Add("width", new JSONNumber(recorder.Width));
                        j.Add("height", new JSONNumber(recorder.Height));
                        j.Add("framerate", new JSONNumber(recorder.Framerate));
                    }
                    else if (sensor is AnalysisSensor analysis)
                    {
                        j = new JSONObject();
                        j.Add("type", "analysis");
                        j.Add("name", analysis.Name);
                        j.Add("suddenbrakemax", new JSONNumber(analysis.SuddenBrakeMax));
                        j.Add("suddensteermax", new JSONNumber(analysis.SuddenSteerMax));
                        j.Add("stucktravelthreshold", new JSONNumber(analysis.StuckTravelThreshold));
                        j.Add("stucktimethreshold", new JSONNumber(analysis.StuckTimeThreshold));
                        j.Add("minfps", new JSONNumber(analysis.MinFPS));
                        j.Add("minfpstime", new JSONNumber(analysis.MinFPSTime));
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
