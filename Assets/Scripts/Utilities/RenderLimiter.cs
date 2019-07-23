/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Utilities
{
    public static class RenderLimiter
    {
        public static void RenderLimitEnabled()
        {
            // loader
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        public static void RenderLimitDisabled()
        {
            // simulator
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
    }
}
