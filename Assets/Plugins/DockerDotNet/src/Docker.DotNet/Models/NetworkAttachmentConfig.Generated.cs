using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NetworkAttachmentConfig // (swarm.NetworkAttachmentConfig)
    {
        [DataMember(Name = "Target", EmitDefaultValue = false)]
        public string Target { get; set; }

        [DataMember(Name = "Aliases", EmitDefaultValue = false)]
        public IList<string> Aliases { get; set; }

        [DataMember(Name = "DriverOpts", EmitDefaultValue = false)]
        public IDictionary<string, string> DriverOpts { get; set; }
    }
}
