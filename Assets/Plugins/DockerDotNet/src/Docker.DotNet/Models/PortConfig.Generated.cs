using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PortConfig // (swarm.PortConfig)
    {
        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Protocol", EmitDefaultValue = false)]
        public string Protocol { get; set; }

        [DataMember(Name = "TargetPort", EmitDefaultValue = false)]
        public uint TargetPort { get; set; }

        [DataMember(Name = "PublishedPort", EmitDefaultValue = false)]
        public uint PublishedPort { get; set; }

        [DataMember(Name = "PublishMode", EmitDefaultValue = false)]
        public string PublishMode { get; set; }
    }
}
