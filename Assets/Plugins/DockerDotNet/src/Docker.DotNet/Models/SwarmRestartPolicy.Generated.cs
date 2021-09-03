using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmRestartPolicy // (swarm.RestartPolicy)
    {
        [DataMember(Name = "Condition", EmitDefaultValue = false)]
        public string Condition { get; set; }

        [DataMember(Name = "Delay", EmitDefaultValue = false)]
        public long? Delay { get; set; }

        [DataMember(Name = "MaxAttempts", EmitDefaultValue = false)]
        public ulong? MaxAttempts { get; set; }

        [DataMember(Name = "Window", EmitDefaultValue = false)]
        public long? Window { get; set; }
    }
}
