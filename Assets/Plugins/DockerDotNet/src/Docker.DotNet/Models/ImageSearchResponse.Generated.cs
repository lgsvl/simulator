using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImageSearchResponse // (registry.SearchResult)
    {
        [DataMember(Name = "star_count", EmitDefaultValue = false)]
        public long StarCount { get; set; }

        [DataMember(Name = "is_official", EmitDefaultValue = false)]
        public bool IsOfficial { get; set; }

        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "is_automated", EmitDefaultValue = false)]
        public bool IsAutomated { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }
    }
}
