using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class EndpointSpec // (swarm.EndpointSpec)
    {
        [DataMember(Name = "Mode", EmitDefaultValue = false)]
        public string Mode { get; set; }

        [DataMember(Name = "Ports", EmitDefaultValue = false)]
        public IList<PortConfig> Ports { get; set; }
    }
}
