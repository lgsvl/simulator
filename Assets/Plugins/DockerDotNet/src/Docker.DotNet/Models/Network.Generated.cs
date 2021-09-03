using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Network // (swarm.Network)
    {
        public Network()
        {
        }

        public Network(Meta Meta)
        {
            if (Meta != null)
            {
                this.Version = Meta.Version;
                this.CreatedAt = Meta.CreatedAt;
                this.UpdatedAt = Meta.UpdatedAt;
            }
        }

        [DataMember(Name = "ID", EmitDefaultValue = false)]
        public string ID { get; set; }

        [DataMember(Name = "Version", EmitDefaultValue = false)]
        public Version Version { get; set; }

        [DataMember(Name = "CreatedAt", EmitDefaultValue = false)]
        public DateTime CreatedAt { get; set; }

        [DataMember(Name = "UpdatedAt", EmitDefaultValue = false)]
        public DateTime UpdatedAt { get; set; }

        [DataMember(Name = "Spec", EmitDefaultValue = false)]
        public NetworkSpec Spec { get; set; }

        [DataMember(Name = "DriverState", EmitDefaultValue = false)]
        public SwarmDriver DriverState { get; set; }

        [DataMember(Name = "IPAMOptions", EmitDefaultValue = false)]
        public IPAMOptions IPAMOptions { get; set; }
    }
}
