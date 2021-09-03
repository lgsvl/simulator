using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Version // (swarm.Version)
    {
        [DataMember(Name = "Index", EmitDefaultValue = false)]
        public ulong Index { get; set; }
    }
}
