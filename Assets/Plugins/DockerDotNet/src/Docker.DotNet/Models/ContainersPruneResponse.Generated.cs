using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainersPruneResponse // (types.ContainersPruneReport)
    {
        [DataMember(Name = "ContainersDeleted", EmitDefaultValue = false)]
        public IList<string> ContainersDeleted { get; set; }

        [DataMember(Name = "SpaceReclaimed", EmitDefaultValue = false)]
        public ulong SpaceReclaimed { get; set; }
    }
}
