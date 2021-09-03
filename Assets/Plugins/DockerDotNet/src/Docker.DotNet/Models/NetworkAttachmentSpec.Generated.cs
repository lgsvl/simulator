using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NetworkAttachmentSpec // (swarm.NetworkAttachmentSpec)
    {
        [DataMember(Name = "ContainerID", EmitDefaultValue = false)]
        public string ContainerID { get; set; }
    }
}
