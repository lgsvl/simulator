using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ConfigReference // (network.ConfigReference)
    {
        [DataMember(Name = "Network", EmitDefaultValue = false)]
        public string Network { get; set; }
    }
}
