/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Simulator.Plugins
{
    public class VideoCapture
    {
        int Id = -1;
        float TimeTillNextCapture;
        static IntPtr RenderEvent;

        string Ffmpeg;
        byte[] Buffer = null;
        Stream Pipe;
        Process Subprocess;
        Queue<AsyncGPUReadbackRequest> ReadbackQueue = new Queue<AsyncGPUReadbackRequest>();

        int RecordedFrames = 0;
        int SkippedFrames = 0;
        Action callback;  // An optional callback function to be called on recoding failure
        int MaxRequests = 10;  // Maximum number of requests for AsyncGPUReadback [MaxRequests := (Simulator FPS / Recording FPS) * frames of latency]

        delegate void LogDelegate(string message);
        static LogDelegate Log = DebugLog;

        static void DebugLog(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        [DllImport("VideoCapture", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        static extern int VideoCapture_Init(string Ffmpeg, LogDelegate log);

        [DllImport("VideoCapture", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        static extern int VideoCapture_Start(int width, int height, int framerate, int bitrate, int maxBitrate, int quality, int streaming, string destination);

        [DllImport("VideoCapture", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        static extern void VideoCapture_Reset(int id, IntPtr texture);

        [DllImport("VideoCapture", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        static extern void VideoCapture_Stop(int id);

        [DllImport("VideoCapture", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        static extern IntPtr VideoCapture_GetRenderEventFunc();

        public bool Init(Action callback_ = null)
        {
            callback = callback_;

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                if (Application.isEditor)
                {
                    Ffmpeg = Path.Combine(Application.dataPath, "Plugins", "VideoCapture", "ffmpeg", "windows", "ffmpeg.exe");
                }
                else
                {
                    Ffmpeg = Path.Combine(Application.dataPath, "Plugins", "ffmpeg.exe");
                }
            }
            else
            {
                if (Application.isEditor)
                {
                    Ffmpeg = Path.Combine(Application.dataPath, "Plugins", "VideoCapture", "ffmpeg", "linux", "ffmpeg");
                }
                else
                {
                    Ffmpeg = Path.Combine(Application.dataPath, "Plugins", "ffmpeg");
                }
            }

            if (!File.Exists(Ffmpeg))
            {
                UnityEngine.Debug.LogWarning($"Cannot find ffmpeg at '{Ffmpeg}' location.");
                return false;
            }

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                if (VideoCapture_Init(Ffmpeg, Log) == 0)
                {
                    RenderEvent = VideoCapture_GetRenderEventFunc();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public bool Start(Camera camera, int framerate, int bitrate, int maxBitrate, int quality, int streaming, string destination)
        {
            RenderTexture texture = camera.targetTexture;
            int width = texture.width;
            int height = texture.height;

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                if (Id == -1 && texture != null)
                {
                    Id = VideoCapture_Start(width, height, framerate, bitrate, maxBitrate, quality, streaming, destination);
                    VideoCapture_Reset(Id, texture.GetNativeTexturePtr());
                }

                return Id != -1;
            }
            else
            {
                string args = $"-y -loglevel error"
                    + $" -f rawvideo -vcodec rawvideo -pixel_format rgba"
                    + $" -video_size {width}x{height}"
                    + $" -framerate {framerate}"
                    + $" -i -"
                    + $" -vf vflip"
                    + $" -c:v h264_nvenc -pix_fmt yuv420p"
                    + $" -b:v {bitrate * 1024} -maxrate:v {maxBitrate * 1024}"
                    + $" -g {framerate * 2} -profile:v high"
                    + $" -rc vbr_hq -cq {quality}"
                    + $" -f mp4"
                    + $" \"{destination}\"";

                var process = new Process()
                {
                    StartInfo =
                    {
                        FileName = Ffmpeg,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                    },
                };

                process.EnableRaisingEvents = true;
                process.Exited += OnProcessExited;
                process.ErrorDataReceived += OnErrorDataReceived;

                try
                {
                    process.Start();
                    process.BeginErrorReadLine();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    return false;
                }

                Subprocess = process;
                Pipe = Subprocess.StandardInput.BaseStream;

                return true;
            }
        }

        private void OnProcessExited(object sender, EventArgs evt)
        {
            var process = sender as Process;
            if (process != null && process.ExitCode != 0)
            {
                UnityEngine.Debug.LogWarning($"FFmpeg exited with exit code: {process.ExitCode}");
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs err)
        {
            if (!String.IsNullOrEmpty(err.Data))
            {
                UnityEngine.Debug.LogWarning($"FFmpeg error: {err.Data}");
            }
        }

        public void Reset(RenderTexture texture)
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                if (Id != -1 && texture != null)
                {
                    VideoCapture_Reset(Id, texture.GetNativeTexturePtr());
                }
            }
        }

        public void Stop()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                if (Id != -1)
                {
                    VideoCapture_Stop(Id);
                    Id = -1;
                }
            }
            else
            {
                ProcessQueue(true);  // Call with the wait flag to make sure it captures all remaining frames at the end
                ReadbackQueue.Clear();

                if (Pipe != null)
                {
                    Pipe.Close();
                    Pipe = null;
                }

                if (Subprocess != null)
                {
                    Subprocess.WaitForExit();
                    Subprocess.Close();
                    Subprocess.Dispose();
                    Subprocess = null;
                }
            }
        }

        private IEnumerator OnRender()
        {
            while (true)
            {
                if (!ProcessQueue())
                {
                    yield break;
                }

                yield return new WaitForEndOfFrame();
                // Pending requests are automatically updated each frame
                // The result is accessible ONLY for a single frame once is successfully fulfilled
            }
        }

        public IEnumerator OnRecord(Camera camera, int framerate, MonoBehaviour obj)
        {
            RenderTexture texture = camera.targetTexture;

            RecordedFrames = 0;
            SkippedFrames = 0;

            if (SystemInfo.operatingSystemFamily != OperatingSystemFamily.Windows)
            {
                if (obj != null)
                {
                    obj.StartCoroutine(OnRender());
                }
            }

            while (true)
            {
                if (Time.timeScale != 0 && texture.IsCreated())
                {
                    if (TimeTillNextCapture <= 0)
                    {
                        camera.Render();

                        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
                        {
                            if (Id != -1 && RenderEvent != IntPtr.Zero)
                            {
                                GL.IssuePluginEvent(RenderEvent, Id);
                            }
                        }
                        else
                        {
                            if (ReadbackQueue.Count < MaxRequests)
                            {
                                ReadbackQueue.Enqueue(AsyncGPUReadback.Request(texture));
                            }
                            else
                            {
                                if (!ProcessQueue())  // Check and process if some requests in the queue are fulfilled already
                                {
                                    yield break;
                                }

                                if (ReadbackQueue.Count < MaxRequests)  // Proceed if there's some room in the queue
                                {
                                    ReadbackQueue.Enqueue(AsyncGPUReadback.Request(texture));
                                }
                                else
                                {
                                    UnityEngine.Debug.LogWarning($"Skipping a frame ({++SkippedFrames} / {RecordedFrames}): Decrease your framerate for Video Recording sensor (Current: {framerate} fps)");
                                }
                            }
                        }

                        TimeTillNextCapture += 1.0f / framerate;
                    }

                    TimeTillNextCapture -= Time.fixedDeltaTime;
                    // Note: Using fixedUnscaledDeltaTime here seems more appropriate
                    // But, it sometimes gives very large value (e.g., 13) which is incorrect (might be a bug)
                }

                yield return new WaitForFixedUpdate();
            }
        }

        private bool ProcessQueue(bool wait = false)
        {
            while (ReadbackQueue.Count > 0)
            {
                var req = ReadbackQueue.Peek();

                if (wait)
                {
                    req.WaitForCompletion();  // This stalls both GPU and CPU resuling in a performance hit
                }

                if (req.done)  // Query req.done each frame
                {
                    req = ReadbackQueue.Dequeue();
                    if (req.hasError)  // A request that has been disposed of will result in req.hasError being true
                    {
                        UnityEngine.Debug.LogWarning($"Skipping a frame ({++SkippedFrames} / {RecordedFrames}): A frame has been disposed of");
                    }
                    else
                    {
                        var data = req.GetData<byte>();
                        if (Buffer == null || Buffer.Length != data.Length)
                        {
                            Buffer = data.ToArray();
                        }
                        else
                        {
                            data.CopyTo(Buffer);  // Re-use bufffer if available
                        }

                        try
                        {
                            Pipe.Write(Buffer, 0, Buffer.Length);
                            Pipe.Flush();
                            RecordedFrames++;
                        }
                        catch  // Something went wrong in ffmpeg process
                        {
                            if (callback != null)
                            {
                                callback();
                            }

                            return false;
                        }
                    }
                }
                else  // AsyncGPUReadback has a few frames of latency
                {
                    break;
                }
            }

            return true;
        }
    }
}
