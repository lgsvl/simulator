using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Health // (types.Health)
    {
        [DataMember(Name = "Status", EmitDefaultValue = false)]
        public string Status { get; set; }

        [DataMember(Name = "FailingStreak", EmitDefaultValue = false)]
        public long FailingStreak { get; set; }

        [DataMember(Name = "Log", EmitDefaultValue = false)]
        public IList<HealthcheckResult> Log { get; set; }
    }
}
