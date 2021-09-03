using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginSpec // (runtime.PluginSpec)
    {
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "remote", EmitDefaultValue = false)]
        public string Remote { get; set; }

        [DataMember(Name = "privileges", EmitDefaultValue = false)]
        public IList<RuntimePluginPrivilege> Privileges { get; set; }

        [DataMember(Name = "disabled", EmitDefaultValue = false)]
        public bool Disabled { get; set; }

        [DataMember(Name = "env", EmitDefaultValue = false)]
        public IList<string> Env { get; set; }
    }
}
