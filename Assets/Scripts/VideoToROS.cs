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
    public GameObject Robot;
    public string TopicName;

    uint seqId;

    AsyncTextureReader Reader;

    private int initWidth;
    private int initHeight;

    private int videoWidth;
    private int videoHeight;    
    public System.ValueTuple<int, int> videoResolution
    {
        get
        {
            return new System.ValueTuple<int, int>(videoWidth, videoHeight);
        }
    }

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

    void Start()
    {
    }

    public void Init()
    {
        renderCam = GetComponent<Camera>();

        initWidth = renderCam.targetTexture.width;
        initHeight = renderCam.targetTexture.height;

        print(initWidth + "x" +initHeight);

        SwitchResolution(initWidth, initHeight);

        // need better way to distinguish type of camera
        // cannot access by asking robot
        if (renderCam.name == "ColorSegmentCamera")
        {
            var segmentColorer = FindObjectOfType<SegmentColorer>();
            segmentColorer.ApplyToCamera(renderCam);
        }
        Robot.GetComponent<CameraSettingsManager>().AddCamera(renderCam);
        addUIElement();
        print("camera init finished");
    }

    public void SwitchResolution()
    {
        videoWidth = initWidth;
        videoHeight = initHeight;
        renderCam.targetTexture.Release();
        renderCam.targetTexture = new RenderTexture(videoWidth, videoHeight, renderCam.targetTexture.depth, renderCam.targetTexture.format, RenderTextureReadWrite.Default);
        Reader = new AsyncTextureReader(renderCam.targetTexture);
    }

    public void SwitchResolution(int width, int height)
    {
        videoWidth = width;
        videoHeight = height;
        renderCam.targetTexture.Release();
        renderCam.targetTexture = new RenderTexture(videoWidth, videoHeight, renderCam.targetTexture.depth, renderCam.targetTexture.format, RenderTextureReadWrite.Default);
        Reader = new AsyncTextureReader(renderCam.targetTexture);
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

        if (Reader.Status == AsyncTextureReader.ReadStatus.Finished)
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

        if (Reader.Status != AsyncTextureReader.ReadStatus.Reading && !ImageIsBeingSent)
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
        string gameObjectName = "ToggleCamera";
        string toggleLabel = "Enable Camera: ";
        var cameraCheckbox = Robot.GetComponent<UserInterfaceTweakables>().AddCheckbox(gameObjectName, toggleLabel, init);
        var cameraPreview = Robot.GetComponent<UserInterfaceTweakables>().AddCameraPreview("camera", "ToggleCamera", renderCam);
        cameraCheckbox.onValueChanged.AddListener(x => 
        {
            renderCam.enabled = x;
            enabled = x;
            cameraPreview.gameObject.SetActive(x);
        });
    }
}
