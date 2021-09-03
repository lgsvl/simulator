using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerExecStartParameters // (main.ContainerExecStartParameters)
    {
        [DataMember(Name = "User", EmitDefaultValue = false)]
        public string User { get; set; }

        [DataMember(Name = "Privileged", EmitDefaultValue = false)]
        public bool Privileged { get; set; }

        [DataMember(Name = "Tty", EmitDefaultValue = false)]
        public bool Tty { get; set; }

        [DataMember(Name = "AttachStdin", EmitDefaultValue = false)]
        public bool AttachStdin { get; set; }

        [DataMember(Name = "AttachStderr", EmitDefaultValue = false)]
        public bool AttachStderr { get; set; }

        [DataMember(Name = "AttachStdout", EmitDefaultValue = false)]
        public bool AttachStdout { get; set; }

        [DataMember(Name = "Detach", EmitDefaultValue = false)]
        public bool Detach { get; set; }

        [DataMember(Name = "DetachKeys", EmitDefaultValue = false)]
        public string DetachKeys { get; set; }

        [DataMember(Name = "Env", EmitDefaultValue = false)]
        public IList<string> Env { get; set; }

        [DataMember(Name = "WorkingDir", EmitDefaultValue = false)]
        public string WorkingDir { get; set; }

        [DataMember(Name = "Cmd", EmitDefaultValue = false)]
        public IList<string> Cmd { get; set; }
    }
}
