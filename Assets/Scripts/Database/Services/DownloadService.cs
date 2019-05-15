/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Database.Services;
using System;
using System.ComponentModel;
using Web;

namespace Assets.Scripts.Database.Services
{
    public class DownloadService : IDownloadService
    {
        public int GetProgress() => DownloadManager.currentPercentage;
        public void SetProgress(int progress) => DownloadManager.currentPercentage = progress;
        public void AddDownload(Uri uri, string localPath, Action<AsyncCompletedEventArgs> onComplete, Action<int> update) => DownloadManager.AddDownloadToQueue(uri, localPath, onComplete, update);
        public void StopDownload() => DownloadManager.StopDownload();
    }
}