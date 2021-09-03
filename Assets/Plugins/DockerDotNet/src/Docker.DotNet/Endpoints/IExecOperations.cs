using Docker.DotNet.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Docker.DotNet
{
    public interface IExecOperations
    {
        /// <summary>
        /// Create an exec instance.
        ///
        /// Runs a command inside a running container.
        /// </summary>
        /// <remarks>
        /// docker exec
        /// docker container exec
        ///
        /// 201 - No error.
        /// 404 - No such container.
        /// 409 - Container is paused.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="id">ID or name of the container.</param>
        Task<ContainerExecCreateResponse> ExecCreateContainerAsync(string id, ContainerExecCreateParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Start an exec instance.
        ///
        /// Starts a previously set up exec instance. If detach is true, this endpoint returns immediately after starting
        /// the command. Otherwise, it sets up an interactive session with the command.
        /// </summary>
        /// <remarks>
        /// 204 - No error.
        /// 404 - No such exec instance.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="id">Exec instance ID.</param>
        Task StartContainerExecAsync(string id, CancellationToken cancellationToken = default(CancellationToken));

        Task<MultiplexedStream> StartAndAttachContainerExecAsync(string id, bool tty, CancellationToken cancellationToken = default(CancellationToken));

        Task<MultiplexedStream> StartWithConfigContainerExecAsync(string id, ContainerExecStartParameters eConfig, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Resize an exec instance.
        ///
        /// Resize the TTY session used by an exec instance. This endpoint only works if {tty} was specified as part of
        /// creating and starting the exec instance.
        /// </summary>
        /// <remarks>
        /// 201 - No error.
        /// 404 - No such exec instance.
        /// </remarks>
        /// <param name="id">Exec instance ID.</param>
        Task ResizeContainerExecTtyAsync(string id, ContainerResizeParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inspect an exec instance.
        ///
        /// Return low-level information about an exec instance.
        /// </summary>
        /// <remarks>
        /// docker inspect
        ///
        /// 200 - No error.
        /// 404 - No such exec instance.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="id">Exec instance ID.</param>
        Task<ContainerExecInspectResponse> InspectContainerExecAsync(string id, CancellationToken cancellationToken = default(CancellationToken));
    }
}
