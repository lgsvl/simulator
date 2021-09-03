using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ConfigReferenceFileTarget // (swarm.ConfigReferenceFileTarget)
    {
        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "UID", EmitDefaultValue = false)]
        public string UID { get; set; }

        [DataMember(Name = "GID", EmitDefaultValue = false)]
        public string GID { get; set; }

        [DataMember(Name = "Mode", EmitDefaultValue = false)]
        public uint Mode { get; set; }
    }
}
