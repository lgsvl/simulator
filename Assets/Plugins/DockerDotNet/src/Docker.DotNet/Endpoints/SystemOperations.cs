using Docker.DotNet.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Docker.DotNet
{
    internal class SystemOperations : ISystemOperations
    {
        private readonly DockerClient _client;

        internal SystemOperations(DockerClient client)
        {
            this._client = client;
        }

        public Task AuthenticateAsync(AuthConfig authConfig, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (authConfig == null)
            {
                throw new ArgumentNullException(nameof(authConfig));
            }
            var data = new JsonRequestContent<AuthConfig>(authConfig, this._client.JsonSerializer);

            return this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Post, "auth", null, data, cancellationToken);
        }

        public async Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Get, "version", cancellationToken).ConfigureAwait(false);
            return this._client.JsonSerializer.DeserializeObject<VersionResponse>(response.Body);
        }

        public Task PingAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Get, "_ping", cancellationToken);
        }

        public async Task<SystemInfoResponse> GetSystemInfoAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Get, "info", cancellationToken).ConfigureAwait(false);
            return this._client.JsonSerializer.DeserializeObject<SystemInfoResponse>(response.Body);
        }

        public Task<Stream> MonitorEventsAsync(ContainerEventsParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            IQueryString queryParameters = new QueryString<ContainerEventsParameters>(parameters);
            return this._client.MakeRequestForStreamAsync(this._client.NoErrorHandlers, HttpMethod.Get, "events", queryParameters, cancellationToken);
        }

        public Task MonitorEventsAsync(ContainerEventsParameters parameters, IProgress<Message> progress, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return StreamUtil.MonitorStreamForMessagesAsync(
                MonitorEventsAsync(parameters, cancellationToken),
                this._client,
                cancellationToken,
                progress);
        }
    }
}