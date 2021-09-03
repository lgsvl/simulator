using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerListResponse // (types.Container)
    {
        [DataMember(Name = "Id", EmitDefaultValue = false)]
        public string ID { get; set; }

        [DataMember(Name = "Names", EmitDefaultValue = false)]
        public IList<string> Names { get; set; }

        [DataMember(Name = "Image", EmitDefaultValue = false)]
        public string Image { get; set; }

        [DataMember(Name = "ImageID", EmitDefaultValue = false)]
        public string ImageID { get; set; }

        [DataMember(Name = "Command", EmitDefaultValue = false)]
        public string Command { get; set; }

        [DataMember(Name = "Created", EmitDefaultValue = false)]
        public DateTime Created { get; set; }

        [DataMember(Name = "Ports", EmitDefaultValue = false)]
        public IList<Port> Ports { get; set; }

        [DataMember(Name = "SizeRw", EmitDefaultValue = false)]
        public long SizeRw { get; set; }

        [DataMember(Name = "SizeRootFs", EmitDefaultValue = false)]
        public long SizeRootFs { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "State", EmitDefaultValue = false)]
        public string State { get; set; }

        [DataMember(Name = "Status", EmitDefaultValue = false)]
        public string Status { get; set; }

        [DataMember(Name = "NetworkSettings", EmitDefaultValue = false)]
        public SummaryNetworkSettings NetworkSettings { get; set; }

        [DataMember(Name = "Mounts", EmitDefaultValue = false)]
        public IList<MountPoint> Mounts { get; set; }
    }
}
