using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System.Threading;

namespace Docker.DotNet
{
    public interface IVolumeOperations
    {
        /// <summary>
        /// List volumes
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<VolumesListResponse> ListAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Create a volume.
        /// </summary>
        /// <remarks>
        /// 201 - The volume was created successfully.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="parameters">Volume parameters to create.</param>
        Task<VolumeResponse> CreateAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inspect a volume.
        /// </summary>
        /// <remarks>
        /// 200 - No error.
        /// 404 - No such volume.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">Volume name or ID.</param>
        Task<VolumeResponse> InspectAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Remove a volume.
        ///
        /// Instruct the driver to remove the volume.
        /// </summary>
        /// <remarks>
        /// 204 - The volume was removed.
        /// 404 - No such volume or volume driver.
        /// 409 - Volume is in use and cannot be removed.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">Volume name or ID.</param>
        /// <param name="force">Force the removal of the volume.</param>
        Task RemoveAsync(string name, bool? force = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete unused volumes.
        /// </summary>
        /// <remarks>
        /// HTTP POST /volumes/prune
        ///
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<VolumesPruneResponse> PruneAsync(VolumesPruneParameters parameters = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}