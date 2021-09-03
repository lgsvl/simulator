using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class RootFS // (types.RootFS)
    {
        [DataMember(Name = "Type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "Layers", EmitDefaultValue = false)]
        public IList<string> Layers { get; set; }

        [DataMember(Name = "BaseLayer", EmitDefaultValue = false)]
        public string BaseLayer { get; set; }
    }
}
