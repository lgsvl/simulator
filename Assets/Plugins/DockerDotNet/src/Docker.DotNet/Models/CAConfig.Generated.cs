using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class CAConfig // (swarm.CAConfig)
    {
        [DataMember(Name = "NodeCertExpiry", EmitDefaultValue = false)]
        public long NodeCertExpiry { get; set; }

        [DataMember(Name = "ExternalCAs", EmitDefaultValue = false)]
        public IList<ExternalCA> ExternalCAs { get; set; }

        [DataMember(Name = "SigningCACert", EmitDefaultValue = false)]
        public string SigningCACert { get; set; }

        [DataMember(Name = "SigningCAKey", EmitDefaultValue = false)]
        public string SigningCAKey { get; set; }

        [DataMember(Name = "ForceRotate", EmitDefaultValue = false)]
        public ulong ForceRotate { get; set; }
    }
}
