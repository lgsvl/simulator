using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmUnlockParameters // (main.SwarmUnlockParameters)
    {
        [DataMember(Name = "UnlockKey", EmitDefaultValue = false)]
        public string UnlockKey { get; set; }
    }
}
