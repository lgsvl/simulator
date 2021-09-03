using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginConfigLinux // (types.PluginConfigLinux)
    {
        [DataMember(Name = "AllowAllDevices", EmitDefaultValue = false)]
        public bool AllowAllDevices { get; set; }

        [DataMember(Name = "Capabilities", EmitDefaultValue = false)]
        public IList<string> Capabilities { get; set; }

        [DataMember(Name = "Devices", EmitDefaultValue = false)]
        public IList<PluginDevice> Devices { get; set; }
    }
}
