/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.ComponentModel;

namespace Simulator.Database.Services
{
    public interface IDownloadService
    {
        int GetProgress();
        void SetProgress(int progress);
        void AddDownload(Uri uri, string localPath, Action<AsyncCompletedEventArgs> onComplete, Action<int> downloadUpdate);
        void StopDownload();
    }
}
