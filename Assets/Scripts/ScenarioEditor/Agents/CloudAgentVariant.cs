/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Database;
    using ICSharpCode.SharpZipLib.Zip;
    using UnityEngine;
    using Web;

    /// <summary>
    /// Data describing a single agent variant of the scenario agent type that is available from the clous
    /// </summary>
    public class CloudAgentVariant : AgentVariant
    {
        /// <summary>
        /// Guid of the vehicle
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
        /// <param name="guid">Guid of the vehicle</param>
        /// <param name="name">User friendly name of the vehicle</param>
        /// <param name="assetGuid">Guid of the asset loaded within this vehicle</param>
        public CloudAgentVariant(string guid, string name, string assetGuid)
        {
            this.guid = guid;
            this.name = name;
            this.assetGuid = assetGuid;
        }
        /// <summary>
        /// Loads the vehicle prefab from database for the selected vehicle model data
        /// </summary>
        /// <exception cref="Exception">Generic exception of the database loading process</exception>
        /// <exception cref="ArgumentException">Invalid prefab path in the vehicle model</exception>
        public void AcquirePrefab()
        {
            var bundlePath = assetModel.LocalPath;
            using (ZipFile zip = new ZipFile(bundlePath))
            {
                Manifest manifest;
                ZipEntry entry = zip.GetEntry("manifest.json");
                using (var ms = zip.GetInputStream(entry))
                {
                    int streamSize = (int) entry.Size;
                    byte[] buffer = new byte[streamSize];
                    streamSize = ms.Read(buffer, 0, streamSize);
                    manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(Encoding.UTF8.GetString(buffer));
                }

                if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Vehicle])
                {
                    throw new Exception(
                        "Out of date Vehicle AssetBundle. Please check content website for updated bundle or rebuild the bundle.");
                }

                AssetBundle textureBundle = null;

                if (zip.FindEntry($"{manifest.assetGuid}_vehicle_textures", true) != -1)
                {
                    var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_textures"));
                    textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                }

                string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                    ? "windows"
                    : "linux";
                var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_main_{platform}"));
                var vehicleBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                if (vehicleBundle == null)
                {
                    throw new Exception($"Failed to load vehicle {name} from '{bundlePath}' asset bundle");
                }

                try
                {
                    var vehicleAssets = vehicleBundle.GetAllAssetNames();
                    if (vehicleAssets.Length != 1)
                    {
                        throw new Exception($"Unsupported '{bundlePath}' vehicle asset bundle, only 1 asset expected");
                    }

                    if (!AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
                    {
                        textureBundle?.LoadAllAssets();
                    }

                    prefab = vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                }
                finally
                {
                    textureBundle?.Unload(false);
                    vehicleBundle.Unload(false);
                }
            }
        }

        /// <summary>
        /// Downloads required asset from the cloud
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public async Task DownloadAsset()
        {
            assetModel = await DownloadManager.GetAsset(BundleConfig.BundleTypes.Vehicle, assetGuid, name);
            AcquirePrefab();
        }

        /// <inheritdoc/>
        public override async Task Prepare()
        {
            await DownloadAsset();
            await base.Prepare();
        }
    }
}