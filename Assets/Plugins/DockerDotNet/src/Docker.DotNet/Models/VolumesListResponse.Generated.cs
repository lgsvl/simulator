using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class VolumesListResponse // (main.VolumesListResponse)
    {
        [DataMember(Name = "Volumes", EmitDefaultValue = false)]
        public IList<VolumeResponse> Volumes { get; set; }

        [DataMember(Name = "Warnings", EmitDefaultValue = false)]
        public IList<string> Warnings { get; set; }
    }
}
