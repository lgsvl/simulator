using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Runtime // (types.Runtime)
    {
        [DataMember(Name = "path", EmitDefaultValue = false)]
        public string Path { get; set; }

        [DataMember(Name = "runtimeArgs", EmitDefaultValue = false)]
        public IList<string> Args { get; set; }
    }
}
