using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NetworkSpec // (swarm.NetworkSpec)
    {
        public NetworkSpec()
        {
        }

        public NetworkSpec(Annotations Annotations)
        {
            if (Annotations != null)
            {
                this.Name = Annotations.Name;
                this.Labels = Annotations.Labels;
            }
        }

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "DriverConfiguration", EmitDefaultValue = false)]
        public SwarmDriver DriverConfiguration { get; set; }

        [DataMember(Name = "IPv6Enabled", EmitDefaultValue = false)]
        public bool IPv6Enabled { get; set; }

        [DataMember(Name = "Internal", EmitDefaultValue = false)]
        public bool Internal { get; set; }

        [DataMember(Name = "Attachable", EmitDefaultValue = false)]
        public bool Attachable { get; set; }

        [DataMember(Name = "Ingress", EmitDefaultValue = false)]
        public bool Ingress { get; set; }

        [DataMember(Name = "IPAMOptions", EmitDefaultValue = false)]
        public IPAMOptions IPAMOptions { get; set; }

        [DataMember(Name = "ConfigFrom", EmitDefaultValue = false)]
        public ConfigReference ConfigFrom { get; set; }

        [DataMember(Name = "Scope", EmitDefaultValue = false)]
        public string Scope { get; set; }
    }
}
