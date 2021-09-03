using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginsInfo // (types.PluginsInfo)
    {
        [DataMember(Name = "Volume", EmitDefaultValue = false)]
        public IList<string> Volume { get; set; }

        [DataMember(Name = "Network", EmitDefaultValue = false)]
        public IList<string> Network { get; set; }

        [DataMember(Name = "Authorization", EmitDefaultValue = false)]
        public IList<string> Authorization { get; set; }

        [DataMember(Name = "Log", EmitDefaultValue = false)]
        public IList<string> Log { get; set; }
    }
}
