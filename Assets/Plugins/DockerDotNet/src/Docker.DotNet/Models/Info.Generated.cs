using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Info // (swarm.Info)
    {
        [DataMember(Name = "NodeID", EmitDefaultValue = false)]
        public string NodeID { get; set; }

        [DataMember(Name = "NodeAddr", EmitDefaultValue = false)]
        public string NodeAddr { get; set; }

        [DataMember(Name = "LocalNodeState", EmitDefaultValue = false)]
        public string LocalNodeState { get; set; }

        [DataMember(Name = "ControlAvailable", EmitDefaultValue = false)]
        public bool ControlAvailable { get; set; }

        [DataMember(Name = "Error", EmitDefaultValue = false)]
        public string Error { get; set; }

        [DataMember(Name = "RemoteManagers", EmitDefaultValue = false)]
        public IList<Peer> RemoteManagers { get; set; }

        [DataMember(Name = "Nodes", EmitDefaultValue = false)]
        public long Nodes { get; set; }

        [DataMember(Name = "Managers", EmitDefaultValue = false)]
        public long Managers { get; set; }

        [DataMember(Name = "Cluster", EmitDefaultValue = false)]
        public ClusterInfo Cluster { get; set; }

        [DataMember(Name = "Warnings", EmitDefaultValue = false)]
        public IList<string> Warnings { get; set; }
    }
}
