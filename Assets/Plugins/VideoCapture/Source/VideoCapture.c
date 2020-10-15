/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

#define COBJMACROS
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <synchapi.h>

#include "IUnityGraphics.h"
#include "IUnityGraphicsD3D11.h"
#include "nvEncodeAPI.h"

#include <intrin.h>
#include <stdarg.h>
#include <stdint.h>

#include "VideoCaptureFlip.cs.h"

static IUnityInterfaces* UnityInterfaces;
static IUnityGraphics* UnityGraphics;

static LARGE_INTEGER Frequency;

static ID3D11Device* Device;
static ID3D11DeviceContext* Context;
static ID3D11ComputeShader* FlipShader;

static SRWLOCK SrwLock;
static HMODULE NvEncodeModule;
static NV_ENCODE_API_FUNCTION_LIST NvEncodeApi;

typedef void VideoCapture_Log(const char* message);

static VideoCapture_Log* LogProc;
static char PathToFFMPEG[MAX_PATH];

typedef struct
{
    void* encoder;

    ID3D11UnorderedAccessView* uaView;
    ID3D11ShaderResourceView* srView;

    void* input;
    void* output;

    uint32_t width;
    uint32_t height;
    uint64_t timestamp;
    uint32_t frame;

    uint32_t framerate;
    LARGE_INTEGER previous;

    HANDLE process;
    HANDLE write;

} VideoCapture;

static VideoCapture Captures[2];

static void Log(const char* msg, ...)
{
    char buffer[1024];

    va_list args;
    va_start(args, msg);
    wvsprintfA(buffer, msg, args);
    va_end(args);

    if (LogProc == NULL)
    {
        OutputDebugStringA(buffer);
        OutputDebugStringA("\n");
    }
    else
    {
        LogProc(buffer);
    }
}

UNITY_INTERFACE_EXPORT int VideoCapture_Init(const char* ffmpeg, VideoCapture_Log* logProc)
{
    wsprintfA(PathToFFMPEG, "%s", ffmpeg);
    LogProc = logProc;

    if (NvEncodeModule == NULL)
    {
        InitializeSRWLock(&SrwLock);

        NvEncodeModule = LoadLibraryA("nvEncodeAPI64.dll");
        if (NvEncodeModule == NULL)
        {
            Log("NVENC library file is not found. Please ensure NV driver is installed");
            return -1;
        }

        typedef NVENCSTATUS NVENCAPI NvEncodeAPIGetMaxSupportedVersionProc(uint32_t * version);
        NvEncodeAPIGetMaxSupportedVersionProc* NvEncodeAPIGetMaxSupportedVersion = (NvEncodeAPIGetMaxSupportedVersionProc*)GetProcAddress(NvEncodeModule, "NvEncodeAPIGetMaxSupportedVersion");
        if (NvEncodeAPIGetMaxSupportedVersion == NULL)
        {
            Log("NvEncodeAPIGetMaxSupportedVersion entry point not. Please upgrade Nvidia driver");
            FreeModule(NvEncodeModule);
            NvEncodeModule = NULL;
            return -1;
        }

        typedef NVENCSTATUS NVENCAPI NvEncodeAPICreateInstanceProc(NV_ENCODE_API_FUNCTION_LIST * list);
        NvEncodeAPICreateInstanceProc* NvEncodeAPICreateInstance = (NvEncodeAPICreateInstanceProc*)GetProcAddress(NvEncodeModule, "NvEncodeAPICreateInstance");
        if (NvEncodeAPICreateInstance == NULL)
        {
            Log("NvEncodeAPICreateInstance entry point not. Please upgrade Nvidia driver");
            FreeModule(NvEncodeModule);
            NvEncodeModule = NULL;
            return -1;
        }

        uint32_t version = 0;
        NVENCSTATUS err = NvEncodeAPIGetMaxSupportedVersion(&version);
        if (err != NV_ENC_SUCCESS)
        {
            Log("NvEncodeAPIGetMaxSupportedVersion failed");
            FreeModule(NvEncodeModule);
            NvEncodeModule = NULL;
            return -1;
        }

        uint32_t currentVersion = (NVENCAPI_MAJOR_VERSION << 4) | NVENCAPI_MINOR_VERSION;
        if (currentVersion > version)
        {
            Log("Current driver version does not support this NvEncodeAPI version, please upgrade driver");
            FreeModule(NvEncodeModule);
            NvEncodeModule = NULL;
            return -1;
        }

        NvEncodeApi.version = NV_ENCODE_API_FUNCTION_LIST_VER;
        err = NvEncodeAPICreateInstance(&NvEncodeApi);
        if (err != NV_ENC_SUCCESS)
        {
            Log("NvEncodeAPICreateInstance failed");
            FreeModule(NvEncodeModule);
            NvEncodeModule = NULL;
            return -1;
        }
    }

    return 0;
}

UNITY_INTERFACE_EXPORT int VideoCapture_Start(int width, int height, int framerate, int bitrate, int maxBitrate, int quality, int streaming, const char* destination)
{
    int id;
    VideoCapture* capture = NULL;

    // find free hw encoder
    for (id = 0; id < sizeof(Captures)/sizeof(*Captures); id++)
    {
        if (Captures[id].encoder == NULL)
        {
            capture = Captures + id;
            break;
        }
    }
    if (capture == NULL)
    {
        return -1;
    }

    void* encoder = NULL;
    ID3D11Texture2D* texture = NULL;
    ID3D11UnorderedAccessView* uaView = NULL;
    void* input = 0;
    void* output = 0;
    HANDLE read = NULL;
    HANDLE write = NULL;
    HANDLE process = NULL;

    NVENCSTATUS err;
    HRESULT hr;
    BOOL ok;

    int result = -1;

    // create encoding session
    {
        NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS params =
        {
            .version = NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS_VER,
            .device = Device,
            .deviceType = NV_ENC_DEVICE_TYPE_DIRECTX,
            .apiVersion = NVENCAPI_VERSION,
        };

        AcquireSRWLockExclusive(&SrwLock);
        err = NvEncodeApi.nvEncOpenEncodeSessionEx(&params, &encoder);
        ReleaseSRWLockExclusive(&SrwLock);
        if (err != NV_ENC_SUCCESS)
        {
            Log("nvEncOpenEncodeSessionEx failed, error = %u", err);
            goto bail;
        }
    }

    // select encoder configuration
    {
        NV_ENC_PRESET_CONFIG preset =
        {
            .version = NV_ENC_PRESET_CONFIG_VER,
            .presetCfg =
            {
                .version = NV_ENC_CONFIG_VER,
            },
        };

        GUID encodeGUID = NV_ENC_CODEC_H264_GUID;
        GUID presetGUID = streaming ? NV_ENC_PRESET_LOW_LATENCY_HQ_GUID : NV_ENC_PRESET_HQ_GUID;

        err = NvEncodeApi.nvEncGetEncodePresetConfig(encoder, encodeGUID, presetGUID, &preset);
        if (err != NV_ENC_SUCCESS)
        {
            Log("nvEncGetEncodePresetConfig failed, error = %u", err);
            goto bail;
        }

        NV_ENC_INITIALIZE_PARAMS params =
        {
            .version = NV_ENC_INITIALIZE_PARAMS_VER,
            .encodeGUID = encodeGUID,
            .presetGUID = presetGUID,
            .encodeWidth = width,
            .encodeHeight = height,
            .darWidth = width,
            .darHeight = height,
            .frameRateNum = framerate,
            .frameRateDen = 1,
            // .enableEncodeAsync = 1,
            .enablePTD = 1,
            .encodeConfig = &preset.presetCfg,
        };

        NV_ENC_CONFIG* config = params.encodeConfig;
        config->profileGUID = streaming ? NV_ENC_H264_PROFILE_MAIN_GUID : NV_ENC_H264_PROFILE_HIGH_GUID;
        config->gopLength = framerate * 2;
        config->frameIntervalP = 1;
        config->frameFieldMode = NV_ENC_PARAMS_FRAME_FIELD_MODE_FRAME;
        config->rcParams.rateControlMode = streaming ? NV_ENC_PARAMS_RC_CBR_LOWDELAY_HQ : NV_ENC_PARAMS_RC_VBR_HQ;
        config->rcParams.averageBitRate = bitrate * 1024; // used for CBR and VBR
        config->rcParams.maxBitRate = maxBitrate * 1024; // used for VBR
        config->rcParams.targetQuality = quality;

        NV_ENC_CONFIG_H264* h264 = &config->encodeCodecConfig.h264Config;
        h264->idrPeriod = config->gopLength;
        h264->sliceMode = 0;
        h264->sliceModeData = 0;
        h264->repeatSPSPPS = 1;
        h264->outputPictureTimingSEI = 1;
        h264->h264VUIParameters.videoSignalTypePresentFlag = 1;
        h264->h264VUIParameters.videoFullRangeFlag = 1;
        h264->h264VUIParameters.colourDescriptionPresentFlag = 1;
        h264->h264VUIParameters.colourMatrix = 1; // BT.709
        h264->h264VUIParameters.colourPrimaries = 1; // BT.709
        h264->h264VUIParameters.transferCharacteristics = 1; // BT.709
        h264->outputBufferingPeriodSEI = streaming ? 1 : 0;

        // lookahead
        // config->rcParams.lookaheadDepth = 8;
        // config->rcParams.enableLookahead = 1;

        // temporal AQ
        // config->rcParams.enableAQ = psycho_aq;
        // config->rcParams.enableTemporalAQ = psycho_aq;

        err = NvEncodeApi.nvEncInitializeEncoder(encoder, &params);
        if (err != NV_ENC_SUCCESS)
        {
            Log("nvEncOpenEncodeSessionEx failed, error = %u", err);
            goto bail;
        }
    }

    // create texture used as input for encoding
    {
        D3D11_TEXTURE2D_DESC desc =
        {
            .Width = width,
            .Height = height,
            .MipLevels = 1,
            .ArraySize = 1,
            .Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            .SampleDesc = { 1, 0 },
            .Usage = D3D11_USAGE_DEFAULT,
            .BindFlags = D3D11_BIND_UNORDERED_ACCESS,
        };

        hr = ID3D11Device_CreateTexture2D(Device, &desc, NULL, &texture);
        if (FAILED(hr))
        {
            Log("ID3D11Device_CreateTexture2D failed, error = 0x%08x", hr);
            goto bail;
        }

        hr = ID3D11Device_CreateUnorderedAccessView(Device, (ID3D11Resource*)texture, NULL, &uaView);
        if (FAILED(hr))
        {
            Log("ID3D11Device_CreateUnorderedAccessView failed, error = 0x%08x", hr);
            goto bail;
        }
    }

    // create encoding input resources
    {
        NV_ENC_REGISTER_RESOURCE reg =
        {
            .version = NV_ENC_REGISTER_RESOURCE_VER,
            .resourceType = NV_ENC_INPUT_RESOURCE_TYPE_DIRECTX,
            .width = width,
            .height = height,
            .subResourceIndex = 0,
            .resourceToRegister = texture,
            .bufferFormat = NV_ENC_BUFFER_FORMAT_ABGR,
            .bufferUsage = NV_ENC_INPUT_IMAGE,
        };

        err = NvEncodeApi.nvEncRegisterResource(encoder, &reg);
        if (err != NV_ENC_SUCCESS)
        {
            Log("nvEncRegisterResource failed, error = %u", err);
            goto bail;
        }

        input = reg.registeredResource;
    }

    // create encoding output buffer
    {
        NV_ENC_CREATE_BITSTREAM_BUFFER buffer =
        {
            .version = NV_ENC_CREATE_BITSTREAM_BUFFER_VER,
        };

        err = NvEncodeApi.nvEncCreateBitstreamBuffer(encoder, &buffer);
        if (err != NV_ENC_SUCCESS)
        {
            Log("nvEncCreateBitstreamBuffer failed, error = %u", err);
            goto bail;
        }

        output = buffer.bitstreamBuffer;
    }

    // launch ffmpeg.exe process
    {
        SECURITY_ATTRIBUTES sa =
        {
            .nLength = sizeof(sa),
            .bInheritHandle = TRUE,
        };

        ok = CreatePipe(&read, &write, &sa, 0);
        if (!ok)
        {
            Log("CreatePipe failed, error = 0x%08x", GetLastError());
            goto bail;
        }

        ok = SetHandleInformation(write, HANDLE_FLAG_INHERIT, 0);
        if (!ok)
        {
            Log("SetHandleInformation failed, error = 0x%08x", GetLastError());
            goto bail;
        }

        const char* format = streaming ? "flv" : "mp4";
        const char* re = streaming ? "-re" : "";

        char cmdline[1024];
        wsprintfA(cmdline, "%s %s -loglevel quiet -i - -c:v copy -y -shortest -f %s \"%s\"",
            PathToFFMPEG, re, format, destination);

        STARTUPINFOA si =
        {
            .cb = sizeof(si),
            .dwFlags = STARTF_USESTDHANDLES,
            .hStdInput = read,
        };

        PROCESS_INFORMATION pi;
        ok = CreateProcessA(NULL, cmdline, NULL, NULL, TRUE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi); // CREATE_NEW_CONSOLE // CREATE_NO_WINDOW
        if (!ok)
        {
            Log("CreateProcessA failed, error = 0x%08x", GetLastError());
            goto bail;
        }
        CloseHandle(pi.hThread);

        process = pi.hProcess;
    }

    // ready to go!

    capture->encoder = encoder;
    capture->uaView = uaView;
    capture->input = input;
    capture->output = output;
    capture->width = width;
    capture->height = height;
    capture->timestamp = 0;
    capture->frame = 0;
    capture->process = process;
    capture->write = write;

    capture->framerate = framerate;
    capture->previous.QuadPart = 0;

    capture->srView = NULL;

    write = NULL;
    process = NULL;
    input = NULL;
    output = NULL;
    uaView = NULL;
    encoder = NULL;

    result = id;

bail:
    if (read) CloseHandle(read);
    if (write) CloseHandle(write);
    if (process)
    {
        TerminateProcess(process, 0);
        CloseHandle(process);
    }
    if (input) NvEncodeApi.nvEncUnregisterResource(encoder, input);
    if (output) NvEncodeApi.nvEncDestroyBitstreamBuffer(encoder, output);
    if (uaView) ID3D11UnorderedAccessView_Release(uaView);
    if (texture) ID3D11Texture2D_Release(texture);
    if (encoder) NvEncodeApi.nvEncDestroyEncoder(encoder);

    return result;
}

static void VideoCapture_Flush(VideoCapture* capture)
{
    NV_ENC_LOCK_BITSTREAM bitstream =
    {
        .version = NV_ENC_LOCK_BITSTREAM_VER,
        .outputBitstream = capture->output,
    };

    NVENCSTATUS err = NvEncodeApi.nvEncLockBitstream(capture->encoder, &bitstream);
    if (err == NV_ENC_SUCCESS)
    {
        DWORD written;
        BOOL ok = WriteFile(capture->write, bitstream.bitstreamBufferPtr, bitstream.bitstreamSizeInBytes, &written, NULL);
        if (!ok)
        {
            Log("WriteFile failed, error = 0x%08x", GetLastError());
        }
        else if (written != bitstream.bitstreamSizeInBytes)
        {
            Log("WriteFile wrote only %u bytes (needed to write %u bytes)", written, bitstream.bitstreamSizeInBytes);
        }

        NvEncodeApi.nvEncUnlockBitstream(capture->encoder, capture->output);
    }
    else
    {
        Log("nvEncLockBitstream failed, error = %u", err);
    }
}

static void UNITY_INTERFACE_API VideoCapture_Update(int id)
{
    VideoCapture* capture = Captures + id;

    ID3D11DeviceContext_CSSetShader(Context, FlipShader, NULL, 0);
    ID3D11DeviceContext_CSSetShaderResources(Context, 0, 1, &capture->srView);
    ID3D11DeviceContext_CSSetUnorderedAccessViews(Context, 0, 1, &capture->uaView, NULL);
    ID3D11DeviceContext_Dispatch(Context, (capture->width + 7) / 8, (capture->height + 7) / 8, 1);

    NV_ENC_MAP_INPUT_RESOURCE resource =
    {
        .version = NV_ENC_MAP_INPUT_RESOURCE_VER,
        .registeredResource = capture->input,
    };

    AcquireSRWLockShared(&SrwLock);
    NVENCSTATUS err = NvEncodeApi.nvEncMapInputResource(capture->encoder, &resource);
    ReleaseSRWLockShared(&SrwLock);
    if (err != NV_ENC_SUCCESS)
    {
        Log("nvEncMapInputResource failed, error = %u", err);
        return;
    }

    LARGE_INTEGER counter;
    QueryPerformanceCounter(&counter);

    uint64_t timestamp;
    if (capture->previous.QuadPart == 0)
    {
        timestamp = 0;
    }
    else
    {
        uint64_t delta = counter.QuadPart - capture->previous.QuadPart;
        timestamp = capture->timestamp + delta * 10 * 1000 * 1000 / Frequency.QuadPart;
    }
    capture->previous = counter;

    NV_ENC_PIC_PARAMS params =
    {
        .version = NV_ENC_PIC_PARAMS_VER,
        .inputWidth = capture->width,
        .inputHeight = capture->height,
        // .encodePicFlags = 0, // NV_ENC_PIC_FLAG_FORCEINTRA
        .frameIdx = capture->frame++,
        .inputTimeStamp = timestamp,
        .inputDuration = 10 * 1000 * 1000 / capture->framerate,
        .inputBuffer = resource.mappedResource,
        .outputBitstream = capture->output,
        // .completionEvent = event,
        .bufferFmt = resource.mappedBufferFmt,
        .pictureStruct = NV_ENC_PIC_STRUCT_FRAME,
    };

    err = NvEncodeApi.nvEncEncodePicture(capture->encoder, &params);
    if (err != NV_ENC_SUCCESS)
    {
        Log("nvEncEncodePicture failed, error = %u", err);
    }

    err = NvEncodeApi.nvEncUnmapInputResource(capture->encoder, resource.mappedResource);
    if (err != NV_ENC_SUCCESS)
    {
        Log("nvEncUnmapInputResource failed, error = %u", err);
    }

    VideoCapture_Flush(capture);
}

UNITY_INTERFACE_EXPORT void VideoCapture_Stop(int id)
{
    VideoCapture* capture = Captures + id;

    {
        NV_ENC_PIC_PARAMS params =
        {
            .version = NV_ENC_PIC_PARAMS_VER,
            .inputWidth = capture->width,
            .inputHeight = capture->height,
            .encodePicFlags = NV_ENC_PIC_FLAG_EOS,
            .outputBitstream = capture->output,
            // .completionEvent = event,
        };

        NVENCSTATUS err = NvEncodeApi.nvEncEncodePicture(capture->encoder, &params);
        if (err == NV_ENC_SUCCESS)
        {
            VideoCapture_Flush(capture);
        }
        else
        {
            Log("nvEncEncodePicture failed, error = %u", err);
        }
    }

    CloseHandle(capture->write);
    WaitForSingleObject(capture->process, INFINITE);
    CloseHandle(capture->process);

    NvEncodeApi.nvEncDestroyBitstreamBuffer(capture->encoder, capture->output);
    NvEncodeApi.nvEncUnregisterResource(capture->encoder, capture->input);
    ID3D11UnorderedAccessView_Release(capture->uaView);
    NvEncodeApi.nvEncDestroyEncoder(capture->encoder);

    if (capture->srView) ID3D11ShaderResourceView_Release(capture->srView);

    capture->encoder = NULL;
}

UNITY_INTERFACE_EXPORT void VideoCapture_Reset(int id, ID3D11Texture2D* texture)
{
    VideoCapture* capture = Captures + id;

    if (capture->srView)
    {
        ID3D11ShaderResourceView_Release(capture->srView);
        capture->srView = NULL;
    }

    D3D11_SHADER_RESOURCE_VIEW_DESC desc =
    {
        .Format = DXGI_FORMAT_R8G8B8A8_UNORM,
        .ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D,
        .Texture2D = { 0, 1 },
    };
    HRESULT hr = ID3D11Device_CreateShaderResourceView(Device, (ID3D11Resource*)texture, &desc, &capture->srView);
    if (FAILED(hr))
    {
        Log("ID3D11Device_CreateShaderResourceView failed, error = 0x%08x", hr);
    }

    // TODO: maybe force next frame to be I-frame
}

UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API VideoCapture_GetRenderEventFunc(void)
{
    return VideoCapture_Update;
}

static void UNITY_INTERFACE_API OnUnityGraphicsDeviceEvent(UnityGfxDeviceEventType event)
{
    if (event == kUnityGfxDeviceEventInitialize)
    {
        UnityGfxRenderer renderer = UnityGraphics->GetRenderer();
        if (renderer == kUnityGfxRendererD3D11)
        {
            IUnityGraphicsD3D11* d3d11 = UNITY_GET_INTERFACE(UnityInterfaces, IUnityGraphicsD3D11);

            Device = d3d11->GetDevice();
            ID3D11Device_GetImmediateContext(Device, &Context);

            ID3D11Device_CreateComputeShader(Device, g_FlipKernel, sizeof(g_FlipKernel), NULL, &FlipShader);

            QueryPerformanceCounter(&Frequency);
        }
    }
    else if (event == kUnityGfxDeviceEventShutdown)
    {
        ID3D11ComputeShader_Release(FlipShader);
        UnityGraphics->UnregisterDeviceEventCallback(OnUnityGraphicsDeviceEvent);
    }
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces)
{
    UnityInterfaces = interfaces;
    UnityGraphics = UNITY_GET_INTERFACE(interfaces, IUnityGraphics);

    UnityGraphics->RegisterDeviceEventCallback(OnUnityGraphicsDeviceEvent);
    OnUnityGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    UnityGraphics->UnregisterDeviceEventCallback(OnUnityGraphicsDeviceEvent);
}

BOOL WINAPI _DllMainCRTStartup(HINSTANCE instance, DWORD reason, LPVOID reserved)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(instance);
    }
    return TRUE;
}

#pragma function(memset)
void* memset(void* dest, int c, size_t count)
{
    __stosb((unsigned char*)dest, (unsigned char)c, count);
    return dest;
}
