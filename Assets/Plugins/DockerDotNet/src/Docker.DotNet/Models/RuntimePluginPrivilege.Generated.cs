using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class RuntimePluginPrivilege // (runtime.PluginPrivilege)
    {
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "value", EmitDefaultValue = false)]
        public IList<string> Value { get; set; }
    }
}
