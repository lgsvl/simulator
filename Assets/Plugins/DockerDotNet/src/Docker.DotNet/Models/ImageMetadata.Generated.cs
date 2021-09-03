using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImageMetadata // (types.ImageMetadata)
    {
        [DataMember(Name = "LastTagTime", EmitDefaultValue = false)]
        public DateTime LastTagTime { get; set; }
    }
}
