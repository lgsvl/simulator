using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class TmpfsOptions // (mount.TmpfsOptions)
    {
        [DataMember(Name = "SizeBytes", EmitDefaultValue = false)]
        public long SizeBytes { get; set; }

        [DataMember(Name = "Mode", EmitDefaultValue = false)]
        public uint Mode { get; set; }
    }
}
