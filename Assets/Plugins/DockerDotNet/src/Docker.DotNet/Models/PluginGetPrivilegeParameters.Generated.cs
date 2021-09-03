using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginGetPrivilegeParameters // (main.PluginGetPrivilegeParameters)
    {
        [QueryStringParameter("remote", true)]
        public string Remote { get; set; }

        [DataMember(Name = "RegistryAuth", EmitDefaultValue = false)]
        public AuthConfig RegistryAuth { get; set; }
    }
}
