using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SELinuxContext // (swarm.SELinuxContext)
    {
        [DataMember(Name = "Disable", EmitDefaultValue = false)]
        public bool Disable { get; set; }

        [DataMember(Name = "User", EmitDefaultValue = false)]
        public string User { get; set; }

        [DataMember(Name = "Role", EmitDefaultValue = false)]
        public string Role { get; set; }

        [DataMember(Name = "Type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "Level", EmitDefaultValue = false)]
        public string Level { get; set; }
    }
}
