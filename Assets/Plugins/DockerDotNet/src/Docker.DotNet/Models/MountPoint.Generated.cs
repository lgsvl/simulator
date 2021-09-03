using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class MountPoint // (types.MountPoint)
    {
        [DataMember(Name = "Type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Source", EmitDefaultValue = false)]
        public string Source { get; set; }

        [DataMember(Name = "Destination", EmitDefaultValue = false)]
        public string Destination { get; set; }

        [DataMember(Name = "Driver", EmitDefaultValue = false)]
        public string Driver { get; set; }

        [DataMember(Name = "Mode", EmitDefaultValue = false)]
        public string Mode { get; set; }

        [DataMember(Name = "RW", EmitDefaultValue = false)]
        public bool RW { get; set; }

        [DataMember(Name = "Propagation", EmitDefaultValue = false)]
        public string Propagation { get; set; }
    }
}
