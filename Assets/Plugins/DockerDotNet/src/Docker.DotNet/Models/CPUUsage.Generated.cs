using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class CPUUsage // (types.CPUUsage)
    {
        [DataMember(Name = "total_usage", EmitDefaultValue = false)]
        public ulong TotalUsage { get; set; }

        [DataMember(Name = "percpu_usage", EmitDefaultValue = false)]
        public IList<ulong> PercpuUsage { get; set; }

        [DataMember(Name = "usage_in_kernelmode", EmitDefaultValue = false)]
        public ulong UsageInKernelmode { get; set; }

        [DataMember(Name = "usage_in_usermode", EmitDefaultValue = false)]
        public ulong UsageInUsermode { get; set; }
    }
}
