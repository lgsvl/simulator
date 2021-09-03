using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace Docker.DotNet
{
    internal class PluginOperations : IPluginOperations
    {
        internal static readonly ApiResponseErrorHandlingDelegate NoSuchPluginHandler = (statusCode, responseBody) =>
        {
            if (statusCode == HttpStatusCode.NotFound)
            {
                throw new DockerPluginNotFoundException(statusCode, responseBody);
            }
        };

        private readonly DockerClient _client;
        private const string TarContentType = "application/x-tar";

        internal PluginOperations(DockerClient client)
        {
            this._client = client;
        }

        public async Task<IList<Plugin>> ListPluginsAsync(PluginListParameters parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            IQueryString queryParameters = parameters == null ? null : new QueryString<PluginListParameters>(parameters);
            var response = await this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Get, "plugins", queryParameters, cancellationToken).ConfigureAwait(false);
            return this._client.JsonSerializer.DeserializeObject<Plugin[]>(response.Body);
        }

        public async Task<IList<PluginPrivilege>> GetPluginPrivilegesAsync(PluginGetPrivilegeParameters parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var query = new QueryString<PluginGetPrivilegeParameters>(parameters);
            var response = await this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Get, "plugins/privileges", query, cancellationToken).ConfigureAwait(false);
            return this._client.JsonSerializer.DeserializeObject<PluginPrivilege[]>(response.Body);
        }

        public Task InstallPluginAsync(PluginInstallParameters parameters, IProgress<JSONMessage> progress, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.Privileges == null)
            {
                throw new ArgumentNullException(nameof(parameters.Privileges));
            }

            var data = new JsonRequestContent<IList<PluginPrivilege>>(parameters.Privileges, this._client.JsonSerializer);

            IQueryString queryParameters = new QueryString<PluginInstallParameters>(parameters);
            return StreamUtil.MonitorStreamForMessagesAsync(
                this._client.MakeRequestForStreamAsync(this._client.NoErrorHandlers, HttpMethod.Post, $"plugins/pull", queryParameters, data, null, CancellationToken.None),
                this._client,
                cancellationToken,
                progress);
        }

        public async Task<Plugin> InspectPluginAsync(string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var response = await this._client.MakeRequestAsync(new[] { NoSuchPluginHandler }, HttpMethod.Get, $"plugins/{name}/json", cancellationToken);
            return this._client.JsonSerializer.DeserializeObject<Plugin>(response.Body);
        }

        public Task RemovePluginAsync(string name, PluginRemoveParameters parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            IQueryString queryParameters = parameters == null ? null : new QueryString<PluginRemoveParameters>(parameters);
            return this._client.MakeRequestAsync(new[] { NoSuchPluginHandler }, HttpMethod.Delete, $"plugins/{name}", queryParameters, cancellationToken);
        }

        public Task EnablePluginAsync(string name, PluginEnableParameters parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            IQueryString queryParameters = parameters == null ? null : new QueryString<PluginEnableParameters>(parameters);
            return this._client.MakeRequestAsync(new[] { NoSuchPluginHandler }, HttpMethod.Post, $"plugins/{name}/enable", queryParameters, cancellationToken);
        }

        public Task DisablePluginAsync(string name, PluginDisableParameters parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            IQueryString queryParameters = parameters == null ? null : new QueryString<PluginDisableParameters>(parameters);
            return this._client.MakeRequestAsync(new[] { NoSuchPluginHandler }, HttpMethod.Post, $"plugins/{name}/disable", queryParameters, cancellationToken);
        }

        public Task UpgradePluginAsync(string name, PluginUpgradeParameters parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.Privileges == null)
            {
                throw new ArgumentNullException(nameof(parameters.Privileges));
            }

            var query = new QueryString<PluginUpgradeParameters>(parameters);
            var data = new JsonRequestContent<IList<PluginPrivilege>>(parameters.Privileges, this._client.JsonSerializer);
            return this._client.MakeRequestAsync(new[] { NoSuchPluginHandler }, HttpMethod.Post, $"plugins/{name}/upgrade", query, data, cancellationToken);
        }

        public Task CreatePluginAsync(PluginCreateParameters parameters, Stream plugin, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            var query = new QueryString<PluginCreateParameters>(parameters);
            var data = new BinaryRequestContent(plugin, TarContentType);
            return this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Post, $"plugins/create", query, data, cancellationToken);
        }

        public Task PushPluginAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return this._client.MakeRequestAsync(new[] { NoSuchPluginHandler }, HttpMethod.Post, $"plugins/{name}/push", cancellationToken);
        }

        public Task ConfigurePluginAsync(string name, PluginConfigureParameters parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.Args == null)
            {
                throw new ArgumentNullException(nameof(parameters.Args));
            }

            var body = new JsonRequestContent<IList<string>>(parameters.Args, this._client.JsonSerializer);
            return this._client.MakeRequestAsync(new[] { NoSuchPluginHandler }, HttpMethod.Post, $"plugins/{name}/set", null, body, cancellationToken);
        }
    }
}
