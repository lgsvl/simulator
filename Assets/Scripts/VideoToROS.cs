/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


#define USE_COMPRESSED

using UnityEngine;
using System.Threading.Tasks;

[RequireComponent(typeof(Camera))]
public class VideoToROS : MonoBehaviour, Ros.IRosClient
{
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

    Ros.Bridge Bridge;
    bool ImageIsBeingSent;

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

        if (captureType == CaptureType.Segmentation)
        {
            SegmentColorer segColorer = FindObjectOfType<SegmentColorer>();
            if (segColorer != null)
            {
                renderCam.SetReplacementShader(segColorer.Shader, "SegmentColor"); // TODO needs to be local ref or manager?
                renderCam.backgroundColor = segColorer.SkyColor; // TODO needs to be local ref or manager?
                renderCam.clearFlags = CameraClearFlags.SolidColor;
                renderCam.renderingPath = RenderingPath.Forward;
            }
        }
        Reader = new AsyncTextureReader<byte>(renderCam.targetTexture);

        GetComponentInParent<CameraSettingsManager>().AddCamera(renderCam);

        // TODO better way
        if (sensorName == "Main Camera")
            GetComponentInParent<RobotSetup>().MainCam = renderCam;

        addUIElement();
    }

    void OnDestroy()
    {
        if (Reader != null)
        {
            Reader.Destroy();
        }
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        ImageIsBeingSent = false;
#if USE_COMPRESSED
        Bridge.AddPublisher<Ros.CompressedImage>(TopicName);
#else
        Bridge.AddPublisher<Ros.Image>(TopicName);
#endif
    }

    public void FPSChangeCallback(int value)
    {
        sendingFPS = value;
    }

    void Update()
    {
        if (Bridge == null || Bridge.Status != Ros.Status.Connected)
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
            SendImage(data.ToArray(), data.Length);
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
        if (Bridge == null || Bridge.Status != Ros.Status.Connected)
        {
            return;
        }

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
        Bridge.Publish(TopicName, msg, () => ImageIsBeingSent = false);
    }

    private void addUIElement()
    {
        var cameraCheckbox = GetComponentInParent<UserInterfaceTweakables>().AddCheckbox(sensorName, $"Toggle {sensorName}:", init);
        var cameraPreview = GetComponentInParent<UserInterfaceTweakables>().AddCameraPreview(sensorName, $"Toggle {sensorName}", renderCam);
        cameraCheckbox.onValueChanged.AddListener(x => 
        {
            renderCam.enabled = x;
            enabled = x;
            cameraPreview.gameObject.SetActive(x);
        });
    }
}
