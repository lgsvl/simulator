using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class DiscreteGenericResource // (swarm.DiscreteGenericResource)
    {
        [DataMember(Name = "Kind", EmitDefaultValue = false)]
        public string Kind { get; set; }

        [DataMember(Name = "Value", EmitDefaultValue = false)]
        public long Value { get; set; }
    }
}
