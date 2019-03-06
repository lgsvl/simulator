/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;

namespace Api.Commands
{
    class VehicleApplyControl : ICommand
    {
        public string Name { get { return "vehicle/apply_control"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;
            var sticky = args["sticky"].AsBool;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var control = args["control"];

                var vc = obj.GetComponent<VehicleController>();

                var steering = control["steering"].AsFloat;
                var throttle = control["throttle"].AsFloat;
                var breaking = control["breaking"].AsFloat;

                vc.ApplyStickyControl(steering, throttle - breaking);

                var reverse = control["reverse"].AsBool;
                if (reverse)
                {
                    while (!vc.InReverse)
                    {
                        vc.GearboxShiftDown();
                    }
                }
                else
                {
                    while (vc.InReverse)
                    {
                        vc.GearboxShiftUp();
                    }
                }

                var handbrake = args["control"]["handbrake"].AsBool;
                vc.SetHandBrake(handbrake);

                if (args["control"]["headlights"] != null)
                {
                    int headlights = args["control"]["headlights"].AsInt;
                    vc.ForceHeadlightsOff();
                    for (int i=0; i<headlights; i++)
                    {
                        vc.ChangeHeadlightMode();
                    }
                }

                if (args["control"]["windshield_wipers"] != null)
                {
                    int state = args["control"]["windshield_wipers"].AsInt;
                    if (state == 0)
                    {
                        vc.SetWindshiledWiperLevelOff();
                    }
                    else if (state == 1)
                    {
                        vc.SetWindshiledWiperLevelLow();
                    }
                    else if (state == 2)
                    {
                        vc.SetWindshiledWiperLevelMid();
                    }
                    else if (state == 3)
                    {
                        vc.SetWindshiledWiperLevelHigh();
                    }
                }

                if (args["control"]["turn_signal_right"] != null)
                {
                    bool on = args["control"]["turn_signal_right"].AsBool;
                    if (on)
                    {
                        vc.EnableLeftTurnSignal();
                    }
                    else
                    {
                        vc.DisbleTurnSignals();
                    }
                }

                if (args["control"]["turn_signal_right"] != null)
                {
                    bool on = args["control"]["turn_signal_right"].AsBool;
                    if (on)
                    {
                        vc.EnableRightTurnSignal();
                    }
                    else
                    {
                        vc.DisbleTurnSignals();
                    }
                }

                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
