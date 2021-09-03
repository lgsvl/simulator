using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SecretReference // (swarm.SecretReference)
    {
        [DataMember(Name = "File", EmitDefaultValue = false)]
        public SecretReferenceFileTarget File { get; set; }

        [DataMember(Name = "SecretID", EmitDefaultValue = false)]
        public string SecretID { get; set; }

        [DataMember(Name = "SecretName", EmitDefaultValue = false)]
        public string SecretName { get; set; }
    }
}
