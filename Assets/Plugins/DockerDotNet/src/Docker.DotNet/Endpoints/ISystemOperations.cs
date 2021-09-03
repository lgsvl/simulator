using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System;

namespace Docker.DotNet
{
    public interface ISystemOperations
    {
        /// <summary>
        /// Check auth configuration.
        /// 
        /// Validate credentials for a registry and, if available, get an identity token for accessing the registry without password.
        /// </summary>
        /// <remarks>
        /// 200 - An identity token was generated successfully.
        /// 204 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task AuthenticateAsync(AuthConfig authConfig, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get version.
        /// 
        /// Returns the version of Docker that is running and various information about the system that Docker is running on.
        /// </summary>
        /// <remarks>
        /// docker version
        ///
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Ping.
        /// 
        /// This is a dummy endpoint you can use to test if the server is accessible.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task PingAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get system information.
        /// </summary>
        /// <remarks>
        /// docker info
        ///
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<SystemInfoResponse> GetSystemInfoAsync(CancellationToken cancellationToken = default(CancellationToken));

        [Obsolete("Use 'Task MonitorEventsAsync(ContainerEventsParameters parameters, CancellationToken cancellationToken, IProgress<JSONMessage> progress)'")]
        Task<Stream> MonitorEventsAsync(ContainerEventsParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Monitor events.
        /// 
        /// Stream real-time events from the server.
        ///
        /// Various objects within Docker report events when something happens to them.
        ///
        /// Containers report these events: {attach, commit, copy, create, destroy, detach, die, exec_create, exec_detach, exec_start, export, kill, oom, pause, rename, resize, restart, start, stop, top, unpause, update}
        ///
        /// Images report these events: {delete, import, load, pull, push, save, tag, untag}
        ///
        /// Volumes report these events: {create, mount, unmount, destroy}
        ///
        /// Networks report these events: {create, connect, disconnect, destroy}
        ///
        /// The Docker daemon reports these events: {reload}
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task MonitorEventsAsync(ContainerEventsParameters parameters, IProgress<Message> progress, CancellationToken cancellationToken = default(CancellationToken));
    }
}