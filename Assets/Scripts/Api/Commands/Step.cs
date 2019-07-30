/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class Step : ICommand
    {
        public string Name => "simulator/step";

        public void Execute(JSONNode args)
        {
          var api = ApiManager.Instance;
          var frames = args["frames"].AsInt;
          var framerate = args["framerate"].AsFloat;
          
          api.TargetFrameRate = framerate;

          api.NonRealtime = true;
                    
          api.FrameLimit = api.CurrentFrame + frames;
          Time.timeScale = 1.0f;
        }
    }
}
