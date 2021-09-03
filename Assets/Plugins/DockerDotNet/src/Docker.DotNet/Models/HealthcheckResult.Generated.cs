using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class HealthcheckResult // (types.HealthcheckResult)
    {
        [DataMember(Name = "Start", EmitDefaultValue = false)]
        public DateTime Start { get; set; }

        [DataMember(Name = "End", EmitDefaultValue = false)]
        public DateTime End { get; set; }

        [DataMember(Name = "ExitCode", EmitDefaultValue = false)]
        public long ExitCode { get; set; }

        [DataMember(Name = "Output", EmitDefaultValue = false)]
        public string Output { get; set; }
    }
}
