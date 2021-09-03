using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ClusterInfo // (swarm.ClusterInfo)
    {
        public ClusterInfo()
        {
        }

        public ClusterInfo(Meta Meta)
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
        public Spec Spec { get; set; }

        [DataMember(Name = "TLSInfo", EmitDefaultValue = false)]
        public TLSInfo TLSInfo { get; set; }

        [DataMember(Name = "RootRotationInProgress", EmitDefaultValue = false)]
        public bool RootRotationInProgress { get; set; }

        [DataMember(Name = "DefaultAddrPool", EmitDefaultValue = false)]
        public IList<string> DefaultAddrPool { get; set; }

        [DataMember(Name = "SubnetSize", EmitDefaultValue = false)]
        public uint SubnetSize { get; set; }

        [DataMember(Name = "DataPathPort", EmitDefaultValue = false)]
        public uint DataPathPort { get; set; }
    }
}
