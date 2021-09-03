using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Actor // (events.Actor)
    {
        [DataMember(Name = "ID", EmitDefaultValue = false)]
        public string ID { get; set; }

        [DataMember(Name = "Attributes", EmitDefaultValue = false)]
        public IDictionary<string, string> Attributes { get; set; }
    }
}
