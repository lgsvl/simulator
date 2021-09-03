using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Privileges // (swarm.Privileges)
    {
        [DataMember(Name = "CredentialSpec", EmitDefaultValue = false)]
        public CredentialSpec CredentialSpec { get; set; }

        [DataMember(Name = "SELinuxContext", EmitDefaultValue = false)]
        public SELinuxContext SELinuxContext { get; set; }
    }
}
