using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class BlkioStats // (types.BlkioStats)
    {
        [DataMember(Name = "io_service_bytes_recursive", EmitDefaultValue = false)]
        public IList<BlkioStatEntry> IoServiceBytesRecursive { get; set; }

        [DataMember(Name = "io_serviced_recursive", EmitDefaultValue = false)]
        public IList<BlkioStatEntry> IoServicedRecursive { get; set; }

        [DataMember(Name = "io_queue_recursive", EmitDefaultValue = false)]
        public IList<BlkioStatEntry> IoQueuedRecursive { get; set; }

        [DataMember(Name = "io_service_time_recursive", EmitDefaultValue = false)]
        public IList<BlkioStatEntry> IoServiceTimeRecursive { get; set; }

        [DataMember(Name = "io_wait_time_recursive", EmitDefaultValue = false)]
        public IList<BlkioStatEntry> IoWaitTimeRecursive { get; set; }

        [DataMember(Name = "io_merged_recursive", EmitDefaultValue = false)]
        public IList<BlkioStatEntry> IoMergedRecursive { get; set; }

        [DataMember(Name = "io_time_recursive", EmitDefaultValue = false)]
        public IList<BlkioStatEntry> IoTimeRecursive { get; set; }

        [DataMember(Name = "sectors_recursive", EmitDefaultValue = false)]
        public IList<BlkioStatEntry> SectorsRecursive { get; set; }
    }
}
