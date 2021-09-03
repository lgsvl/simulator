using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class DeviceRequest // (container.DeviceRequest)
    {
        [DataMember(Name = "Driver", EmitDefaultValue = false)]
        public string Driver { get; set; }

        [DataMember(Name = "Count", EmitDefaultValue = false)]
        public long Count { get; set; }

        [DataMember(Name = "DeviceIDs", EmitDefaultValue = false)]
        public IList<string> DeviceIDs { get; set; }

        [DataMember(Name = "Capabilities", EmitDefaultValue = false)]
        public IList<IList<string>> Capabilities { get; set; }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        public IDictionary<string, string> Options { get; set; }
    }
}
