using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class AuthResponse // (registry.AuthenticateOKBody)
    {
        [DataMember(Name = "IdentityToken", EmitDefaultValue = false)]
        public string IdentityToken { get; set; }

        [DataMember(Name = "Status", EmitDefaultValue = false)]
        public string Status { get; set; }
    }
}
