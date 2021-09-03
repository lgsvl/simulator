using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class JoinTokens // (swarm.JoinTokens)
    {
        [DataMember(Name = "Worker", EmitDefaultValue = false)]
        public string Worker { get; set; }

        [DataMember(Name = "Manager", EmitDefaultValue = false)]
        public string Manager { get; set; }
    }
}
