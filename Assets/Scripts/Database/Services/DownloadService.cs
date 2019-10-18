/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using Simulator.Web;

namespace Simulator.Database.Services
{
    public class DownloadService : IDownloadService
    {
        public void AddDownload(Uri uri, string localPath, Action<int> update, Action<bool, Exception> completed)
            => DownloadManager.AddDownloadToQueue(uri, localPath, update, completed);

        public void StopDownload(string url) => DownloadManager.StopDownload(url);
    }
}