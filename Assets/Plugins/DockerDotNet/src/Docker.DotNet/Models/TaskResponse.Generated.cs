using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class TaskResponse // (swarm.Task)
    {
        public TaskResponse()
        {
        }

        public TaskResponse(Meta Meta, Annotations Annotations)
        {
            if (Meta != null)
            {
                this.Version = Meta.Version;
                this.CreatedAt = Meta.CreatedAt;
                this.UpdatedAt = Meta.UpdatedAt;
            }

            if (Annotations != null)
            {
                this.Name = Annotations.Name;
                this.Labels = Annotations.Labels;
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

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "Spec", EmitDefaultValue = false)]
        public TaskSpec Spec { get; set; }

        [DataMember(Name = "ServiceID", EmitDefaultValue = false)]
        public string ServiceID { get; set; }

        [DataMember(Name = "Slot", EmitDefaultValue = false)]
        public long Slot { get; set; }

        [DataMember(Name = "NodeID", EmitDefaultValue = false)]
        public string NodeID { get; set; }

        [DataMember(Name = "Status", EmitDefaultValue = false)]
        public TaskStatus Status { get; set; }

        [DataMember(Name = "DesiredState", EmitDefaultValue = false)]
        public TaskState DesiredState { get; set; }

        [DataMember(Name = "NetworksAttachments", EmitDefaultValue = false)]
        public IList<NetworkAttachment> NetworksAttachments { get; set; }

        [DataMember(Name = "GenericResources", EmitDefaultValue = false)]
        public IList<GenericResource> GenericResources { get; set; }

        [DataMember(Name = "JobIteration", EmitDefaultValue = false)]
        public Version JobIteration { get; set; }
    }
}
