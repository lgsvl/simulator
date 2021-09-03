using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Platform // (swarm.Platform)
    {
        [DataMember(Name = "Architecture", EmitDefaultValue = false)]
        public string Architecture { get; set; }

        [DataMember(Name = "OS", EmitDefaultValue = false)]
        public string OS { get; set; }
    }
}
