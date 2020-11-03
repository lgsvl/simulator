/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class DateTimeSet : ICommand
    {
        public string Name => "environment/datetime/set";

        public void Execute(JSONNode args)
        {
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;
            var api = ApiManager.Instance;

            if (env == null)
            {
                api.SendError(this, "Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            string date_time_str = args["datetime"].Value;

            string[] values = date_time_str.Split(new string[] { "-", " ", ":", "." }, StringSplitOptions.None);
            DateTime date_time;
            try
            {
                date_time = new DateTime(
                    int.Parse(values[0]),
                    int.Parse(values[1]),
                    int.Parse(values[2]),
                    int.Parse(values[3]),
                    int.Parse(values[4]),
                    int.Parse(values[5])
                );
            }
            catch (IndexOutOfRangeException)
            {
                date_time = DateTime.Now;
                string Message = String.Format("Invalid datetime received. Setting date and time to: {0}", date_time);
                api.SendError(this, Message);
            }
            env.SetDateTime(date_time);
            env.CurrentTimeOfDayCycle = args["fixed"].AsBool ? EnvironmentEffectsManager.TimeOfDayCycleTypes.Freeze : EnvironmentEffectsManager.TimeOfDayCycleTypes.Normal;

            api.SendResult(this);
        }
    }
}
