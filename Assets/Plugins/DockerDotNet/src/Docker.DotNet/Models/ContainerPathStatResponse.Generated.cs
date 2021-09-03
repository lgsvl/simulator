using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerPathStatResponse // (types.ContainerPathStat)
    {
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "size", EmitDefaultValue = false)]
        public long Size { get; set; }

        [DataMember(Name = "mode", EmitDefaultValue = false)]
        public uint Mode { get; set; }

        [DataMember(Name = "mtime", EmitDefaultValue = false)]
        public DateTime Mtime { get; set; }

        [DataMember(Name = "linkTarget", EmitDefaultValue = false)]
        public string LinkTarget { get; set; }
    }
}
