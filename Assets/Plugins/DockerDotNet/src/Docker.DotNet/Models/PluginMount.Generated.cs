using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginMount // (types.PluginMount)
    {
        [DataMember(Name = "Description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "Destination", EmitDefaultValue = false)]
        public string Destination { get; set; }

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        public IList<string> Options { get; set; }

        [DataMember(Name = "Settable", EmitDefaultValue = false)]
        public IList<string> Settable { get; set; }

        [DataMember(Name = "Source", EmitDefaultValue = false)]
        public string Source { get; set; }

        [DataMember(Name = "Type", EmitDefaultValue = false)]
        public string Type { get; set; }
    }
}
