using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginConfigInterface // (types.PluginConfigInterface)
    {
        [DataMember(Name = "ProtocolScheme", EmitDefaultValue = false)]
        public string ProtocolScheme { get; set; }

        [DataMember(Name = "Socket", EmitDefaultValue = false)]
        public string Socket { get; set; }

        [DataMember(Name = "Types", EmitDefaultValue = false)]
        public IList<string> Types { get; set; }
    }
}
