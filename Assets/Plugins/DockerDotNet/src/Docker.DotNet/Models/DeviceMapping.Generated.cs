using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class DeviceMapping // (container.DeviceMapping)
    {
        [DataMember(Name = "PathOnHost", EmitDefaultValue = false)]
        public string PathOnHost { get; set; }

        [DataMember(Name = "PathInContainer", EmitDefaultValue = false)]
        public string PathInContainer { get; set; }

        [DataMember(Name = "CgroupPermissions", EmitDefaultValue = false)]
        public string CgroupPermissions { get; set; }
    }
}
