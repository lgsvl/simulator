using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Endpoint // (swarm.Endpoint)
    {
        [DataMember(Name = "Spec", EmitDefaultValue = false)]
        public EndpointSpec Spec { get; set; }

        [DataMember(Name = "Ports", EmitDefaultValue = false)]
        public IList<PortConfig> Ports { get; set; }

        [DataMember(Name = "VirtualIPs", EmitDefaultValue = false)]
        public IList<EndpointVirtualIP> VirtualIPs { get; set; }
    }
}
