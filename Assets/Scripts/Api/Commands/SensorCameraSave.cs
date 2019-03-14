/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using UnityEngine.PostProcessing;

namespace Api.Commands
{
    class SensorCameraSave : ICommand
    {
        public string Name { get { return "sensor/camera/save"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;

            Component sensor;
            if (ApiManager.Instance.Sensors.TryGetValue(uid, out sensor))
            {
                if (sensor is VideoToROS)
                {
                    var camera = sensor as VideoToROS;
                    var path = args["path"].Value;
                    var quality = args["quality"].AsInt;

                    var pp = camera.GetComponent<PostProcessingBehaviour>();
                    bool oldpp = false;
                    if (pp != null)
                    {
                        oldpp = pp.profile.motionBlur.enabled;
                        pp.profile.motionBlur.enabled = false;
                    }

                    bool result = camera.Save(path, quality);

                    if (pp != null)
                    {
                        pp.profile.motionBlur.enabled = oldpp;
                    }

                    ApiManager.Instance.SendResult(client, result);
                }
                else
                {
                    ApiManager.Instance.SendError(client, $"Sensor '{uid}' is not a camera sensor");
                }
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Sensor '{uid}' not found");
            }
        }
    }
}
