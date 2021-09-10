/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System;
    using System.Threading.Tasks;
    using Database;
    using Managers;
    using UnityEngine;
    using Web;

    /// <summary>
    /// Data describing a single agent variant of the scenario agent type that is available from the cloud
    /// </summary>
    public class CloudAgentVariant : AgentVariant
    {
        /// <summary>
        /// Guid of the agent variant
        /// </summary>
        public readonly string guid;

        /// <summary>
        /// Guid of the asset loaded within this vehicle
        /// </summary>
        public readonly string assetGuid;

        /// <summary>
        /// Asset model of the downloaded vehicle, null if vehicle is not cached yet
        /// </summary>
        public AssetModel assetModel;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The source of the scenario agent type, this variant is a part of this source</param>
        /// <param name="name">Name of this agent variant</param>
        /// <param name="prefab">Prefab used to visualize this agent variant</param>
        /// <param name="description">Description with agent variant details</param>
        /// <param name="guid">Guid of the vehicle</param>
        /// <param name="assetGuid">Guid of the asset loaded within this vehicle</param>
        public CloudAgentVariant(ScenarioAgentSource source, string name, GameObject prefab, string description,
            string guid,
            string assetGuid) : base(source, name, prefab, description)
        {
            this.guid = guid;
            this.assetGuid = assetGuid;
        }

        /// <summary>
        /// Loads the vehicle prefab from database for the selected vehicle model data
        /// </summary>
        /// <exception cref="Exception">Generic exception of the database loading process</exception>
        /// <exception cref="ArgumentException">Invalid prefab path in the vehicle model</exception>
        public bool AcquirePrefab()
        {
            try
            {
                prefab = Loader.LoadVehicleBundle(assetModel.LocalPath);
            }
            catch (Exception ex)
            {
                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Could not load the vehicle bundle {name}. Error: \"{ex.Message}\".");
                return false;
            }

            IsPrepared = prefab != null;
            return true;
        }

        /// <summary>
        /// Downloads required asset from the cloud
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task DownloadAsset(IProgress<SourceVariant> progress)
        {
            ScenarioManager.Instance.logPanel.EnqueueInfo($"Started a download process of the {name} agent.");
            ScenarioManager.Instance.ReportAssetDownload(assetGuid);
            Progress<Tuple<string, float>> downloadProgress = null;
            if (progress != null)
            {
                downloadProgress = new Progress<Tuple<string, float>>(p =>
                {
                    PreparationProgress = p.Item2;
                    progress.Report(this);
                });
            }
            assetModel = await DownloadManager.GetAsset(BundleConfig.BundleTypes.Vehicle, assetGuid, name, downloadProgress);
            ScenarioManager.Instance.ReportAssetFinishedDownload(assetGuid);
            if (AcquirePrefab())
                ScenarioManager.Instance.logPanel.EnqueueInfo($"Agent {name} has been downloaded.");
        }

        /// <inheritdoc/>
        public override async Task Prepare(IProgress<SourceVariant> progress = null)
        {
            if (IsPrepared || IsBusy)
                return;
            IsBusy = true;
            PreparationProgress = 0.0f;
            await DownloadAsset(progress);
            await base.Prepare(progress);
            IsBusy = false;
        }
    }
}