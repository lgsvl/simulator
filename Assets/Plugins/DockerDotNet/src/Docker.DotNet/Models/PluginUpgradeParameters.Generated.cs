using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginUpgradeParameters // (main.PluginUpgradeParameters)
    {
        [QueryStringParameter("remote", true)]
        public string Remote { get; set; }

        [DataMember(Name = "RegistryAuth", EmitDefaultValue = false)]
        public AuthConfig RegistryAuth { get; set; }

        [DataMember(Name = "Privileges", EmitDefaultValue = false)]
        public IList<PluginPrivilege> Privileges { get; set; }
    }
}
