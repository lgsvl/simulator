using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class TaskDefaults // (swarm.TaskDefaults)
    {
        [DataMember(Name = "LogDriver", EmitDefaultValue = false)]
        public SwarmDriver LogDriver { get; set; }
    }
}
