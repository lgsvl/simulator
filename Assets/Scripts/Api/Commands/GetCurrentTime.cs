/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;

namespace Api.Commands
{
    class GetCurrentTime : ICommand
    {
        public string Name { get { return "simulator/current_time"; } }

        public void Execute(JSONNode args)
        {
            var result = new JSONNumber(ApiManager.Instance.CurrentTime);
            ApiManager.Instance.SendResult(result);
        }
    }
}
