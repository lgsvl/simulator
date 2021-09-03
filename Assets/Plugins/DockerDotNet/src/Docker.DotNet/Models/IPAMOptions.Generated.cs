using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class IPAMOptions // (swarm.IPAMOptions)
    {
        [DataMember(Name = "Driver", EmitDefaultValue = false)]
        public SwarmDriver Driver { get; set; }

        [DataMember(Name = "Configs", EmitDefaultValue = false)]
        public IList<SwarmIPAMConfig> Configs { get; set; }
    }
}
