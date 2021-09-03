using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SecretCreateResponse // (main.SecretCreateResponse)
    {
        [DataMember(Name = "ID", EmitDefaultValue = false)]
        public string ID { get; set; }
    }
}
