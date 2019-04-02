/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;

namespace Api.Commands
{
    class GetVersion : ICommand
    {
        public string Name { get { return "simulator/version"; } }

        public void Execute(JSONNode args)
        {
            var result = new JSONString(BuildInfo.buildVersion);
            ApiManager.Instance.SendResult(result);
        }
    }
}
