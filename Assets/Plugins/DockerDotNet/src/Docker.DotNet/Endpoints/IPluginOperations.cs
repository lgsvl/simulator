using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System;
using System.IO;
using System.Threading;

namespace Docker.DotNet
{
    public interface IPluginOperations
    {
        /// <summary>
        /// List plugins.
        ///
        /// Returns information about installed plugins.
        /// </summary>
        /// <remarks>
        /// docker plugin ls
        ///
        /// HTTP GET /plugins
        ///
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<IList<Plugin>> ListPluginsAsync(PluginListParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get plugin privileges.
        /// </summary>
        /// <remarks>
        /// docker plugin privileges
        ///
        /// HTTP POST /plugins/privileges
        ///
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task<IList<PluginPrivilege>> GetPluginPrivilegesAsync(PluginGetPrivilegeParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Install a plugin.
        ///
        /// Pulls and installs a plugin. After the plugin is installed, it can be enabled using the `POST /plugins/{name}/enable` endpoint.
        /// </summary>
        /// <remarks>
        /// docker plugin pull
        ///
        /// HTTP POST /plugins/pull
        ///
        /// 204 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task InstallPluginAsync(PluginInstallParameters parameters, IProgress<JSONMessage> progress, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inspect a plugin.
        /// </summary>
        /// <remarks>
        /// docker plugin inspect
        ///
        /// HTTP GET /plugins/{name}/json
        ///
        /// 200 - No error.
        /// 404 - Plugin not installed.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">The name of the plugin. The `:latest` tag is optional, and is the default if omitted.</param>
        Task<Plugin> InspectPluginAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Remove a plugin.
        /// </summary>
        /// <remarks>
        /// docker plugin rm
        ///
        /// HTTP DELETE /plugins/{name}
        ///
        /// 200 - No error.
        /// 404 - Plugin not installed.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">The name of the plugin. The `:latest` tag is optional, and is the default if omitted.</param>
        Task RemovePluginAsync(string name, PluginRemoveParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Enable a plugin.
        /// </summary>
        /// <remarks>
        /// docker plugin enable
        ///
        /// HTTP POST /plugins/{name}/enable
        ///
        /// 200 - No error.
        /// 404 - Plugin not installed.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">The name of the plugin. The `:latest` tag is optional, and is the default if omitted.</param>
        Task EnablePluginAsync(string name, PluginEnableParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Disable a plugin.
        /// </summary>
        /// <remarks>
        /// docker plugin disable
        ///
        /// HTTP POST /plugins/{name}/disable
        ///
        /// 200 - No error.
        /// 404 - Plugin not installed.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">The name of the plugin. The `:latest` tag is optional, and is the default if omitted.</param>
        Task DisablePluginAsync(string name, PluginDisableParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Upgrade a plugin.
        /// </summary>
        /// <remarks>
        /// docker plugin upgrade
        ///
        /// HTTP POST /plugins/{name}/upgrade
        ///
        /// 200 - No error.
        /// 404 - Plugin not installed.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">The name of the plugin. The `:latest` tag is optional, and is the default if omitted.</param>
        Task UpgradePluginAsync(string name, PluginUpgradeParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Create a plugin.
        /// </summary>
        /// <remarks>
        /// docker plugin create
        ///
        /// HTTP POST /plugins/create
        ///
        /// 200 - No error.
        /// 500 - Server error.
        /// </remarks>
        Task CreatePluginAsync(PluginCreateParameters parameters, Stream plugin, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Push a plugin.
        ///
        /// Push a plugin to the registry.
        /// </summary>
        /// <remarks>
        /// docker plugin push
        ///
        /// HTTP POST /plugins/{name}/push
        ///
        /// 200 - No error.
        /// 404 - Plugin not installed.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">The name of the plugin. The `:latest` tag is optional, and is the default if omitted.</param>
        Task PushPluginAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Configure a plugin.
        /// </summary>
        /// <remarks>
        /// docker plugin set
        ///
        /// HTTP POST /plugins/{name}/set
        ///
        /// 204 - No error.
        /// 404 - Plugin not installed.
        /// 500 - Server error.
        /// </remarks>
        /// <param name="name">The name of the plugin. The `:latest` tag is optional, and is the default if omitted.</param>
        Task ConfigurePluginAsync(string name, PluginConfigureParameters parameters, CancellationToken cancellationToken = default(CancellationToken));
    }
}
