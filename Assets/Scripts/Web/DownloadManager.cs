/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
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
using UnityEngine.Assertions;
using System.Linq;
using Simulator.Database.Services;
using Simulator.Database;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Simulator.Web
{
    public static class DownloadManager
    {
        class Download : IProgressTask
        {
            public const float rateLimit = 0.5f; // in seconds

            public Uri uri;
            public string path;
            public event Action<IProgressTask> OnUpdated = delegate { };
            public event Action<IProgressTask, bool, Exception> OnCompleted = delegate { };
            public bool valid = true;
            public long ExpectedBytes = 0;
            public long BytesReceived = 0;

            public Download(Uri uri, string path, string description)
            {
                this.uri = uri;
                this.path = path;
                Description = description;
            }

            public float Progress => ExpectedBytes > 0 ? (float)BytesReceived / ExpectedBytes : 0.0f;
            public string Description { get; set; }

            public void Cancelled()
            {
                OnCompleted(this, false, null);
            }

            public void Completed(object sender, AsyncCompletedEventArgs args)
            {
                if (args.Error != null && !cancelled)
                {
                    Debug.LogException(args.Error);
                }

                if (ExpectedBytes != BytesReceived || ExpectedBytes <= 0)
                {
                    if (!cancelled)
                        OnCompleted(this, false, new Exception($"Download incomplete, received {BytesReceived} of {ExpectedBytes}"));
                    // TODO continue partial download with range header.
                    // needs rewrite to use WebRequest here and needs range support on WISE asset service side
                }
                else
                {
                    OnCompleted(this, args.Error == null && !args.Cancelled, args.Error);
                }

                client.DownloadProgressChanged -= Update;
                client.DownloadFileCompleted -= Completed;
                OnCompleted(this, !cancelled, null);
            }

            public void Update(object sender, DownloadProgressChangedEventArgs args)
            {
                ExpectedBytes = args.TotalBytesToReceive;
                BytesReceived = args.BytesReceived;
                long now = Stopwatch.GetTimestamp();
                if (now < nextUpdate)
                {
                    return;
                }

                if (currentProgress != args.ProgressPercentage)
                {
                    currentProgress = args.ProgressPercentage;
                    OnUpdated(this);
                    nextUpdate = now + (long)(rateLimit * Stopwatch.Frequency);
                }
            }
        }

        static ConcurrentQueue<Download> downloads = new ConcurrentQueue<Download>();
        static WebClient client;
        static WebProxy proxy;
        static string currentUrl;
        static int currentProgress;
        static long nextUpdate;
        static bool cancelled;
        static bool initialized = false;

        private static void Init()
        {
            if (initialized)
            {
                return;
            }

            client = new WebClient();
            client.Headers.Add("SimId", Config.SimID);
            // Initialize proxy here
            if (!string.IsNullOrEmpty(Config.CloudProxy))
            {
                proxy = new WebProxy(new Uri(Config.CloudProxy));
                client.Proxy = proxy;
            }

            ManageDownloads();
        }

        public static IProgressTask AddDownloadToQueue(Uri uri, string path, string description)
        {
            Init();
            var download = new Download(uri, path, description);
            downloads.Enqueue(download);
            return download;
        }

        public static Task<AssetModel> GetAsset(BundleConfig.BundleTypes type, string assetGuid, string name = null,
            IProgress<Tuple<string, float>> progressCallback = null)
        {
            Assert.IsNotNull(assetGuid, $"{nameof(assetGuid)} must not be null when trying to get ${BundleConfig.singularOf(type)} asset.");

            Init();
            var assetService = new AssetService();
            var found = assetService.Get(assetGuid);
            if (found != null)
            {
                if (File.Exists(found.LocalPath))
                {
                    return Task.FromResult(found);
                }
                else
                {
                    Debug.Log($"removing stale entry from assetService due to missing file: {found.LocalPath}");
                    assetService.Delete(assetGuid);
                }
            }

            var typeString = BundleConfig.singularOf(type);

            if (name == null) name = typeString;

            string localPath = WebUtilities.GenerateLocalPath(assetGuid, type);

            Uri uri = new Uri(Config.CloudUrl + "/api/v1/assets/download/bundle/" + assetGuid);

            var progressState = new Tuple<string, float>(name, 0.0f);
            progressCallback?.Report(new Tuple<string, float>(name, 0.0f));
            Debug.Log($"{name} Download at 0%");
            var t = new TaskCompletionSource<AssetModel>();
            var download = new Download(uri, localPath, name);

            download.OnUpdated += (IProgressTask t) =>
            {
                progressCallback?.Report(new Tuple<string, float>(name, t.Progress * 100));
                Debug.Log($"{name} Download at {t.Progress * 100}%");
            };

            download.OnCompleted += (IProgressTask pt, bool success, Exception ex) =>
            {
                if (success)
                {
                    try
                    {
                        var model = new AssetModel()
                        {
                            AssetGuid = assetGuid,
                            Type = typeString,
                            Name = name,
                            LocalPath = localPath,
                            DateAdded = DateTime.UtcNow.ToString()
                        };
                        assetService.Add(model);
                        Debug.Log($"{name} Download Complete.");
                        progressCallback?.Report(new Tuple<string, float>(name, 100));
                        t.TrySetResult(model);
                    }
                    catch (Exception e)
                    {
                        t.TrySetException(e);
                    }
                }
                else if (ex != null)
                {
                    t.TrySetException(ex);
                }
            };

            downloads.Enqueue(download);
            TaskProgressManager.Instance.AddTask(download);
            return t.Task;
        }

        public static void StopAssetDownload(string assetGuid)
        {
            var uri = new Uri(Config.CloudUrl + "/api/v1/assets/download/bundle/" + assetGuid);
            StopDownload(uri.OriginalString);
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
                    Debug.LogWarning($"Cannot remove download from download queue: {url} is not in the download queue.");
                    return;
                }

                download.valid = false;
            }
        }

        static async void ManageDownloads()
        {
            initialized = true;
            while (true)
            {
                if (downloads.TryDequeue(out Download download))
                {
                    if (download.valid)
                    {
                        currentUrl = download.uri.OriginalString;
                        await DownloadFile(download);
                    }
                    else
                    {
                        download.Cancelled();
                    }
                }
                else
                {
                    await Task.Delay(1000);
                }
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
                client.DownloadProgressChanged += ValidateDownloadPreflight;
                client.DownloadProgressChanged += download.Update;
                client.DownloadFileCompleted += download.Completed;
                cancelled = false;
                await client.DownloadFileTaskAsync(download.uri, download.path);
            }
            catch (Exception ex)
            {
                if (File.Exists(download.path))
                {
                    File.Delete(download.path);
                }

                if (!(ex is WebException webException) || webException.Status != WebExceptionStatus.RequestCanceled)
                {
                    Debug.LogException(ex);
                    throw ex;
                }
            }
        }

        static void ValidateDownloadPreflight(object sender, DownloadProgressChangedEventArgs args)
        {
            if (!(client.ResponseHeaders["content-type"].StartsWith("application") || client.ResponseHeaders["content-type"].StartsWith("binary")))
            {
                StopDownload(currentUrl);
                Debug.LogError($"Failed to download: Content-Type {client.ResponseHeaders["content-type"]} not supported.");
            }

            client.DownloadProgressChanged -= ValidateDownloadPreflight;
        }
    }
}
