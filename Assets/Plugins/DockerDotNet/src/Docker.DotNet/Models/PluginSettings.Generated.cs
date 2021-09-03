using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginSettings // (types.PluginSettings)
    {
        [DataMember(Name = "Args", EmitDefaultValue = false)]
        public IList<string> Args { get; set; }

        [DataMember(Name = "Devices", EmitDefaultValue = false)]
        public IList<PluginDevice> Devices { get; set; }

        [DataMember(Name = "Env", EmitDefaultValue = false)]
        public IList<string> Env { get; set; }

        [DataMember(Name = "Mounts", EmitDefaultValue = false)]
        public IList<PluginMount> Mounts { get; set; }
    }
}
