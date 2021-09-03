using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class EncryptionConfig // (swarm.EncryptionConfig)
    {
        [DataMember(Name = "AutoLockManagers", EmitDefaultValue = false)]
        public bool AutoLockManagers { get; set; }
    }
}
