/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


#define USE_COMPRESSED

using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;


[RequireComponent(typeof(Camera))]
public class VideoToROS : MonoBehaviour, Comm.BridgeClient
{
    public ROSTargetEnvironment TargetEnvironment;
    private bool init = false;

    const string FrameId = "camera"; // used by Autoware

    public string TopicName;
    public string sensorName = "Camera";

    public enum CaptureType
    {
        Capture,
        Segmentation,
        Depth
    };
    public CaptureType captureType = CaptureType.Capture;

    public enum ResolutionType
    {
        SD,
        HD
    };
    public ResolutionType resolutionType = ResolutionType.HD;
    private RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;
    private RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.sRGB;
    private int videoWidth = 1920;
    private int videoHeight = 1080;
    private int rtDepth = 24;

    uint seqId;

    AsyncTextureReader<byte> Reader;
    
    private byte[] jpegArray = new byte[1024 * 1024];

    private Camera renderCam;
    public int sendingFPS = 15;
    public int JpegQuality = 75;
    public bool manual;
    private float lastTimePoint;

    Comm.Bridge Bridge;
    #if USE_COMPRESSED
        Comm.Writer<Ros.CompressedImage> VideoWriter;
        Comm.Writer<Apollo.Drivers.CompressedImage> CyberVideoWriter;
    #else
        Comm.Writer<Ros.Image> VideoWriter;
    #endif
    bool ImageIsBeingSent;

    [System.NonSerialized]
    public RenderTextureDisplayer cameraPreview;

    private void Awake()
    {   
        if (!init)
        {
            Init();
            init = true;
        }
    }

    public void Init()
    {
        switch (captureType)
        {
            case CaptureType.Capture:
                rtFormat = RenderTextureFormat.ARGB32;
                rtReadWrite = RenderTextureReadWrite.sRGB;
                rtDepth = 24;
                break;
            case CaptureType.Segmentation:
                rtFormat = RenderTextureFormat.ARGB32;
                rtReadWrite = RenderTextureReadWrite.sRGB;
                rtDepth = 24;
                break;
            case CaptureType.Depth:
                rtFormat = RenderTextureFormat.ARGB32;
                rtReadWrite = RenderTextureReadWrite.Linear;
                rtDepth = 24;
                break;
            default:
                break;
        }

        switch (resolutionType)
        {
            case ResolutionType.SD:
                videoWidth = 640;
                videoHeight = 480;
                break;
            case ResolutionType.HD:
                videoWidth = 1920;
                videoHeight = 1080;
                break;
            default:
                break;
        }

        RenderTexture activeRT = new RenderTexture(videoWidth, videoHeight, rtDepth, rtFormat, rtReadWrite)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            antiAliasing = 1,
            useMipMap = false,
            useDynamicScale = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        activeRT.name = captureType.ToString() + resolutionType.ToString();
        activeRT.Create();

        renderCam = GetComponent<Camera>();
        renderCam.targetTexture = activeRT;

        Reader = new AsyncTextureReader<byte>(renderCam.targetTexture);

        GetComponentInParent<CameraSettingsManager>().AddCamera(renderCam);

        // TODO better way
        if (sensorName == "Main Camera")
            GetComponentInParent<AgentSetup>().MainCam = renderCam;

        addUIElement();
    }

    public void InitSegmentation(Shader shader, Color color)
    {
        if (captureType == CaptureType.Segmentation)
        {
            if (SegmentationManager.Instance != null)
            {
                renderCam.SetReplacementShader(shader, "SegmentColor"); // TODO needs to be local ref or manager?
                renderCam.backgroundColor = color; // TODO needs to be local ref or manager?
                renderCam.clearFlags = CameraClearFlags.SolidColor;
                renderCam.renderingPath = RenderingPath.Forward;
            }
        }
    }

    void OnDestroy()
    {
        if (Reader != null)
        {
            Reader.Destroy();
        }
    }

    public void GetSensors(List<Component> sensors)
    {
        sensors.Add(this);
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            ImageIsBeingSent = false;
#if USE_COMPRESSED
<<<<<<< HEAD
            if (TargetEnvironment == ROSTargetEnvironment.APOLLO35)
            {
                // TODO
            }
            else
            {
                VideoWriter = Bridge.AddWriter<Ros.CompressedImage>(TopicName);
            }
#else
            if (TargetEnvironment == ROSTargetEnvironment.APOLLO35)
            {
                // TODO
            }
            else
            {
                VideoWriter = Bridge.AddWriter<Ros.Image>(TopicName);
            }
=======
        if (TargetEnvironment == ROSTargetEnvironment.APOLLO35)
        {
            CyberVideoWriter = Bridge.AddWriter<Apollo.Drivers.CompressedImage>(TopicName);
        }
        else
        {
            VideoWriter = Bridge.AddWriter<Ros.CompressedImage>(TopicName);
        }
#else
        if (TargetEnvironment == ROSTargetEnvironment.APOLLO35)
        {
            // TODO
        }    
        else
        {
            VideoWriter = Bridge.AddWriter<Ros.Image>(TopicName);
        }
>>>>>>> 4c270eca... Modify sensors for apollo 3.5 cyber bridge
#endif
        };
    }
    public void FPSChangeCallback(int value)
    {
        sendingFPS = value;
    }

    void OnDisable()
    {
        Reader.Reset();
    }

    void Update()
    {
        if (Bridge == null || Bridge.Status != Comm.BridgeStatus.Connected)
        {
            return;
        }

        Reader.Update();

        if (Reader.Status == AsyncTextureReaderStatus.Finished)
        {
            var data = Reader.GetData();
#if USE_COMPRESSED
            Task.Run(() =>
            {
                lock (jpegArray)
                {
                    int length = JpegEncoder.Encode(data, videoWidth, videoHeight, Reader.BytesPerPixel, JpegQuality, jpegArray);
                    if (length > 0)
                    {
                        SendImage(jpegArray, length);
                    }
                }
            });
#else
            SendImage(data, data.Length);
#endif
        }

        if (Reader.Status != AsyncTextureReaderStatus.Reading && !ImageIsBeingSent)
        {
            if (manual)
            {
                if (Input.GetKeyDown(KeyCode.M))
                {
                    Reader.Start();
                }
            }
            else
            {
                if (Time.time - lastTimePoint > 1.0f / sendingFPS)
                {
                    lastTimePoint = Time.time;
                    Reader.Start();
                }
            }
        }
    }

    void SendImage(byte[] data, int length)
    {
        if (Bridge == null || Bridge.Status != Comm.BridgeStatus.Connected)
        {
            return;
        }

        if (TargetEnvironment == ROSTargetEnvironment.APOLLO35)
        {
            System.DateTime Unixepoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            double measurement_time = (double)(System.DateTime.UtcNow - Unixepoch).TotalSeconds;

#if USE_COMPRESSED
            var msg = new Apollo.Drivers.CompressedImage()
            {
                Header = new Apollo.Common.Header()
                {
                    TimestampSec = measurement_time,
                    Version = 1,
                    Status = new Apollo.Common.StatusPb()
                    {
                        ErrorCode = Apollo.Common.ErrorCode.Ok,
                    },
                },
                MeasurementTime = measurement_time,
                FrameId = FrameId,
                // Format = "png",
                Format = "jpg",

                Data = ByteString.CopyFrom(data, 0, length),

            };
#else
            // TODO

#endif

            ImageIsBeingSent = true;
            CyberVideoWriter.Publish(msg, () => ImageIsBeingSent = false);        
        }

        else 
        {            

#if USE_COMPRESSED
            var msg = new Ros.CompressedImage()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = seqId++,
                    frame_id = FrameId,
                },
                format = "jpeg",
                data = new Ros.PartialByteArray()
                {
                    Array = data,
                    Length = length,
                }
            };
#else
            byte[] temp = new byte[videoWidth * Reader.BytesPerPixel];
            int stride = videoWidth * Reader.BytesPerPixel;
            for (int y = 0; y < videoHeight / 2; y++)
            {
                int row1 = stride * y;
                int row2 = stride * (videoHeight - 1 - y);
                System.Array.Copy(data, row1, temp, 0, stride);
                System.Array.Copy(data, row2, data, row1, stride);
                System.Array.Copy(temp, 0, data, row2, stride);
            }
            var msg = new Ros.Image()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = seqId++,
                    frame_id = FrameId,
                },
                height = (uint)videoHeight,
                width = (uint)videoWidth,
                encoding = Reader.BytesPerPixel == 3 ? "rgb8" : "rgba8",
                is_bigendian = 0,
                step = (uint)stride,
                data = data,
            };
#endif
            ImageIsBeingSent = true;
            VideoWriter.Publish(msg, () => ImageIsBeingSent = false);            
            }
    }

    public bool Save(string path, int quality, int compression)
    {
        renderCam.Render();

        Reader.Start();
        Reader.WaitForCompletion();

        var data = Reader.GetData();

        var bytes = new byte[16 * 1024 * 1024];
        int length;

        var ext = System.IO.Path.GetExtension(path).ToLower();

        if (ext == ".png")
        {
            length = PngEncoder.Encode(data, videoWidth, videoHeight, Reader.BytesPerPixel, compression, bytes);
        }
        else if (ext == ".jpeg" || ext == ".jpg")
        {
            length = JpegEncoder.Encode(data, videoWidth, videoHeight, Reader.BytesPerPixel, quality, bytes);
        }
        else
        {
            return false;
        }

        if (length > 0)
        {
            try
            {
                using (var file = System.IO.File.Create(path))
                {
                    file.Write(bytes, 0, length);
                }
                return true;
            }
            catch
            {
            }
        }
        return false;
    }

    private void addUIElement()
    {
        var cameraCheckbox = GetComponentInParent<UserInterfaceTweakables>().AddCheckbox(sensorName, $"Toggle {sensorName}:", init);
        cameraPreview = GetComponentInParent<UserInterfaceTweakables>().AddCameraPreview(sensorName, $"Toggle {sensorName}", renderCam);
        cameraCheckbox.onValueChanged.AddListener(x => 
        {
            renderCam.enabled = x;
            enabled = x;
            cameraPreview.gameObject.SetActive(x);
        });
    }
}
