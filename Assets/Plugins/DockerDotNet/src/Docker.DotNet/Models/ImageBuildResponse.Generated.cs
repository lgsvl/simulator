using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImageBuildResponse // (types.ImageBuildResponse)
    {
        [DataMember(Name = "Body", EmitDefaultValue = false)]
        public object Body { get; set; }

        [DataMember(Name = "OSType", EmitDefaultValue = false)]
        public string OSType { get; set; }
    }
}
