using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImagesPruneResponse // (types.ImagesPruneReport)
    {
        [DataMember(Name = "ImagesDeleted", EmitDefaultValue = false)]
        public IList<ImageDeleteResponse> ImagesDeleted { get; set; }

        [DataMember(Name = "SpaceReclaimed", EmitDefaultValue = false)]
        public ulong SpaceReclaimed { get; set; }
    }
}
