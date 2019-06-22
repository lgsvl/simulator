/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;

namespace Simulator.Web
{
    public class WebUtilities
    {
        public static string GenerateLocalPath(string header)
        {
            return Path.Combine(Config.PersistentDataPath, header, Guid.NewGuid().ToString());
        }
    }
}
