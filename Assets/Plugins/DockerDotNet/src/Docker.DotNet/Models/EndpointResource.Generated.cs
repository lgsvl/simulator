using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class EndpointResource // (types.EndpointResource)
    {
        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "EndpointID", EmitDefaultValue = false)]
        public string EndpointID { get; set; }

        [DataMember(Name = "MacAddress", EmitDefaultValue = false)]
        public string MacAddress { get; set; }

        [DataMember(Name = "IPv4Address", EmitDefaultValue = false)]
        public string IPv4Address { get; set; }

        [DataMember(Name = "IPv6Address", EmitDefaultValue = false)]
        public string IPv6Address { get; set; }
    }
}
