/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine.Assertions;
using System;
using System.IO;

namespace Simulator.Web
{
    public class WebUtilities
    {
        public static string GenerateLocalPath(string assetGuid, BundleConfig.BundleTypes type)
        {
            Assert.IsNotNull(assetGuid, $"{nameof(assetGuid)} must not be null when trying to get LocalPath of ${BundleConfig.singularOf(type)}.");
            string directoryPath = Path.Combine(Config.PersistentDataPath, BundleConfig.pluralOf(type));
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return Path.Combine(directoryPath, assetGuid);
        }
    }
}
