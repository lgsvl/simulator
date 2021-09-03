using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NetworkSettings // (types.NetworkSettings)
    {
        public NetworkSettings()
        {
        }

        public NetworkSettings(NetworkSettingsBase NetworkSettingsBase, DefaultNetworkSettings DefaultNetworkSettings)
        {
            if (NetworkSettingsBase != null)
            {
                this.Bridge = NetworkSettingsBase.Bridge;
                this.SandboxID = NetworkSettingsBase.SandboxID;
                this.HairpinMode = NetworkSettingsBase.HairpinMode;
                this.LinkLocalIPv6Address = NetworkSettingsBase.LinkLocalIPv6Address;
                this.LinkLocalIPv6PrefixLen = NetworkSettingsBase.LinkLocalIPv6PrefixLen;
                this.Ports = NetworkSettingsBase.Ports;
                this.SandboxKey = NetworkSettingsBase.SandboxKey;
                this.SecondaryIPAddresses = NetworkSettingsBase.SecondaryIPAddresses;
                this.SecondaryIPv6Addresses = NetworkSettingsBase.SecondaryIPv6Addresses;
            }

            if (DefaultNetworkSettings != null)
            {
                this.EndpointID = DefaultNetworkSettings.EndpointID;
                this.Gateway = DefaultNetworkSettings.Gateway;
                this.GlobalIPv6Address = DefaultNetworkSettings.GlobalIPv6Address;
                this.GlobalIPv6PrefixLen = DefaultNetworkSettings.GlobalIPv6PrefixLen;
                this.IPAddress = DefaultNetworkSettings.IPAddress;
                this.IPPrefixLen = DefaultNetworkSettings.IPPrefixLen;
                this.IPv6Gateway = DefaultNetworkSettings.IPv6Gateway;
                this.MacAddress = DefaultNetworkSettings.MacAddress;
            }
        }

        [DataMember(Name = "Bridge", EmitDefaultValue = false)]
        public string Bridge { get; set; }

        [DataMember(Name = "SandboxID", EmitDefaultValue = false)]
        public string SandboxID { get; set; }

        [DataMember(Name = "HairpinMode", EmitDefaultValue = false)]
        public bool HairpinMode { get; set; }

        [DataMember(Name = "LinkLocalIPv6Address", EmitDefaultValue = false)]
        public string LinkLocalIPv6Address { get; set; }

        [DataMember(Name = "LinkLocalIPv6PrefixLen", EmitDefaultValue = false)]
        public long LinkLocalIPv6PrefixLen { get; set; }

        [DataMember(Name = "Ports", EmitDefaultValue = false)]
        public IDictionary<string, IList<PortBinding>> Ports { get; set; }

        [DataMember(Name = "SandboxKey", EmitDefaultValue = false)]
        public string SandboxKey { get; set; }

        [DataMember(Name = "SecondaryIPAddresses", EmitDefaultValue = false)]
        public IList<Address> SecondaryIPAddresses { get; set; }

        [DataMember(Name = "SecondaryIPv6Addresses", EmitDefaultValue = false)]
        public IList<Address> SecondaryIPv6Addresses { get; set; }

        [DataMember(Name = "EndpointID", EmitDefaultValue = false)]
        public string EndpointID { get; set; }

        [DataMember(Name = "Gateway", EmitDefaultValue = false)]
        public string Gateway { get; set; }

        [DataMember(Name = "GlobalIPv6Address", EmitDefaultValue = false)]
        public string GlobalIPv6Address { get; set; }

        [DataMember(Name = "GlobalIPv6PrefixLen", EmitDefaultValue = false)]
        public long GlobalIPv6PrefixLen { get; set; }

        [DataMember(Name = "IPAddress", EmitDefaultValue = false)]
        public string IPAddress { get; set; }

        [DataMember(Name = "IPPrefixLen", EmitDefaultValue = false)]
        public long IPPrefixLen { get; set; }

        [DataMember(Name = "IPv6Gateway", EmitDefaultValue = false)]
        public string IPv6Gateway { get; set; }

        [DataMember(Name = "MacAddress", EmitDefaultValue = false)]
        public string MacAddress { get; set; }

        [DataMember(Name = "Networks", EmitDefaultValue = false)]
        public IDictionary<string, EndpointSettings> Networks { get; set; }
    }
}
