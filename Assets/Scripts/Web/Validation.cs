/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Simulator.Web
{
    public static class Validation
    {
        public static bool IsValidUrl(string url)
        {
            try
            {
                new Uri(url);
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        // TODO:
        // set responseStatus
        // if url is new:
        // if url starts with file:// and file exists set localPath, if it does not exist throw an exception
        // if url starts with http:// or https:// create a temporary file and initiate downloading
        //    when downloading is completed move file to expected location and update localPath
        // otherwise throw exception with error message
        public static bool BeValidFilePath(string url)
        {
            var uri = new Uri(url);
            if (uri.IsFile)
            {
                return File.Exists(uri.LocalPath);
            }
            else
            {
                return uri.IsWellFormedOriginalString();
            }
        }

        public static bool BeValidAssetBundle(string url)
        {
            var uri = new Uri(url);
            
            byte[] buffer = new byte[7];
            try
            {
                if (uri.IsFile && File.Exists(uri.LocalPath))
                {
                    using (var fs = new FileStream(uri.OriginalString, FileMode.Open, FileAccess.Read))
                    {
                        if (fs.Read(buffer, 0, buffer.Length) != buffer.Length)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // TODO: check remote file
                    return true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogException(ex);
                return false;
            }

            return Encoding.ASCII.GetString(buffer) == "UnityFS";
        }

    }
}
