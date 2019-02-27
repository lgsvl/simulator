/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    namespace Srv
    {
        [MessageType("std_srvs/SetBool")]
        public struct SetBool
        {
            public bool data;
            public SetBool(bool d)
            {
                data = d;
            }
        }

        [MessageType("std_srvs/SetBool")]
        public struct SetBoolResponse
        {
            public bool success;
            public string message;
            public SetBoolResponse(bool d, string m)
            {
                success = d;
                message = m;
            }
        }
    }
}
