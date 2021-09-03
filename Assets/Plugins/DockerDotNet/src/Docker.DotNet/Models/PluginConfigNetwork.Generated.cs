using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginConfigNetwork // (types.PluginConfigNetwork)
    {
        [DataMember(Name = "Type", EmitDefaultValue = false)]
        public string Type { get; set; }
    }
}
