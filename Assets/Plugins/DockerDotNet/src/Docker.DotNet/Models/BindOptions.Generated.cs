using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class BindOptions // (mount.BindOptions)
    {
        [DataMember(Name = "Propagation", EmitDefaultValue = false)]
        public string Propagation { get; set; }

        [DataMember(Name = "NonRecursive", EmitDefaultValue = false)]
        public bool NonRecursive { get; set; }
    }
}
