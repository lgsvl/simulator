using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ThrottlingData // (types.ThrottlingData)
    {
        [DataMember(Name = "periods", EmitDefaultValue = false)]
        public ulong Periods { get; set; }

        [DataMember(Name = "throttled_periods", EmitDefaultValue = false)]
        public ulong ThrottledPeriods { get; set; }

        [DataMember(Name = "throttled_time", EmitDefaultValue = false)]
        public ulong ThrottledTime { get; set; }
    }
}
