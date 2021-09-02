using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry
{
    /// <summary>
    /// Graphics device unit
    /// </summary>
    /// <remarks>
    /// The value types are not made nullable due to limitation of <see cref="JsonUtility"/>
    /// </remarks>
    /// <seealso href="https://feedback.unity3d.com/suggestions/add-support-for-nullable-types-to-jsonutility"/>
    [Serializable]
    public class Gpu
    {
        /// <summary>
        /// The name of the graphics device
        /// </summary>
        /// <example>
        /// iPod touch:	Apple A8 GPU
        /// Samsung S7: Mali-T880
        /// </example>
        public string name;

        /// <summary>
        /// The PCI Id of the graphics device
        /// </summary>
        /// <remarks>
        /// Combined with <see cref="vendor_id"/> uniquely identifies the GPU
        /// </remarks>
        public int id;

        /// <summary>
        /// The PCI vendor Id of the graphics device
        /// </summary>
        /// <remarks>
        /// Combined with <see cref="Id"/> uniquely identifies the GPU
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/windows-hardware/drivers/install/identifiers-for-pci-devices"/>
        /// <seealso href="http://pci-ids.ucw.cz/read/PC/"/>
        public string vendor_id;

        /// <summary>
        /// The vendor name reported by the graphic device
        /// </summary>
        /// <example>
        /// Apple, ARM, WebKit
        /// </example>
        public string vendor_name;

        /// <summary>
        /// Total GPU memory available in mega-bytes.
        /// </summary>
        public int memory_size;

        /// <summary>
        /// Device type
        /// </summary>
        /// <remarks>The low level API used</remarks>
        /// <example>Metal, Direct3D11, OpenGLES3, PlayStation4, XboxOne</example>
        public string api_type;

        /// <summary>
        /// Whether the GPU is multi-threaded rendering or not.
        /// </summary>
        /// <remarks>Type hre should be Nullable{bool} which isn't supported by JsonUtility></remarks>
        public bool multi_threaded_rendering;

        /// <summary>
        /// The Version of the API of the graphics device
        /// </summary>
        /// <example>
        /// iPod touch: Metal
        /// Android: OpenGL ES 3.2 v1.r22p0-01rel0.f294e54ceb2cb2d81039204fa4b0402e
        /// WebGL Windows: OpenGL ES 3.0 (WebGL 2.0 (OpenGL ES 3.0 Chromium))
        /// OpenGL 2.0, Direct3D 9.0c
        /// </example>
        public string version;

        /// <summary>
        /// The Non-Power-Of-Two support level
        /// </summary>
        /// <example>
        /// Full
        /// </example>
        public string npot_support;
    }

    /// <summary>
    /// Represents Sentry's context for OS
    /// </summary>
    /// <remarks>
    /// Defines the operating system that caused the event. In web contexts, this is the operating system of the browser (normally pulled from the User-Agent string).
    /// </remarks>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/#context-types"/>
    [Serializable]
    public class OperatingSystem
    {
        /// <summary>
        /// The name of the operating system.
        /// </summary>
        public string name;

        /// <summary>
        /// The version of the operating system.
        /// </summary>
        public string version;

        /// <summary>
        /// An optional raw description that Sentry can use in an attempt to normalize OS info.
        /// </summary>
        /// <remarks>
        /// When the system doesn't expose a clear API for <see cref="Name"/> and <see cref="Version"/>
        /// this field can be used to provide a raw system info (e.g: uname)
        /// </remarks>
        public string raw_description;

        /// <summary>
        /// The internal build revision of the operating system.
        /// </summary>
        public string build;

        /// <summary>
        ///  If known, this can be an independent kernel version string. Typically
        /// this is something like the entire output of the 'uname' tool.
        /// </summary>
        public string kernel_version;
    }

    /// <summary>
    /// Describes the device that caused the event. This is most appropriate for mobile applications.
    /// </summary>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/"/>
    [Serializable]
    public class Device
    {
        /// <summary>
        /// The name of the device. This is typically a hostname.
        /// </summary>
        public string name;
        /// <summary>
        /// The family of the device.
        /// </summary>
        /// <remarks>
        /// This is normally the common part of model names across generations.
        /// </remarks>
        /// <example>
        /// iPhone, Samsung Galaxy
        /// </example>
        public string family;
        /// <summary>
        /// The model name.
        /// </summary>
        /// <example>
        /// Samsung Galaxy S3
        /// </example>
        public string model;
        /// <summary>
        /// An internal hardware revision to identify the device exactly.
        /// </summary>
        public string model_id;
        /// <summary>
        /// The CPU architecture.
        /// </summary>
        public string arch;
        /// <summary>
        /// The CPU description
        /// </summary>
        /// <example>
        /// Intel(R) Core(TM) i7-7920HQ CPU @ 3.10GHz
        /// </example>
        public string cpu_description;
        /// <summary>
        /// If the device has a battery an integer defining the battery level (in the range 0-100).
        /// </summary>
        public float battery_level;
        /// <summary>
        /// The battery status
        /// </summary>
        /// <example>
        /// Unknown, Charging, Discharging, NotCharging, Full
        /// </example>
        /// <see cref="BatteryStatus"/>
        public string battery_status;
        /// <summary>
        /// This can be a string portrait or landscape to define the orientation of a device.
        /// </summary>
        public string orientation;
        /// <summary>
        /// A boolean defining whether this device is a simulator or an actual device.
        /// </summary>
        public bool simulator;
        /// <summary>
        /// Total system memory available in bytes.
        /// </summary>
        public long memory_size;
        /// <summary>
        /// A formatted UTC timestamp when the system was booted.
        /// </summary>
        /// <example>
        /// 018-02-08T12:52:12Z
        /// </example>
        public DateTimeOffset? boot_time;
        /// <summary>
        /// The timezone of the device.
        /// </summary>
        /// <example>
        /// Europe/Vienna
        /// </example>
        public string timezone;
        /// <summary>
        /// The type of the device
        /// </summary>
        /// <example>
        /// Unknown, Handheld, Console, Desktop
        /// </example>
        /// <see cref="DeviceType"/>
        public string device_type;
    }

    /// <summary>
    /// Describes the application.
    /// </summary>
    /// <remarks>
    /// As opposed to the runtime, this is the actual application that
    /// was running and carries meta data about the current session.
    /// </remarks>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/"/>
    [Serializable]
    public class App
    {
        /// <summary>
        /// Version-independent application identifier, often a dotted bundle ID.
        /// </summary>
        public string app_identifier;
        /// <summary>
        /// Formatted UTC timestamp when the application was started by the user.
        /// </summary>
        // DateTimeOffset? doesn't get serialized
        public string app_start_time;
        /// <summary>
        /// Application specific device identifier.
        /// </summary>
        public string device_app_hash;
        /// <summary>
        /// String identifying the kind of build, e.g. testflight.
        /// </summary>
        public string build_type;
        /// <summary>
        /// Human readable application name, as it appears on the platform.
        /// </summary>
        public string app_name;
        /// <summary>
        /// Human readable application version, as it appears on the platform.
        /// </summary>
        public string app_version;
        /// <summary>
        /// Internal build identifier, as it appears on the platform.
        /// </summary>
        public string app_build;
    }

    [Serializable]
    public class SdkVersion
    {
        public string name = "sentry.unity.lite";
        public string version = "1.0.2";
    }

    [Serializable]
    public class Context
    {
        public App app;
        public Gpu gpu;
        public OperatingSystem os;
        public Device device;

        public Context()
        {
            os = new OperatingSystem
            {
                // TODO: Will move to raw_description once parsing is done in Sentry
                name = SystemInfo.operatingSystem
            };

            device = new Device();
            switch (Input.deviceOrientation)
            {
                case UnityEngine.DeviceOrientation.Portrait:
                case UnityEngine.DeviceOrientation.PortraitUpsideDown:
                    device.orientation = "portrait";
                    break;
                case UnityEngine.DeviceOrientation.LandscapeLeft:
                case UnityEngine.DeviceOrientation.LandscapeRight:
                    device.orientation = "landscape";
                    break;
                case UnityEngine.DeviceOrientation.FaceUp:
                case UnityEngine.DeviceOrientation.FaceDown:
                    // TODO: Add to protocol?
                    break;
            }

            var model = SystemInfo.deviceModel;
            if (model != SystemInfo.unsupportedIdentifier
                // Returned by the editor
                && model != "System Product Name (System manufacturer)")
            {
                device.model = model;
            }

            device.battery_level = SystemInfo.batteryLevel * 100;
            device.battery_status = SystemInfo.batteryStatus.ToString();

            // This is the approximate amount of system memory in megabytes.
            // This function is not supported on Windows Store Apps and will always return 0.
            if (SystemInfo.systemMemorySize != 0)
            {
                device.memory_size = SystemInfo.systemMemorySize * 1048576L; // Sentry device mem is in Bytes
            }

            device.device_type = SystemInfo.deviceType.ToString();
            device.cpu_description = SystemInfo.processorType;

#if UNITY_EDITOR
            device.simulator = true;
#else
            device.simulator = false;
#endif

            gpu = new Gpu
            {
                id = SystemInfo.graphicsDeviceID,
                name = SystemInfo.graphicsDeviceName,
                vendor_id = SystemInfo.graphicsDeviceVendorID.ToString(),
                vendor_name = SystemInfo.graphicsDeviceVendor,
                memory_size = SystemInfo.graphicsMemorySize,
                multi_threaded_rendering = SystemInfo.graphicsMultiThreaded,
                npot_support = SystemInfo.npotSupport.ToString(),
                version = SystemInfo.graphicsDeviceVersion,
                api_type = SystemInfo.graphicsDeviceType.ToString()
            };

            app = new App();
            app.app_start_time = DateTimeOffset.UtcNow
                .AddSeconds(-Time.realtimeSinceStartup)
                .ToString("yyyy-MM-ddTHH\\:mm\\:ssZ");

            if (Debug.isDebugBuild)
            {
                app.build_type = "debug";
            }
            else
            {
                app.build_type = "release";
            }
        }
    }

    // Unity doesn't serialize Dictionary
    [Serializable]
    public class Tags
    {
        public string deviceUniqueIdentifier;
    }

    [Serializable]
    public class Extra
    {
        public string unityVersion;
        public string simulatorVersion;
    }

    [Serializable]
    public class User
    {
        public string email = "test@test.com";
    }

    [Serializable]
    public class SentryEvent
    {
        public string event_id;
        public string message;
        public string timestamp;
        public string logger;
        public string level;
        public string platform = "csharp";
        public string release;
        public Context contexts;
        public SdkVersion sdk = new SdkVersion();
        public List<Breadcrumb> breadcrumbs = null;
        public User user = new User();
        public Tags tags;
        public Extra extra;

        public SentryEvent(string message, List<Breadcrumb> breadcrumbs = null)
        {
            this.event_id = Guid.NewGuid().ToString("N");
            this.message = message;
            this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            this.level = "error";
            this.breadcrumbs = breadcrumbs;
            this.contexts = new Context();
            this.release = Application.version;
            this.tags = new Tags();
            this.extra = new Extra();
        }
    }

    [Serializable]
    public class StackTraceContainer
    {
        public List<StackTraceSpec> frames;

        public StackTraceContainer(List<StackTraceSpec> frames)
        {
            this.frames = frames;
        }
    }

    [Serializable]
    public class StackTraceSpec
    {
        public string filename;
        public string function;
        public string module = "";
        public int lineno;
        public bool in_app;

        public StackTraceSpec(string filename, string function, int lineNo, bool inApp)
        {
            this.filename = filename;
            this.function = function;
            lineno = lineNo;
            in_app = inApp;
        }
    }

    [Serializable]
    public class ExceptionSpec
    {
        public string type;
        public string value;
        public StackTraceContainer stacktrace;

        public ExceptionSpec(string type, string value, List<StackTraceSpec> stacktrace)
        {
            this.type = type;
            this.value = value;
            this.stacktrace = new StackTraceContainer(stacktrace);
        }
    }

    [Serializable]
    public class ExceptionContainer
    {
        public List<ExceptionSpec> values;

        public ExceptionContainer(List<ExceptionSpec> arg)
        {
            values = arg;
        }
    }

    public class SentryExceptionEvent : SentryEvent
    {
        public ExceptionContainer exception;

        public SentryExceptionEvent(string exceptionType,
                                      string exceptionValue,
                                      List<Breadcrumb> breadcrumbs,
                                      List<StackTraceSpec> stackTrace) : base(exceptionType, breadcrumbs)
        {
            this.exception = new ExceptionContainer(new List<ExceptionSpec> { new ExceptionSpec(exceptionType, exceptionValue, stackTrace) });
        }
    }

    [Serializable]
    public class Breadcrumb
    {
        public const int MaxBreadcrumbs = 100;

        public string timestamp;
        public string message;

        public Breadcrumb(string timestamp, string message)
        {
            this.timestamp = timestamp;
            this.message = message;
        }

        /* combine breadcrumbs from array[], start & count into List<Breadcrumb> */
        public static List<Breadcrumb> CombineBreadcrumbs(
            Breadcrumb[] breadcrumbs,
            int index,
            int number)
        {
            var res = new List<Breadcrumb>(number);
            var start = (index + MaxBreadcrumbs - number) % MaxBreadcrumbs;
            for (var i = 0; i < number; i++)
            {
                res.Add(breadcrumbs[(i + start) % MaxBreadcrumbs]);
            }
            return res;
        }
    }

    public class Dsn
    {
        private Uri _uri;

        public Uri callUri;
        public string secretKey, publicKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dsn"/> class.
        /// </summary>
        /// <param name="dsn">The DSN in the format: {PROTOCOL}://{PUBLIC_KEY}@{HOST}/{PATH}{PROJECT_ID}</param>
        /// <remarks>
        /// A legacy DSN containing a secret will also be accepted: {PROTOCOL}://{PUBLIC_KEY}:{SECRET_KEY}@{HOST}/{PATH}{PROJECT_ID}
        /// </remarks>
        public Dsn(string dsn)
        {
            if (dsn == "")
            {
                throw new ArgumentException("invalid argument - DSN cannot be empty");
            }
            _uri = new Uri(dsn);
            if (string.IsNullOrEmpty(_uri.UserInfo))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            var keys = _uri.UserInfo.Split(':');
            publicKey = keys[0];
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            secretKey = null;
            if (keys.Length > 1)
            {
                secretKey = keys[1];
            }

            var path = _uri.AbsolutePath.Substring(0, _uri.AbsolutePath.LastIndexOf('/'));
            var projectId = _uri.AbsoluteUri.Substring(_uri.AbsoluteUri.LastIndexOf('/') + 1);

            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException("Invalid DSN: A Project Id is required.");
            }

            var builder = new UriBuilder
            {
                Scheme = _uri.Scheme,
                Host = _uri.DnsSafeHost,
                Port = _uri.Port,
                Path = string.Format("{0}/api/{1}/store/", path, projectId)
            };
            callUri = builder.Uri;
        }
    }
}
