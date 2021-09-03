using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class JobStatus // (swarm.JobStatus)
    {
        [DataMember(Name = "JobIteration", EmitDefaultValue = false)]
        public Version JobIteration { get; set; }

        [DataMember(Name = "LastExecution", EmitDefaultValue = false)]
        public DateTime LastExecution { get; set; }
    }
}
