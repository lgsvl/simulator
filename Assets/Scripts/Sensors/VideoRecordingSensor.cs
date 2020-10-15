/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.IO;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Sensors.UI;
using Simulator.Utilities;
using Simulator.Plugins;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

namespace Simulator.Sensors
{
    [RequireComponent(typeof(Camera))]
    [SensorType("Video Recording", new System.Type[] { })]
    public class VideoRecordingSensor : SensorBase
    {
        [SensorParameter]
        [Range(1, 1920)]
        public int Width = 1920;

        [SensorParameter]
        [Range(1, 1080)]
        public int Height = 1080;

        [SensorParameter]
        [Range(1, 30)]
        public int Framerate = 15;

        [SensorParameter]
        [Range(1000, 6000)]
        public int Bitrate = 3000;

        [SensorParameter]
        [Range(1000, 6000)]
        public int MaxBitrate = 6000;

        [SensorParameter]
        [Range(0, 51)]
        public int Quality = 22;

        [SensorParameter]
        [Range(1.0f, 90.0f)]
        public float FieldOfView = 60.0f;

        [SensorParameter]
        [Range(0.01f, 1000.0f)]
        public float MinDistance = 0.1f;

        [SensorParameter]
        [Range(0.01f, 2000.0f)]
        public float MaxDistance = 2000.0f;

        Camera Camera;
        RenderTexture Texture;
        VideoCapture Recorder;

        string Outdir;
        string Outfile;
        bool IsInit = false;
        bool IsRecording = false;
        private float CurrentFieldOfView;

        void Init()
        {
            if (IsInit)
            {
                return;
            }

            Outdir = Path.Combine(Simulator.Web.Config.PersistentDataPath, "Videos");
            if (!Directory.Exists(Outdir))
            {
                Directory.CreateDirectory(Outdir);
            }

            Recorder = new VideoCapture();
            IsInit = Recorder.Init(OnRecordingFailed);
        }

        void OnEnable()
        {
            Init();

            Camera = GetComponent<Camera>();
            Debug.Assert(Camera != null);

            Camera.enabled = false;
            Camera.depth = -1;
            CurrentFieldOfView = FieldOfView;
            Camera.nearClipPlane = MinDistance;
            Camera.farClipPlane = MaxDistance;

            var hd = Camera.GetComponent<HDAdditionalCameraData>();
            hd.hasPersistentHistory = true;

            CheckTexture();
        }

        void OnDisable()
        {
            StopRecording();

            Texture.Release();
            Destroy(Texture);
            Texture = null;

            IsInit = false;
            Recorder = null;
        }

        void Update()
        {
            Camera.fieldOfView = FieldOfView;
            CheckTexture();
        }

        void CheckTexture()
        {
            if (Texture != null)
            {
                bool isResized = Camera.targetTexture.width != Width || Camera.targetTexture.height != Height;

                if (isResized || !Texture.IsCreated())
                {
                    Texture.Release();
                    Texture = null;
                }
            }

            if (Texture == null)
            {
                Texture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32);
                Texture.Create();
                Camera.targetTexture = Texture;

                if (Recorder != null)
                {
                    Recorder.Reset(Camera.targetTexture);
                }
            }
        }

        public bool StartRecording(string outfile = null)
        {
            if (IsInit && !IsRecording)
            {
                string destination = GetDestination(outfile);
                if (string.IsNullOrEmpty(destination))
                {
                    return false;
                }

                if (Camera.targetTexture == null)
                {
                    CheckTexture();
                }

                if (Recorder.Start(Camera, Framerate, Bitrate, MaxBitrate, Quality, 0, destination))
                {
                    Outfile = Path.GetFileName(destination);
                    Debug.Log($"Start recording a video: {Outfile}");
                    StartCoroutine(Recorder.OnRecord(Camera, Framerate, this));
                    IsRecording = true;

                    return true;
                }
            }

            return false;
        }

        public void OnRecordingFailed()
        {
            StopAllCoroutines();

            if (Recorder != null)
            {
                Recorder.Stop();
            }

            IsRecording = false;
            Debug.LogWarning($"Recording failed: {Outfile}");

            string outfile = Path.Combine(Outdir, Outfile);
            if (File.Exists(outfile))
            {
                try
                {
                    File.Delete(outfile);
                    Debug.LogWarning($"Deleted: {Outfile}");
                }
                catch
                {
                    //
                }
            }
        }

        public bool StopRecording()
        {
            if (IsInit && IsRecording)
            {
                StopAllCoroutines();

                if (Recorder != null)
                {
                    Recorder.Stop();
                }

                IsRecording = false;
                Debug.Log($"Stop recording and saved: {Outfile}");

                return true;
            }

            return false;
        }

        string GetDestination(string outfile = null)
        {
            if (string.IsNullOrEmpty(outfile))
            {
                outfile = System.Guid.NewGuid().ToString() + ".mp4";
            }
            else
            {
                var ext = Path.GetExtension(outfile).ToLower();

                if (ext != ".mp4")
                {
                    return null;
                }
            }

            string destination = Path.Combine(Outdir, outfile);

            return destination;
        }

        public string GetFileName()
        {
            return Outfile;
        }

        public string GetOutdir()
        {
            return Outdir;
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            //
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);
            if (Texture)
            {
                visualizer.UpdateRenderTexture(Texture, Camera.aspect);
            }
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }


        public override void SetAnalysisData()
        {
            if (Loader.Instance.Network.IsClient)
                Debug.LogError("Saving captured video on clients simulation is currently unsupported. The data will be lost.");
            else if (StopRecording())
            {

                SensorAnalysisData = new Hashtable
                {
                    { "VideoCapture", Path.Combine(GetOutdir(), GetFileName()) },
                };
            }
        }
    }
}
