using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NetworkAttachment // (swarm.NetworkAttachment)
    {
        [DataMember(Name = "Network", EmitDefaultValue = false)]
        public Network Network { get; set; }

        [DataMember(Name = "Addresses", EmitDefaultValue = false)]
        public IList<string> Addresses { get; set; }
    }
}
