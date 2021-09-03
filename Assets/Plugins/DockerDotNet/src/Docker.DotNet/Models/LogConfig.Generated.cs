using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class LogConfig // (container.LogConfig)
    {
        [DataMember(Name = "Type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "Config", EmitDefaultValue = false)]
        public IDictionary<string, string> Config { get; set; }
    }
}
