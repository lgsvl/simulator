using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmResources // (swarm.Resources)
    {
        [DataMember(Name = "NanoCPUs", EmitDefaultValue = false)]
        public long NanoCPUs { get; set; }

        [DataMember(Name = "MemoryBytes", EmitDefaultValue = false)]
        public long MemoryBytes { get; set; }

        [DataMember(Name = "GenericResources", EmitDefaultValue = false)]
        public IList<GenericResource> GenericResources { get; set; }
    }
}
