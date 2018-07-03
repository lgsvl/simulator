/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    public interface IRosClient
    {
        void OnRosBridgeAvailable(Ros.Bridge bridge);
        void OnRosConnected();
    }
}
