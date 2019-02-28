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
        [MessageType("lgsvl_srvs/Int")]
        public struct Int
        {
            public int data;
            public Int(int n)
            {
                data = n;
            }
        }
    }
}
