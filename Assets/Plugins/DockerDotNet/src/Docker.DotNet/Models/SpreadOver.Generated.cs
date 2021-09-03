using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SpreadOver // (swarm.SpreadOver)
    {
        [DataMember(Name = "SpreadDescriptor", EmitDefaultValue = false)]
        public string SpreadDescriptor { get; set; }
    }
}
