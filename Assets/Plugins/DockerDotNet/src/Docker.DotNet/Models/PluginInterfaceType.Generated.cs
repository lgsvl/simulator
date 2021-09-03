using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginInterfaceType // (types.PluginInterfaceType)
    {
        [DataMember(Name = "Capability", EmitDefaultValue = false)]
        public string Capability { get; set; }

        [DataMember(Name = "Prefix", EmitDefaultValue = false)]
        public string Prefix { get; set; }

        [DataMember(Name = "Version", EmitDefaultValue = false)]
        public string Version { get; set; }
    }
}
