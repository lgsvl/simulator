using System;

namespace Docker.DotNet
{
    public interface IDockerClient : IDisposable
    {
        DockerClientConfiguration Configuration { get; }

        TimeSpan DefaultTimeout { get; set; }

        #region Endpoints

        IContainerOperations Containers { get; }

        IImageOperations Images { get; }

        INetworkOperations Networks { get; }

        IVolumeOperations Volumes { get; }

        ISecretsOperations Secrets { get; }

        ISwarmOperations Swarm { get; }

        ITasksOperations Tasks { get; }

        ISystemOperations System { get; }

        IPluginOperations Plugin { get; }

        IExecOperations Exec { get; }

        #endregion Endpoints
    }
}
