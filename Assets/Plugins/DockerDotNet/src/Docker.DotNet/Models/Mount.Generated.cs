using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Mount // (mount.Mount)
    {
        [DataMember(Name = "Type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "Source", EmitDefaultValue = false)]
        public string Source { get; set; }

        [DataMember(Name = "Target", EmitDefaultValue = false)]
        public string Target { get; set; }

        [DataMember(Name = "ReadOnly", EmitDefaultValue = false)]
        public bool ReadOnly { get; set; }

        [DataMember(Name = "Consistency", EmitDefaultValue = false)]
        public string Consistency { get; set; }

        [DataMember(Name = "BindOptions", EmitDefaultValue = false)]
        public BindOptions BindOptions { get; set; }

        [DataMember(Name = "VolumeOptions", EmitDefaultValue = false)]
        public VolumeOptions VolumeOptions { get; set; }

        [DataMember(Name = "TmpfsOptions", EmitDefaultValue = false)]
        public TmpfsOptions TmpfsOptions { get; set; }
    }
}
