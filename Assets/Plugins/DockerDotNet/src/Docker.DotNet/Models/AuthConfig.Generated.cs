using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class AuthConfig // (types.AuthConfig)
    {
        [DataMember(Name = "username", EmitDefaultValue = false)]
        public string Username { get; set; }

        [DataMember(Name = "password", EmitDefaultValue = false)]
        public string Password { get; set; }

        [DataMember(Name = "auth", EmitDefaultValue = false)]
        public string Auth { get; set; }

        [DataMember(Name = "email", EmitDefaultValue = false)]
        public string Email { get; set; }

        [DataMember(Name = "serveraddress", EmitDefaultValue = false)]
        public string ServerAddress { get; set; }

        [DataMember(Name = "identitytoken", EmitDefaultValue = false)]
        public string IdentityToken { get; set; }

        [DataMember(Name = "registrytoken", EmitDefaultValue = false)]
        public string RegistryToken { get; set; }
    }
}
