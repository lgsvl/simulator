using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NodeListResponse // (swarm.Node)
    {
        public NodeListResponse()
        {
        }

        public NodeListResponse(Meta Meta)
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
        public NodeUpdateParameters Spec { get; set; }

        [DataMember(Name = "Description", EmitDefaultValue = false)]
        public NodeDescription Description { get; set; }

        [DataMember(Name = "Status", EmitDefaultValue = false)]
        public NodeStatus Status { get; set; }

        [DataMember(Name = "ManagerStatus", EmitDefaultValue = false)]
        public ManagerStatus ManagerStatus { get; set; }
    }
}
