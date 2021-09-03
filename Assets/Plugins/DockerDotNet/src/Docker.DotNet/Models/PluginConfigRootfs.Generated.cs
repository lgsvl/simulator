using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginConfigRootfs // (types.PluginConfigRootfs)
    {
        [DataMember(Name = "diff_ids", EmitDefaultValue = false)]
        public IList<string> DiffIds { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }
    }
}
