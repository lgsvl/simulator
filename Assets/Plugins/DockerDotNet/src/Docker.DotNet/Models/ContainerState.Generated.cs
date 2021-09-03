using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerState // (types.ContainerState)
    {
        [DataMember(Name = "Status", EmitDefaultValue = false)]
        public string Status { get; set; }

        [DataMember(Name = "Running", EmitDefaultValue = false)]
        public bool Running { get; set; }

        [DataMember(Name = "Paused", EmitDefaultValue = false)]
        public bool Paused { get; set; }

        [DataMember(Name = "Restarting", EmitDefaultValue = false)]
        public bool Restarting { get; set; }

        [DataMember(Name = "OOMKilled", EmitDefaultValue = false)]
        public bool OOMKilled { get; set; }

        [DataMember(Name = "Dead", EmitDefaultValue = false)]
        public bool Dead { get; set; }

        [DataMember(Name = "Pid", EmitDefaultValue = false)]
        public long Pid { get; set; }

        [DataMember(Name = "ExitCode", EmitDefaultValue = false)]
        public long ExitCode { get; set; }

        [DataMember(Name = "Error", EmitDefaultValue = false)]
        public string Error { get; set; }

        [DataMember(Name = "StartedAt", EmitDefaultValue = false)]
        public string StartedAt { get; set; }

        [DataMember(Name = "FinishedAt", EmitDefaultValue = false)]
        public string FinishedAt { get; set; }

        [DataMember(Name = "Health", EmitDefaultValue = false)]
        public Health Health { get; set; }
    }
}
