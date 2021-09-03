using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System.Threading;

namespace Docker.DotNet
{
    public interface ISwarmOperations
    {
        #region Swarm

        /// <summary>
        /// Get the unlock key.
        /// </summary>
        /// <remarks>
        /// 200 - No Error.
        /// 500 - Server Error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        Task<SwarmUnlockResponse> GetSwarmUnlockKeyAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Initialize a new swarm.
        /// </summary>
        /// <remarks>
        /// 200 - No Error.
        /// 400 - Bad parameters.
        /// 500 - Server Error.
        /// 503 - Node is already part of a swarm.
        /// </remarks>
        /// <param name="parameters">The join parameters.</param>
        /// <returns>The node id.</returns>
        Task<string> InitSwarmAsync(SwarmInitParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inspect swarm.
        /// </summary>
        /// <remarks>
        /// 200 - No Error.
        /// 404 - No such swarm.
        /// 500 - Server Error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        Task<SwarmInspectResponse> InspectSwarmAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Join an existing swarm.
        /// </summary>
        /// <remarks>
        /// 200 - No Error.
        /// 500 - Server Error.
        /// 503 - Node is already part of a swarm.
        /// </remarks>
        /// <param name="parameters">The join parameters.</param>
        Task JoinSwarmAsync(SwarmJoinParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Leave a swarm.
        /// </summary>
        /// <remarks>
        /// 200 - No Error.
        /// 500 - Server Error.
        /// 503 - Node not part of a swarm.
        /// </remarks>
        /// <param name="parameters">The leave parameters.</param>
        Task LeaveSwarmAsync(SwarmLeaveParameters parameters = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Unlock a locked manager.
        /// </summary>
        /// <remarks>
        /// 200 - No Error.
        /// 500 - Server Error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        /// <param name="parameters">The swarm's unlock key.</param>
        Task UnlockSwarmAsync(SwarmUnlockParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Update a swarm.
        /// </summary>
        /// <remarks>
        /// 200 - No Error.
        /// 400 - Bad parameter.
        /// 500 - Server Error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        /// <param name="parameters">The update parameters.</param>
        Task UpdateSwarmAsync(SwarmUpdateParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        #endregion Swarm

        #region Services

        /// <summary>
        /// Create a service.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 400 - Bad parameter.
        /// 403 - Network is not eligible for services.
        /// 409 - Name conflicts with an existing service.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        Task<ServiceCreateResponse> CreateServiceAsync(ServiceCreateParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inspect a service.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 404 - No such service.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        /// <param name="id">ID or name of service.</param>
        /// <returns>The service spec.</returns>
        Task<SwarmService> InspectServiceAsync(string id, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// List services with optional serviceFilters.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        Task<IEnumerable<SwarmService>> ListServicesAsync(ServicesListParameters parameters = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Update a service.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 400 - Bad parameter.
        /// 404 - No such service.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        /// <param name="id">ID or name of service.</param>
        /// <returns>The service spec.</returns>
        Task<ServiceUpdateResponse> UpdateServiceAsync(string id, ServiceUpdateParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete a service.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 404 - No such service.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        /// <param name="id">ID or name of service.</param>
        Task RemoveServiceAsync(string id, CancellationToken cancellationToken = default(CancellationToken));

        #endregion Services

        #region Nodes

        /// <summary>
        /// List nodes.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        /// <returns></returns>
        Task<IEnumerable<NodeListResponse>> ListNodesAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inspect a node.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 404 - No such node.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        Task<NodeListResponse> InspectNodeAsync(string id, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete a node.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 404 - No such node.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        Task RemoveNodeAsync(string id, bool force, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Update node.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 404 - No such node.
        /// 500 - Server error.
        /// 503 - Node is not part of a swarm.
        /// </remarks>
        /// <param name="id">ID or name of the node.</param>
        /// <param name="version">Current version of the node object.</param>
        /// <param name="parameters">Parameters to update.</param>
        Task UpdateNodeAsync(string id, ulong version, NodeUpdateParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        #endregion
    }
}