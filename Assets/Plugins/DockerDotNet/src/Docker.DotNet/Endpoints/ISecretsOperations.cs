using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace Docker.DotNet
{
    public interface ISecretsOperations
    {
        /// <summary>
        /// List secrets
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<IList<Secret>> ListAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Create a secret
        /// </summary>
        /// <remarks>
        /// 201 - No error.
        /// 406 - Server error or node is not part of a swarm.
        /// 409 - Name conflicts with an existing object.
        /// 500 - Server error.
        /// </remarks>
        Task<SecretCreateResponse> CreateAsync(SecretSpec body, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inspect a secret
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 404 - Secret not found.
        /// 406 - Node is not part of a swarm.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="id">ID of the secret.</param>
        Task<Secret> InspectAsync(string id, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete a secret
        /// </summary>
        /// <remarks>
        /// 204 - No error.
        /// 404 - Secret not found.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="id">ID of the secret.</param>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default(CancellationToken));
    }
}