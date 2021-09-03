using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PlacementPreference // (swarm.PlacementPreference)
    {
        [DataMember(Name = "Spread", EmitDefaultValue = false)]
        public SpreadOver Spread { get; set; }
    }
}
