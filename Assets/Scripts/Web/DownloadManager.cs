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
        static int currentProgress;
        public static string dataPath;
        static Queue<Download> downloads = new Queue<Download>();

        public static void Init()
        {
            dataPath = Application.dataPath;
            ManageDownloads();
        }

        public static void AddDownloadToQueue(Download download)
        {
            downloads.Enqueue(download);
        }

        public static async void ManageDownloads()
        {
            while (true){
                if(downloads.Count > 0)
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
                WebClient client = new WebClient();
                Debug.Log($"Downloading {fileName}...");
                currentProgress = 0;
                if (download.onDownloadComplete != null)
                {
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(download.onDownloadComplete);
                }

                if (download.onDownloadProgressChanged != null)
                {
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(OnDownloadProgressChanged);
                }

                await client.DownloadFileTaskAsync(download.uri, download.path);
            }
            catch (Exception ex)
            {
                Debug.Log($"Download error: {ex.Message}");
            }
        }

        public static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage != currentProgress)
            {
                currentProgress = e.ProgressPercentage;
                Debug.Log($"{e.ProgressPercentage}% downloaded...");
            }
        }

        public static void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            Debug.Log("Download Complete!");
        }
    }

    public class Download{
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
