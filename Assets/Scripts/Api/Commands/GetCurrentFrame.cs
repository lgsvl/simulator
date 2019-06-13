/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;

namespace Api.Commands
{
    class GetCurrentFrame : ICommand
    {
        public string Name { get { return "simulator/current_frame"; } }

        public void Execute(JSONNode args)
        {
            var result = new JSONNumber(ApiManager.Instance.CurrentFrame);
            ApiManager.Instance.SendResult(result);
        }
    }
}
