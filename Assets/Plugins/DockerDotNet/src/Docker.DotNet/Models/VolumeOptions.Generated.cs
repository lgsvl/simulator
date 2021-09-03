using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class VolumeOptions // (mount.VolumeOptions)
    {
        [DataMember(Name = "NoCopy", EmitDefaultValue = false)]
        public bool NoCopy { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "DriverConfig", EmitDefaultValue = false)]
        public Driver DriverConfig { get; set; }
    }
}
