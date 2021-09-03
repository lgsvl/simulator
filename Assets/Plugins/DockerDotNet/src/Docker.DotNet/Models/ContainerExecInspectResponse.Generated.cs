using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerExecInspectResponse // (types.ContainerExecInspect)
    {
        [DataMember(Name = "ID", EmitDefaultValue = false)]
        public string ExecID { get; set; }

        [DataMember(Name = "ContainerID", EmitDefaultValue = false)]
        public string ContainerID { get; set; }

        [DataMember(Name = "Running", EmitDefaultValue = false)]
        public bool Running { get; set; }

        [DataMember(Name = "ExitCode", EmitDefaultValue = false)]
        public long ExitCode { get; set; }

        [DataMember(Name = "Pid", EmitDefaultValue = false)]
        public long Pid { get; set; }
    }
}
