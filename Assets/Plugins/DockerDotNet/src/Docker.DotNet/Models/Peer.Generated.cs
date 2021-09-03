using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Peer // (swarm.Peer)
    {
        [DataMember(Name = "NodeID", EmitDefaultValue = false)]
        public string NodeID { get; set; }

        [DataMember(Name = "Addr", EmitDefaultValue = false)]
        public string Addr { get; set; }
    }
}
