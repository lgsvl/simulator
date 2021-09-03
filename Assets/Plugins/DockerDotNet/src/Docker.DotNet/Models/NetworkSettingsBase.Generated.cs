using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NetworkSettingsBase // (types.NetworkSettingsBase)
    {
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
    }
}
