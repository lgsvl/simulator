using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ExternalCA // (swarm.ExternalCA)
    {
        [DataMember(Name = "Protocol", EmitDefaultValue = false)]
        public string Protocol { get; set; }

        [DataMember(Name = "URL", EmitDefaultValue = false)]
        public string URL { get; set; }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        public IDictionary<string, string> Options { get; set; }

        [DataMember(Name = "CACert", EmitDefaultValue = false)]
        public string CACert { get; set; }
    }
}
