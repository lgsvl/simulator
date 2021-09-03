using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmLimit // (swarm.Limit)
    {
        [DataMember(Name = "NanoCPUs", EmitDefaultValue = false)]
        public long NanoCPUs { get; set; }

        [DataMember(Name = "MemoryBytes", EmitDefaultValue = false)]
        public long MemoryBytes { get; set; }

        [DataMember(Name = "Pids", EmitDefaultValue = false)]
        public long Pids { get; set; }
    }
}
