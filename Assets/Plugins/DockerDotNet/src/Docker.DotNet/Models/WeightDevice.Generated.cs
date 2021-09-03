using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class WeightDevice // (blkiodev.WeightDevice)
    {
        [DataMember(Name = "Path", EmitDefaultValue = false)]
        public string Path { get; set; }

        [DataMember(Name = "Weight", EmitDefaultValue = false)]
        public ushort Weight { get; set; }
    }
}
