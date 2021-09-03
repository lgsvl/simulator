using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class GenericResource // (swarm.GenericResource)
    {
        [DataMember(Name = "NamedResourceSpec", EmitDefaultValue = false)]
        public NamedGenericResource NamedResourceSpec { get; set; }

        [DataMember(Name = "DiscreteResourceSpec", EmitDefaultValue = false)]
        public DiscreteGenericResource DiscreteResourceSpec { get; set; }
    }
}
