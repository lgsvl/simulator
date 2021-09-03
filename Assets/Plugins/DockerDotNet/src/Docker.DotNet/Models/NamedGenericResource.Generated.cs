using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NamedGenericResource // (swarm.NamedGenericResource)
    {
        [DataMember(Name = "Kind", EmitDefaultValue = false)]
        public string Kind { get; set; }

        [DataMember(Name = "Value", EmitDefaultValue = false)]
        public string Value { get; set; }
    }
}
