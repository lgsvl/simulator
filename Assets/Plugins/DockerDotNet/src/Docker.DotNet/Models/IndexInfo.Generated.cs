using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class IndexInfo // (registry.IndexInfo)
    {
        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Mirrors", EmitDefaultValue = false)]
        public IList<string> Mirrors { get; set; }

        [DataMember(Name = "Secure", EmitDefaultValue = false)]
        public bool Secure { get; set; }

        [DataMember(Name = "Official", EmitDefaultValue = false)]
        public bool Official { get; set; }
    }
}
