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
        [MessageType("lgsvl_srvs/String")]
        public struct String
        {
            public string str;
            public String(string s)
            {
                str = s;
            }
        }
    }
}
