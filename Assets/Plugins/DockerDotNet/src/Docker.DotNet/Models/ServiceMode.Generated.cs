using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ServiceMode // (swarm.ServiceMode)
    {
        [DataMember(Name = "Replicated", EmitDefaultValue = false)]
        public ReplicatedService Replicated { get; set; }

        [DataMember(Name = "Global", EmitDefaultValue = false)]
        public GlobalService Global { get; set; }

        [DataMember(Name = "ReplicatedJob", EmitDefaultValue = false)]
        public ReplicatedJob ReplicatedJob { get; set; }

        [DataMember(Name = "GlobalJob", EmitDefaultValue = false)]
        public GlobalJob GlobalJob { get; set; }
    }
}
