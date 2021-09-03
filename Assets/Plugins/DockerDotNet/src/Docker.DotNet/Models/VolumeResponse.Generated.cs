using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class VolumeResponse // (main.VolumeResponse)
    {
        [DataMember(Name = "CreatedAt", EmitDefaultValue = false)]
        public string CreatedAt { get; set; }

        [DataMember(Name = "Driver", EmitDefaultValue = false)]
        public string Driver { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "Mountpoint", EmitDefaultValue = false)]
        public string Mountpoint { get; set; }

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        public IDictionary<string, string> Options { get; set; }

        [DataMember(Name = "Scope", EmitDefaultValue = false)]
        public string Scope { get; set; }

        [DataMember(Name = "Status", EmitDefaultValue = false)]
        public IDictionary<string, object> Status { get; set; }

        [DataMember(Name = "UsageData", EmitDefaultValue = false)]
        public VolumeUsageData UsageData { get; set; }
    }
}
