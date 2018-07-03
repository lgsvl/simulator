#pragma once
#include "IUnityInterface.h"

typedef enum UnityGfxRenderer
{
    //kUnityGfxRendererOpenGL            =  0, // Legacy OpenGL, removed
    //kUnityGfxRendererD3D9              =  1, // Direct3D 9, removed
    kUnityGfxRendererD3D11             =  2, // Direct3D 11
    kUnityGfxRendererGCM               =  3, // PlayStation 3
    kUnityGfxRendererNull              =  4, // "null" device (used in batch mode)
    kUnityGfxRendererOpenGLES20        =  8, // OpenGL ES 2.0
    kUnityGfxRendererOpenGLES30        = 11, // OpenGL ES 3.0
    kUnityGfxRendererGXM               = 12, // PlayStation Vita
    kUnityGfxRendererPS4               = 13, // PlayStation 4
    kUnityGfxRendererXboxOne           = 14, // Xbox One
    kUnityGfxRendererMetal             = 16, // iOS Metal
    kUnityGfxRendererOpenGLCore        = 17, // OpenGL core
    kUnityGfxRendererD3D12             = 18, // Direct3D 12
    kUnityGfxRendererVulkan            = 21, // Vulkan
    kUnityGfxRendererNvn               = 22, // Nintendo Switch NVN API
    kUnityGfxRendererXboxOneD3D12      = 23  // MS XboxOne Direct3D 12
} UnityGfxRenderer;

typedef enum UnityGfxDeviceEventType
{
    kUnityGfxDeviceEventInitialize     = 0,
    kUnityGfxDeviceEventShutdown       = 1,
    kUnityGfxDeviceEventBeforeReset    = 2,
    kUnityGfxDeviceEventAfterReset     = 3,
} UnityGfxDeviceEventType;

typedef void (UNITY_INTERFACE_API * IUnityGraphicsDeviceEventCallback)(UnityGfxDeviceEventType eventType);

// Should only be used on the rendering thread unless noted otherwise.
UNITY_DECLARE_INTERFACE(IUnityGraphics)
{
    UnityGfxRenderer(UNITY_INTERFACE_API * GetRenderer)();  // Thread safe

    // This callback will be called when graphics device is created, destroyed, reset, etc.
    // It is possible to miss the kUnityGfxDeviceEventInitialize event in case plugin is loaded at a later time,
    // when the graphics device is already created.
    void(UNITY_INTERFACE_API * RegisterDeviceEventCallback)(IUnityGraphicsDeviceEventCallback callback);
    void(UNITY_INTERFACE_API * UnregisterDeviceEventCallback)(IUnityGraphicsDeviceEventCallback callback);
    int(UNITY_INTERFACE_API * ReserveEventIDRange)(int count);  // reserves 'count' event IDs. Plugins should use the result as a base index when issuing events back and forth to avoid event id clashes.
};
UNITY_REGISTER_INTERFACE_GUID(0x7CBA0A9CA4DDB544ULL, 0x8C5AD4926EB17B11ULL, IUnityGraphics)


// Certain Unity APIs (GL.IssuePluginEvent, CommandBuffer.IssuePluginEvent) can callback into native plugins.
// Provide them with an address to a function of this signature.
typedef void (UNITY_INTERFACE_API * UnityRenderingEvent)(int eventId);
typedef void (UNITY_INTERFACE_API * UnityRenderingEventAndData)(int eventId, void* data);
