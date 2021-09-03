using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Stats // (types.Stats)
    {
        [DataMember(Name = "read", EmitDefaultValue = false)]
        public DateTime Read { get; set; }

        [DataMember(Name = "preread", EmitDefaultValue = false)]
        public DateTime PreRead { get; set; }

        [DataMember(Name = "pids_stats", EmitDefaultValue = false)]
        public PidsStats PidsStats { get; set; }

        [DataMember(Name = "blkio_stats", EmitDefaultValue = false)]
        public BlkioStats BlkioStats { get; set; }

        [DataMember(Name = "num_procs", EmitDefaultValue = false)]
        public uint NumProcs { get; set; }

        [DataMember(Name = "storage_stats", EmitDefaultValue = false)]
        public StorageStats StorageStats { get; set; }

        [DataMember(Name = "cpu_stats", EmitDefaultValue = false)]
        public CPUStats CPUStats { get; set; }

        [DataMember(Name = "precpu_stats", EmitDefaultValue = false)]
        public CPUStats PreCPUStats { get; set; }

        [DataMember(Name = "memory_stats", EmitDefaultValue = false)]
        public MemoryStats MemoryStats { get; set; }
    }
}
