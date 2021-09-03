using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NetworkDisconnectParameters // (types.NetworkDisconnect)
    {
        [DataMember(Name = "Container", EmitDefaultValue = false)]
        public string Container { get; set; }

        [DataMember(Name = "Force", EmitDefaultValue = false)]
        public bool Force { get; set; }
    }
}
