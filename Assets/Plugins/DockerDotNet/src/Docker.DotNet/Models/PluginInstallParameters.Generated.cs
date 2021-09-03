using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginInstallParameters // (main.PluginInstallParameters)
    {
        [QueryStringParameter("remote", true)]
        public string Remote { get; set; }

        [QueryStringParameter("name", false)]
        public string Name { get; set; }

        [DataMember(Name = "RegistryAuth", EmitDefaultValue = false)]
        public AuthConfig RegistryAuth { get; set; }

        [DataMember(Name = "Privileges", EmitDefaultValue = false)]
        public IList<PluginPrivilege> Privileges { get; set; }
    }
}
