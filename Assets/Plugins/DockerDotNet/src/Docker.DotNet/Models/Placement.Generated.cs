using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Placement // (swarm.Placement)
    {
        [DataMember(Name = "Constraints", EmitDefaultValue = false)]
        public IList<string> Constraints { get; set; }

        [DataMember(Name = "Preferences", EmitDefaultValue = false)]
        public IList<PlacementPreference> Preferences { get; set; }

        [DataMember(Name = "MaxReplicas", EmitDefaultValue = false)]
        public ulong MaxReplicas { get; set; }

        [DataMember(Name = "Platforms", EmitDefaultValue = false)]
        public IList<Platform> Platforms { get; set; }
    }
}
