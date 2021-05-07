/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class VehicleApplyControl : ICommand
    {
        public string Name => "vehicle/apply_control";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var sticky = args["sticky"].AsBool;
                var control = args["control"];

                var vc = obj.GetComponent<IAgentController>();
                var va = obj.GetComponent<IVehicleActions>();
                var vd = obj.GetComponent<IVehicleDynamics>();

                if (vc == null)
                {
                    api.SendError(this, $"{nameof(IAgentController)} component not found in agent '{uid}'.");
                    return;
                }

                if (va == null)
                {
                    api.SendError(this, $"{nameof(IVehicleActions)} component not found in agent '{uid}'.");
                    return;
                }
                
                if (vd == null)
                {
                    api.SendError(this, $"{nameof(IVehicleDynamics)} component not found in agent '{uid}'.");
                    return;
                }

                var steering = control["steering"].AsFloat;
                var throttle = control["throttle"].AsFloat;
                var braking = control["braking"].AsFloat;

                vc.ApplyControl(sticky, steering, throttle - braking);

                var reverse = control["reverse"].AsBool;

                if (reverse)
                    vd.ShiftReverse();
                else
                    vd.ShiftFirstGear();

                var handbrake = args["control"]["handbrake"].AsBool;
                vd.SetHandBrake(handbrake);

                if (args["control"]["headlights"] != null)
                {
                    int headlights = args["control"]["headlights"].AsInt;
                    va.CurrentHeadLightState = (HeadLightState)headlights;
                }

                if (args["control"]["windshield_wipers"] != null)
                {
                    int state = args["control"]["windshield_wipers"].AsInt;
                    va.CurrentWiperState = (WiperState)state;
                }

                if (args["control"]["turn_signal_left"] != null)
                {
                    bool on = args["control"]["turn_signal_left"].AsBool;
                    va.LeftTurnSignal = on;
                }

                if (args["control"]["turn_signal_right"] != null)
                {
                    bool on = args["control"]["turn_signal_right"].AsBool;
                    va.RightTurnSignal = on;
                }

                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
