#include "rfb_client.h"
#include <string.h>

#include "Unity/IUnityGraphics.h"

#ifdef _WIN32
#define COBJMACROS
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>

#include "Unity/IUnityGraphicsD3D11.h"

#define DLL_EXPORT __declspec(dllexport)

static IUnityInterfaces* unity;
static ID3D11Device* device;
static ID3D11DeviceContext* context;
#define SAFE_RELEASE(x) if (x) { IUnknown_Release((IUnknown*)x); x = NULL; }

#else

#define GL_GLEXT_PROTOTYPES
#include <GL/gl.h>
#include <stdio.h>

#include "Unity/IUnityInterface.h"
#define DLL_EXPORT __attribute__((visibility("default")))

static uint32_t ilog2(uint32_t x)
{
#ifdef _MSC_VER
    unsigned long r = 0;
    _BitScanReverse(&r, x);
    return r;
#else
    return 32 - __builtin_clz(x) - 1;
#endif
}

static uint32_t max(uint32_t a, uint32_t b)
{
    return a > b ? a : b;
}

#endif

static void RfbClientDefaultDebugOutput(const char* message)
{
#ifdef _WIN32
    OutputDebugStringA(message);
#else
    fputs(message, stderr);
#endif
}

static void (*DebugOutput)(const char*) = RfbClientDefaultDebugOutput;

typedef struct {
    rfbc* client;

    uint32_t width;
    uint32_t height;

    int closing;
#ifdef _WIN32
    ID3D11Texture2D* staging;
    ID3D11Texture2D* texture;
    ID3D11ShaderResourceView* view;
#else
    GLuint texture;
    GLuint pbo;
#endif
} RfbClient;

#define MAX_CLIENTS 32

static RfbClient gClients[MAX_CLIENTS];

void DLL_EXPORT UNITY_INTERFACE_API RfbClientSetDebug(void* ptr)
{
    *((void**)&DebugOutput) = ptr;
}

int DLL_EXPORT UNITY_INTERFACE_API RfbClientStart(const char* address, uint16_t port)
{
    if (address == NULL)
    {
        return -1;
    }
    int id;
    for (id=0; id<MAX_CLIENTS; id++)
    {
       if (gClients[id].client == NULL)
       {
           break;
       }
    }
    if (id == MAX_CLIENTS)
    {
        return -1;
    }

    RfbClient* client = gClients + id;
    memset(client, 0, sizeof(*client));

    client->client = rfbc_connect(address, port, 0);
    return id;
}

void DLL_EXPORT UNITY_INTERFACE_API RfbClientStop(int id)
{
    RfbClient* client = gClients + id;
    client->closing = 1;
}

int DLL_EXPORT UNITY_INTERFACE_API RfbClientDoUpdate(int id, int* width, int* height, void** texture)
{
    RfbClient* client = gClients + id;

    *width = client->width;
    *height = client->height;
#ifdef _WIN32
    *texture = client->view;
#else
    *texture = (void*)(intptr_t)client->texture;
#endif

    return rfbc_get_status(client->client) == RFBC_STATUS_CONNECTED;
}

static void RfbClientUpdate(int id)
{
    RfbClient* client = gClients + id;

    if (client->client && client->closing)
    {
#ifdef _WIN32
        SAFE_RELEASE(client->staging);
        SAFE_RELEASE(client->texture);
        // SAFE_RELEASE(client->view); // unity is releasing this
#else
        glDeleteTextures(1, &client->texture);
        glDeleteBuffers(1, &client->pbo);
        client->pbo = client->texture = 0;
#endif
        rfbc_close(client->client);
        client->client = NULL;
        client->closing = 0;
        return;
    }

    if (rfbc_get_status(client->client) != RFBC_STATUS_CONNECTED)
    {
        return;
    }

    uint32_t w, h;
    rfbc_get_size(client->client, &w, &h);

    if (client->width != w || client->height != h)
    {
        client->width = w;
        client->height = h;
#ifdef _WIN32
        SAFE_RELEASE(client->staging);
        SAFE_RELEASE(client->texture);
        //SAFE_RELEASE(client->view); // unity is releasing this

        D3D11_TEXTURE2D_DESC staging_desc =
        {
            .Width = w,
            .Height = h,
            .MipLevels = 1,
            .ArraySize = 1,
            .Format = DXGI_FORMAT_B8G8R8X8_UNORM,
            .SampleDesc =
            {
                .Count = 1,
                .Quality = 0,
            },
            .Usage = D3D11_USAGE_DYNAMIC,
            .BindFlags = D3D11_BIND_SHADER_RESOURCE,
            .CPUAccessFlags = D3D11_CPU_ACCESS_WRITE,
        };
        HRESULT hr = ID3D11Device_CreateTexture2D(device, &staging_desc, NULL, &client->staging);
        if (FAILED(hr))
        {
            DebugOutput("failed to create staging texture");
        }

        D3D11_TEXTURE2D_DESC texture_desc =
        {
            .Width = w,
            .Height = h,
            .MipLevels = 0,
            .ArraySize = 1,
            .Format = DXGI_FORMAT_B8G8R8X8_UNORM_SRGB,
            .SampleDesc =
            {
                .Count = 1,
                .Quality = 0,
            },
            .Usage = D3D11_USAGE_DEFAULT,
            .BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET,
            .MiscFlags = D3D11_RESOURCE_MISC_GENERATE_MIPS,
        };

        hr = ID3D11Device_CreateTexture2D(device, &texture_desc, NULL, &client->texture);
        if (FAILED(hr))
        {
            DebugOutput("failed to create texture");
        }

        hr = ID3D11Device_CreateShaderResourceView(device, (ID3D11Resource*)client->texture, NULL, &client->view);
        if (FAILED(hr))
        {
            DebugOutput("failed to create texture view");
        }
#else
        glGenTextures(1, &client->texture);
        glBindTexture(GL_TEXTURE_2D, client->texture);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

        GLint minor, major;
        glGetIntegerv(GL_MAJOR_VERSION, &major);
        glGetIntegerv(GL_MINOR_VERSION, &minor);

        if (major > 4 || (major == 4 && minor >= 2))
        {
            glTexStorage2D(GL_TEXTURE_2D, ilog2(max(w, h))+1, GL_SRGB8, w, h);
        }
        else
        {
            glTexImage2D(GL_TEXTURE_2D, 0, GL_SRGB8, w, h, 0, GL_BGRA, GL_UNSIGNED_BYTE, NULL);
        }
        glBindTexture(GL_TEXTURE_2D, 0);

        glGenBuffers(1, &client->pbo);
        glBindBuffer(GL_PIXEL_UNPACK_BUFFER, client->pbo);
        if (major > 4 || (major == 4 && minor >= 4))
        {
            glBufferStorage(GL_PIXEL_UNPACK_BUFFER, w * h * 4, NULL, GL_CLIENT_STORAGE_BIT | GL_MAP_WRITE_BIT);
        }
        else
        {
            glBufferData(GL_PIXEL_UNPACK_BUFFER, w * h * 4, NULL, GL_STREAM_DRAW);
        }
        glBindBuffer(GL_PIXEL_UNPACK_BUFFER, 0);
#endif
    }

    if (client->texture)
    {
#ifdef _WIN32
        D3D11_MAPPED_SUBRESOURCE map;
        HRESULT hr = ID3D11DeviceContext_Map(context, (ID3D11Resource*)client->staging, 0, D3D11_MAP_WRITE_DISCARD, 0, &map);
        if (SUCCEEDED(hr))
        {
            rfbc_get_data(client->client, map.pData, map.RowPitch, w, h);
            ID3D11DeviceContext_Unmap(context, (ID3D11Resource*)client->staging, 0);

            ID3D11DeviceContext_CopySubresourceRegion(context, (ID3D11Resource*)client->texture, 0, 0, 0, 0, (ID3D11Resource*)client->staging, 0, NULL);
            ID3D11DeviceContext_GenerateMips(context, client->view);
        }
        else
        {
            DebugOutput("failed to map staging texture");
        }
#else
        glBindBuffer(GL_PIXEL_UNPACK_BUFFER, client->pbo);
        GLvoid* data = glMapBufferRange(GL_PIXEL_UNPACK_BUFFER, 0, w * h * 4, GL_MAP_WRITE_BIT | GL_MAP_INVALIDATE_BUFFER_BIT);
        if (data)
        {
            rfbc_get_data(client->client, data, w * 4, w, h);
            glUnmapBuffer(GL_PIXEL_UNPACK_BUFFER);

            glBindTexture(GL_TEXTURE_2D, client->texture);
            glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
            glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, w, h, GL_BGRA, GL_UNSIGNED_BYTE, NULL);
            glGenerateMipmap(GL_TEXTURE_2D);
            glBindTexture(GL_TEXTURE_2D, 0);
        }
        else
        {
            DebugOutput("failed to map staging buffer");
        }
        glBindBuffer(GL_PIXEL_UNPACK_BUFFER, 0);
#endif
    }

    rfbc_refresh(client->client, 1);
}

UnityRenderingEvent DLL_EXPORT UNITY_INTERFACE_API RfbClientGetUpdateFunc(void)
{
    return &RfbClientUpdate;
}

#ifdef _WIN32

static void UNITY_INTERFACE_API OnDeviceEvent(UnityGfxDeviceEventType type)
{
    if (type == kUnityGfxDeviceEventInitialize)
    {
        IUnityGraphicsD3D11* d3d11 = (IUnityGraphicsD3D11*)unity->GetInterface(IUnityGraphicsD3D11_GUID);
        device = d3d11->GetDevice();
        ID3D11Device_GetImmediateContext(device, &context);
    }
    else if (type == kUnityGfxDeviceEventShutdown)
    {
        SAFE_RELEASE(context);
        device = NULL;
    }
}

void DLL_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces)
{
    unity = interfaces;
    IUnityGraphics* graphics = (IUnityGraphics*)unity->GetInterface(IUnityGraphics_GUID);
    graphics->RegisterDeviceEventCallback(OnDeviceEvent);
    OnDeviceEvent(kUnityGfxDeviceEventInitialize);
}

void DLL_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    IUnityGraphics* graphics = (IUnityGraphics*)unity->GetInterface(IUnityGraphics_GUID);
    graphics->UnregisterDeviceEventCallback(OnDeviceEvent);
}

BOOL WINAPI DllMain(HINSTANCE module, DWORD reason, LPVOID reserved)
{
    (void)reserved;
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(module);
    }
    return TRUE;
}

#endif
