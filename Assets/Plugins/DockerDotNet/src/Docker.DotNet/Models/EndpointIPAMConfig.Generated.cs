using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class EndpointIPAMConfig // (network.EndpointIPAMConfig)
    {
        [DataMember(Name = "IPv4Address", EmitDefaultValue = false)]
        public string IPv4Address { get; set; }

        [DataMember(Name = "IPv6Address", EmitDefaultValue = false)]
        public string IPv6Address { get; set; }

        [DataMember(Name = "LinkLocalIPs", EmitDefaultValue = false)]
        public IList<string> LinkLocalIPs { get; set; }
    }
}
