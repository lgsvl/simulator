using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class CPUStats // (types.CPUStats)
    {
        [DataMember(Name = "cpu_usage", EmitDefaultValue = false)]
        public CPUUsage CPUUsage { get; set; }

        [DataMember(Name = "system_cpu_usage", EmitDefaultValue = false)]
        public ulong SystemUsage { get; set; }

        [DataMember(Name = "online_cpus", EmitDefaultValue = false)]
        public uint OnlineCPUs { get; set; }

        [DataMember(Name = "throttling_data", EmitDefaultValue = false)]
        public ThrottlingData ThrottlingData { get; set; }
    }
}
