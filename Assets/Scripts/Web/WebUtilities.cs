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
            string directoryPath = Path.Combine(Config.PersistentDataPath, header);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return Path.Combine(directoryPath, Guid.NewGuid().ToString());
        }
    }
}
