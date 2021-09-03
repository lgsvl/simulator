using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmConfigReference // (swarm.ConfigReference)
    {
        [DataMember(Name = "File", EmitDefaultValue = false)]
        public ConfigReferenceFileTarget File { get; set; }

        [DataMember(Name = "Runtime", EmitDefaultValue = false)]
        public ConfigReferenceRuntimeTarget Runtime { get; set; }

        [DataMember(Name = "ConfigID", EmitDefaultValue = false)]
        public string ConfigID { get; set; }

        [DataMember(Name = "ConfigName", EmitDefaultValue = false)]
        public string ConfigName { get; set; }
    }
}
