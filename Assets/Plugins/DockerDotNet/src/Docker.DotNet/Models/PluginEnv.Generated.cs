using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginEnv // (types.PluginEnv)
    {
        [DataMember(Name = "Description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Settable", EmitDefaultValue = false)]
        public IList<string> Settable { get; set; }

        [DataMember(Name = "Value", EmitDefaultValue = false)]
        public string Value { get; set; }
    }
}
