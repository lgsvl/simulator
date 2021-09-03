using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace Docker.DotNet
{
    /// <summary>
    /// A task is a container running on a swarm. It is the atomic scheduling unit of swarm. Swarm mode must be enabled for these endpoints to work.
    /// </summary>
    public interface ITasksOperations
    {
        /// <summary>
        /// List tasks
        /// </summary>
        /// <remarks>
        /// HTTP GET /tasks
        ///
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<IList<TaskResponse>> ListAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// List tasks
        /// </summary>
        /// <remarks>
        /// HTTP GET /tasks
        ///
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<IList<TaskResponse>> ListAsync(TasksListParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inspect a task
        /// </summary>
        /// <remarks>
        /// HTTP GET /tasks/{id}
        ///
        /// 200 - No error.
        /// 404 - No such task.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="id">ID of the task.</param>
        Task<TaskResponse> InspectAsync(string id, CancellationToken cancellationToken = default(CancellationToken));
    }
}