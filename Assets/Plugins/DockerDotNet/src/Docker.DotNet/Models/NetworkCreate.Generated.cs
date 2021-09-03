using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NetworkCreate // (types.NetworkCreate)
    {
        [DataMember(Name = "CheckDuplicate", EmitDefaultValue = false)]
        public bool CheckDuplicate { get; set; }

        [DataMember(Name = "Driver", EmitDefaultValue = false)]
        public string Driver { get; set; }

        [DataMember(Name = "Scope", EmitDefaultValue = false)]
        public string Scope { get; set; }

        [DataMember(Name = "EnableIPv6", EmitDefaultValue = false)]
        public bool EnableIPv6 { get; set; }

        [DataMember(Name = "IPAM", EmitDefaultValue = false)]
        public IPAM IPAM { get; set; }

        [DataMember(Name = "Internal", EmitDefaultValue = false)]
        public bool Internal { get; set; }

        [DataMember(Name = "Attachable", EmitDefaultValue = false)]
        public bool Attachable { get; set; }

        [DataMember(Name = "Ingress", EmitDefaultValue = false)]
        public bool Ingress { get; set; }

        [DataMember(Name = "ConfigOnly", EmitDefaultValue = false)]
        public bool ConfigOnly { get; set; }

        [DataMember(Name = "ConfigFrom", EmitDefaultValue = false)]
        public ConfigReference ConfigFrom { get; set; }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        public IDictionary<string, string> Options { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }
    }
}
