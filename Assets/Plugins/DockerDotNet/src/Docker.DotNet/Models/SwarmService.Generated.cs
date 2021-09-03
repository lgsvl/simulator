using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmService // (swarm.Service)
    {
        public SwarmService()
        {
        }

        public SwarmService(Meta Meta)
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
        public ServiceSpec Spec { get; set; }

        [DataMember(Name = "PreviousSpec", EmitDefaultValue = false)]
        public ServiceSpec PreviousSpec { get; set; }

        [DataMember(Name = "Endpoint", EmitDefaultValue = false)]
        public Endpoint Endpoint { get; set; }

        [DataMember(Name = "UpdateStatus", EmitDefaultValue = false)]
        public UpdateStatus UpdateStatus { get; set; }

        [DataMember(Name = "ServiceStatus", EmitDefaultValue = false)]
        public ServiceStatus ServiceStatus { get; set; }

        [DataMember(Name = "JobStatus", EmitDefaultValue = false)]
        public JobStatus JobStatus { get; set; }
    }
}
