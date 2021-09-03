using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class TaskStatus // (swarm.TaskStatus)
    {
        [DataMember(Name = "Timestamp", EmitDefaultValue = false)]
        public DateTime Timestamp { get; set; }

        [DataMember(Name = "State", EmitDefaultValue = false)]
        public TaskState State { get; set; }

        [DataMember(Name = "Message", EmitDefaultValue = false)]
        public string Message { get; set; }

        [DataMember(Name = "Err", EmitDefaultValue = false)]
        public string Err { get; set; }

        [DataMember(Name = "ContainerStatus", EmitDefaultValue = false)]
        public ContainerStatus ContainerStatus { get; set; }

        [DataMember(Name = "PortStatus", EmitDefaultValue = false)]
        public PortStatus PortStatus { get; set; }
    }
}
