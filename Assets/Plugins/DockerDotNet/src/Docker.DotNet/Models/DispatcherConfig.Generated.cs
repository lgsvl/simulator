using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class DispatcherConfig // (swarm.DispatcherConfig)
    {
        [DataMember(Name = "HeartbeatPeriod", EmitDefaultValue = false)]
        public long HeartbeatPeriod { get; set; }
    }
}
