using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerStatsResponse // (types.StatsJSON)
    {
        public ContainerStatsResponse()
        {
        }

        public ContainerStatsResponse(Stats Stats)
        {
            if (Stats != null)
            {
                this.Read = Stats.Read;
                this.PreRead = Stats.PreRead;
                this.PidsStats = Stats.PidsStats;
                this.BlkioStats = Stats.BlkioStats;
                this.NumProcs = Stats.NumProcs;
                this.StorageStats = Stats.StorageStats;
                this.CPUStats = Stats.CPUStats;
                this.PreCPUStats = Stats.PreCPUStats;
                this.MemoryStats = Stats.MemoryStats;
            }
        }

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

        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string ID { get; set; }

        [DataMember(Name = "networks", EmitDefaultValue = false)]
        public IDictionary<string, NetworkStats> Networks { get; set; }
    }
}
