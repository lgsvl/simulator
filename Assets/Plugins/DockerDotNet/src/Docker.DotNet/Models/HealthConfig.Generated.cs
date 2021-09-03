using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class HealthConfig // (container.HealthConfig)
    {
        [DataMember(Name = "Test", EmitDefaultValue = false)]
        public IList<string> Test { get; set; }

        [DataMember(Name = "Interval", EmitDefaultValue = false)]
        [JsonConverter(typeof(TimeSpanNanosecondsConverter))]
        public TimeSpan Interval { get; set; }

        [DataMember(Name = "Timeout", EmitDefaultValue = false)]
        [JsonConverter(typeof(TimeSpanSecondsConverter))]
        public TimeSpan Timeout { get; set; }

        [DataMember(Name = "StartPeriod", EmitDefaultValue = false)]
        public long StartPeriod { get; set; }

        [DataMember(Name = "Retries", EmitDefaultValue = false)]
        public long Retries { get; set; }
    }
}
