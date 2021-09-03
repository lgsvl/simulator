using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmUpdateConfig // (swarm.UpdateConfig)
    {
        [DataMember(Name = "Parallelism", EmitDefaultValue = false)]
        public ulong Parallelism { get; set; }

        [DataMember(Name = "Delay", EmitDefaultValue = false)]
        public long Delay { get; set; }

        [DataMember(Name = "FailureAction", EmitDefaultValue = false)]
        public string FailureAction { get; set; }

        [DataMember(Name = "Monitor", EmitDefaultValue = false)]
        public long Monitor { get; set; }

        [DataMember(Name = "MaxFailureRatio", EmitDefaultValue = false)]
        public float MaxFailureRatio { get; set; }

        [DataMember(Name = "Order", EmitDefaultValue = false)]
        public string Order { get; set; }
    }
}
