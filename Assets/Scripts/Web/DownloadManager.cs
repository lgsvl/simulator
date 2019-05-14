/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace Web
{
    public static class DownloadManager
    {
        public static int currentPercentage;
        static Queue<Download> downloads = new Queue<Download>();
        static WebClient currentClient;

        public static void Init()
        {
            ManageDownloads();
            currentClient = new WebClient();
        }

        public static void AddDownloadToQueue(Uri uri, string path, Action<AsyncCompletedEventArgs> onDownloadComplete, Action<int> onDownloadProgressChanged)
        {
            Download download = new Download(uri, path, (o, e) => onDownloadComplete(e), (o, e) => onDownloadProgressChanged(e.ProgressPercentage));
            downloads.Enqueue(download);
        }

        public static void StopDownload()
        {
            currentClient.CancelAsync();
        }

        static async void ManageDownloads()
        {
            while (true)
            {
                if (downloads.Count > 0)
                {
                    Download d = downloads.Dequeue();
                    await DownloadFile(d);
                }

                await Task.Delay(1000);
            }
        }

        async static Task DownloadFile(Download download)
        {
            try
            {
                string fileName = Path.GetFileName(download.uri.AbsolutePath);
                Debug.Log($"Downloading {fileName}...");
                currentPercentage = 0;
                if (download.onDownloadComplete != null)
                {
                    currentClient.DownloadFileCompleted += download.onDownloadComplete;
                }

                if (download.onDownloadProgressChanged != null)
                {
                    currentClient.DownloadProgressChanged += download.onDownloadProgressChanged;
                }

                currentClient.DownloadProgressChanged += ValidateDownload;

                await currentClient.DownloadFileTaskAsync(download.uri, download.path);
            }
            catch (Exception ex)
            {
                if (File.Exists(download.path))
                {
                    File.Delete(download.path);
                }

                Debug.Log($"Download error: {ex.Message}");
            }
        }

        static void ValidateDownload(object sender, DownloadProgressChangedEventArgs e)
        {
            if (!currentClient.ResponseHeaders["content-type"].StartsWith("application"))
            {
                StopDownload();
            }

            currentClient.DownloadProgressChanged -= ValidateDownload;
        }
    }

    public class Download
    {
        public Uri uri;
        public string path;
        public AsyncCompletedEventHandler onDownloadComplete;
        public DownloadProgressChangedEventHandler onDownloadProgressChanged;

        public Download(Uri uri, string path, AsyncCompletedEventHandler onDownloadComplete = null, DownloadProgressChangedEventHandler onDownloadProgressChanged = null)
        {
            this.uri = uri;
            this.path = path;
            this.onDownloadComplete = onDownloadComplete;
            this.onDownloadProgressChanged = onDownloadProgressChanged;
        }
    }
}