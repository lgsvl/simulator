using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SystemInfoResponse // (types.Info)
    {
        [DataMember(Name = "ID", EmitDefaultValue = false)]
        public string ID { get; set; }

        [DataMember(Name = "Containers", EmitDefaultValue = false)]
        public long Containers { get; set; }

        [DataMember(Name = "ContainersRunning", EmitDefaultValue = false)]
        public long ContainersRunning { get; set; }

        [DataMember(Name = "ContainersPaused", EmitDefaultValue = false)]
        public long ContainersPaused { get; set; }

        [DataMember(Name = "ContainersStopped", EmitDefaultValue = false)]
        public long ContainersStopped { get; set; }

        [DataMember(Name = "Images", EmitDefaultValue = false)]
        public long Images { get; set; }

        [DataMember(Name = "Driver", EmitDefaultValue = false)]
        public string Driver { get; set; }

        [DataMember(Name = "DriverStatus", EmitDefaultValue = false)]
        public IList<string[]> DriverStatus { get; set; }

        [DataMember(Name = "SystemStatus", EmitDefaultValue = false)]
        public IList<string[]> SystemStatus { get; set; }

        [DataMember(Name = "Plugins", EmitDefaultValue = false)]
        public PluginsInfo Plugins { get; set; }

        [DataMember(Name = "MemoryLimit", EmitDefaultValue = false)]
        public bool MemoryLimit { get; set; }

        [DataMember(Name = "SwapLimit", EmitDefaultValue = false)]
        public bool SwapLimit { get; set; }

        [DataMember(Name = "KernelMemory", EmitDefaultValue = false)]
        public bool KernelMemory { get; set; }

        [DataMember(Name = "KernelMemoryTCP", EmitDefaultValue = false)]
        public bool KernelMemoryTCP { get; set; }

        [DataMember(Name = "CpuCfsPeriod", EmitDefaultValue = false)]
        public bool CPUCfsPeriod { get; set; }

        [DataMember(Name = "CpuCfsQuota", EmitDefaultValue = false)]
        public bool CPUCfsQuota { get; set; }

        [DataMember(Name = "CPUShares", EmitDefaultValue = false)]
        public bool CPUShares { get; set; }

        [DataMember(Name = "CPUSet", EmitDefaultValue = false)]
        public bool CPUSet { get; set; }

        [DataMember(Name = "PidsLimit", EmitDefaultValue = false)]
        public bool PidsLimit { get; set; }

        [DataMember(Name = "IPv4Forwarding", EmitDefaultValue = false)]
        public bool IPv4Forwarding { get; set; }

        [DataMember(Name = "BridgeNfIptables", EmitDefaultValue = false)]
        public bool BridgeNfIptables { get; set; }

        [DataMember(Name = "BridgeNfIp6tables", EmitDefaultValue = false)]
        public bool BridgeNfIP6tables { get; set; }

        [DataMember(Name = "Debug", EmitDefaultValue = false)]
        public bool Debug { get; set; }

        [DataMember(Name = "NFd", EmitDefaultValue = false)]
        public long NFd { get; set; }

        [DataMember(Name = "OomKillDisable", EmitDefaultValue = false)]
        public bool OomKillDisable { get; set; }

        [DataMember(Name = "NGoroutines", EmitDefaultValue = false)]
        public long NGoroutines { get; set; }

        [DataMember(Name = "SystemTime", EmitDefaultValue = false)]
        public string SystemTime { get; set; }

        [DataMember(Name = "LoggingDriver", EmitDefaultValue = false)]
        public string LoggingDriver { get; set; }

        [DataMember(Name = "CgroupDriver", EmitDefaultValue = false)]
        public string CgroupDriver { get; set; }

        [DataMember(Name = "CgroupVersion", EmitDefaultValue = false)]
        public string CgroupVersion { get; set; }

        [DataMember(Name = "NEventsListener", EmitDefaultValue = false)]
        public long NEventsListener { get; set; }

        [DataMember(Name = "KernelVersion", EmitDefaultValue = false)]
        public string KernelVersion { get; set; }

        [DataMember(Name = "OperatingSystem", EmitDefaultValue = false)]
        public string OperatingSystem { get; set; }

        [DataMember(Name = "OSVersion", EmitDefaultValue = false)]
        public string OSVersion { get; set; }

        [DataMember(Name = "OSType", EmitDefaultValue = false)]
        public string OSType { get; set; }

        [DataMember(Name = "Architecture", EmitDefaultValue = false)]
        public string Architecture { get; set; }

        [DataMember(Name = "IndexServerAddress", EmitDefaultValue = false)]
        public string IndexServerAddress { get; set; }

        [DataMember(Name = "RegistryConfig", EmitDefaultValue = false)]
        public ServiceConfig RegistryConfig { get; set; }

        [DataMember(Name = "NCPU", EmitDefaultValue = false)]
        public long NCPU { get; set; }

        [DataMember(Name = "MemTotal", EmitDefaultValue = false)]
        public long MemTotal { get; set; }

        [DataMember(Name = "GenericResources", EmitDefaultValue = false)]
        public IList<GenericResource> GenericResources { get; set; }

        [DataMember(Name = "DockerRootDir", EmitDefaultValue = false)]
        public string DockerRootDir { get; set; }

        [DataMember(Name = "HttpProxy", EmitDefaultValue = false)]
        public string HTTPProxy { get; set; }

        [DataMember(Name = "HttpsProxy", EmitDefaultValue = false)]
        public string HTTPSProxy { get; set; }

        [DataMember(Name = "NoProxy", EmitDefaultValue = false)]
        public string NoProxy { get; set; }

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IList<string> Labels { get; set; }

        [DataMember(Name = "ExperimentalBuild", EmitDefaultValue = false)]
        public bool ExperimentalBuild { get; set; }

        [DataMember(Name = "ServerVersion", EmitDefaultValue = false)]
        public string ServerVersion { get; set; }

        [DataMember(Name = "ClusterStore", EmitDefaultValue = false)]
        public string ClusterStore { get; set; }

        [DataMember(Name = "ClusterAdvertise", EmitDefaultValue = false)]
        public string ClusterAdvertise { get; set; }

        [DataMember(Name = "Runtimes", EmitDefaultValue = false)]
        public IDictionary<string, Runtime> Runtimes { get; set; }

        [DataMember(Name = "DefaultRuntime", EmitDefaultValue = false)]
        public string DefaultRuntime { get; set; }

        [DataMember(Name = "Swarm", EmitDefaultValue = false)]
        public Info Swarm { get; set; }

        [DataMember(Name = "LiveRestoreEnabled", EmitDefaultValue = false)]
        public bool LiveRestoreEnabled { get; set; }

        [DataMember(Name = "Isolation", EmitDefaultValue = false)]
        public string Isolation { get; set; }

        [DataMember(Name = "InitBinary", EmitDefaultValue = false)]
        public string InitBinary { get; set; }

        [DataMember(Name = "ContainerdCommit", EmitDefaultValue = false)]
        public Commit ContainerdCommit { get; set; }

        [DataMember(Name = "RuncCommit", EmitDefaultValue = false)]
        public Commit RuncCommit { get; set; }

        [DataMember(Name = "InitCommit", EmitDefaultValue = false)]
        public Commit InitCommit { get; set; }

        [DataMember(Name = "SecurityOptions", EmitDefaultValue = false)]
        public IList<string> SecurityOptions { get; set; }

        [DataMember(Name = "ProductLicense", EmitDefaultValue = false)]
        public string ProductLicense { get; set; }

        [DataMember(Name = "DefaultAddressPools", EmitDefaultValue = false)]
        public IList<NetworkAddressPool> DefaultAddressPools { get; set; }

        [DataMember(Name = "Warnings", EmitDefaultValue = false)]
        public IList<string> Warnings { get; set; }
    }
}
