using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class StorageStats // (types.StorageStats)
    {
        [DataMember(Name = "read_count_normalized", EmitDefaultValue = false)]
        public ulong ReadCountNormalized { get; set; }

        [DataMember(Name = "read_size_bytes", EmitDefaultValue = false)]
        public ulong ReadSizeBytes { get; set; }

        [DataMember(Name = "write_count_normalized", EmitDefaultValue = false)]
        public ulong WriteCountNormalized { get; set; }

        [DataMember(Name = "write_size_bytes", EmitDefaultValue = false)]
        public ulong WriteSizeBytes { get; set; }
    }
}
