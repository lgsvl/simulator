using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class OrchestrationConfig // (swarm.OrchestrationConfig)
    {
        [DataMember(Name = "TaskHistoryRetentionLimit", EmitDefaultValue = false)]
        public long? TaskHistoryRetentionLimit { get; set; }
    }
}
