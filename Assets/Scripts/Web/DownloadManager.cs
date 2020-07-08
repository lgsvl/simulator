/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;
using System.Linq;
using Simulator.Database.Services;
using Simulator.Database;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Simulator.Web
{
    public static class DownloadManager
    {
        class Download
        {
            public const float rateLimit = 1; // in seconds

            public Uri uri;
            public string path;
            public Action<int> update;
            public Action<bool, Exception> completed;
            public bool valid = true;

            public Download(Uri uri, string path, Action<int> update, Action<bool, Exception> completed)
            {
                this.uri = uri;
                this.path = path;
                this.update = update;
                this.completed = completed;
            }

            public void Completed(object sender, AsyncCompletedEventArgs args)
            {
                if (args.Error != null && !cancelled)
                {
                    Debug.LogException(args.Error);
                }

                completed?.Invoke(args.Error == null && !args.Cancelled, args.Error);

                client.DownloadProgressChanged -= Update;
                client.DownloadFileCompleted -= Completed;
            }

            public void Update(object sender, DownloadProgressChangedEventArgs args)
            {
                long now = Stopwatch.GetTimestamp();
                if (now < nextUpdate)
                {
                    return;
                }

                if (currentProgress != args.ProgressPercentage)
                {
                    currentProgress = args.ProgressPercentage;
                    update?.Invoke(args.ProgressPercentage);
                    nextUpdate = now + (long)(rateLimit * Stopwatch.Frequency);
                }
            }
        }

        static ConcurrentQueue<Download> downloads = new ConcurrentQueue<Download>();
        static WebClient client;
        static string currentUrl;
        static int currentProgress;
        static long nextUpdate;
        static bool cancelled;
        static bool initialized = false;

        private static void Init()
        {
            if(initialized) return;
            client = new WebClient();
            client.Headers.Add("SimId", Config.SimID);
            ManageDownloads();
        }

        public static void AddDownloadToQueue(Uri uri, string path, Action<int> update = null, Action<bool, Exception> completed = null)
        {
            Init();
            downloads.Enqueue(new Download(uri, path, update, completed));
        }

        public static Task<AssetModel> GetAsset(BundleConfig.BundleTypes type, string assetGuid, string name = null)
        {
            Init();
            var assetService = new AssetService();
            var found = assetService.Get(assetGuid);
            if(found != null) {
                return Task.FromResult(found);
            }

            var typeString = BundleConfig.singularOf(type);

            if (name == null) name = typeString;

            string localPath = WebUtilities.GenerateLocalPath(assetGuid, type);

            Uri uri = new Uri(Config.CloudUrl + "/api/v1/assets/download/bundle/" + assetGuid);

            var t = new TaskCompletionSource<AssetModel>();
            downloads.Enqueue(new Download(uri, localPath,
            progress => {
                ConnectionUI.instance?.UpdateDownloadProgress(name, progress);
                Debug.Log($"{name} Download at {progress}%");
            } ,
            (success, ex) => {
                if (success)
                {
                    var model = new AssetModel()
                    {
                        AssetGuid = assetGuid,
                        Type = typeString,
                        Name = name,
                        LocalPath = localPath
                    };
                    assetService.Add(model);
                    t.TrySetResult(model);
                }
                else
                {
                    t.TrySetException(ex);
                }
            }));
            return t.Task;
        }

        public static void StopDownload(string url)
        {
            if (url == currentUrl)
            {
                cancelled = true;
                client.CancelAsync();
            }
            else
            {
                Download download = downloads.FirstOrDefault(d => d.uri.OriginalString == url);
                if (download == null)
                {
                    throw new Exception($"Cannot remove download from download queue: {url} is not in the download queue.");
                }

                download.valid = false;
            }
        }

        static async void ManageDownloads()
        {
            initialized = true;
            while (true)
            {
                Download download;
                if (downloads.TryDequeue(out download) && download.valid)
                {
                    currentUrl = download.uri.OriginalString;
                    await DownloadFile(download);
                }

                await Task.Delay(1000);
            }
        }

        static async Task DownloadFile(Download download)
        {
            try
            {
                var fileName = Path.GetFileName(download.uri.AbsolutePath);
                Debug.Log($"Downloading {download.uri.AbsoluteUri}");

                currentProgress = 0;
                nextUpdate = 0;
                client.DownloadProgressChanged += ValidateDownload;
                client.DownloadProgressChanged += download.Update;
                client.DownloadFileCompleted += download.Completed;
                cancelled = false;
                await client.DownloadFileTaskAsync(download.uri, download.path);
            }
            catch
            {
                if (File.Exists(download.path))
                {
                    File.Delete(download.path);
                }
            }
        }

        static void ValidateDownload(object sender, DownloadProgressChangedEventArgs args)
        {
            if (!(client.ResponseHeaders["content-type"].StartsWith("application") || client.ResponseHeaders["content-type"].StartsWith("binary")))
            {
                StopDownload(currentUrl);
                Debug.LogError($"Failed to download: Content-Type {client.ResponseHeaders["content-type"]} not supported.");
            }

            client.DownloadProgressChanged -= ValidateDownload;
        }
    }
}
