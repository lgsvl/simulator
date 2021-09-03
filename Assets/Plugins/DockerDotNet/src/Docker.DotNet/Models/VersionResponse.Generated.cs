using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class VersionResponse // (types.Version)
    {
        [DataMember(Name = "Components", EmitDefaultValue = false)]
        public IList<ComponentVersion> Components { get; set; }

        [DataMember(Name = "Version", EmitDefaultValue = false)]
        public string Version { get; set; }

        [DataMember(Name = "ApiVersion", EmitDefaultValue = false)]
        public string APIVersion { get; set; }

        [DataMember(Name = "MinAPIVersion", EmitDefaultValue = false)]
        public string MinAPIVersion { get; set; }

        [DataMember(Name = "GitCommit", EmitDefaultValue = false)]
        public string GitCommit { get; set; }

        [DataMember(Name = "GoVersion", EmitDefaultValue = false)]
        public string GoVersion { get; set; }

        [DataMember(Name = "Os", EmitDefaultValue = false)]
        public string Os { get; set; }

        [DataMember(Name = "Arch", EmitDefaultValue = false)]
        public string Arch { get; set; }

        [DataMember(Name = "KernelVersion", EmitDefaultValue = false)]
        public string KernelVersion { get; set; }

        [DataMember(Name = "Experimental", EmitDefaultValue = false)]
        public bool Experimental { get; set; }

        [DataMember(Name = "BuildTime", EmitDefaultValue = false)]
        public string BuildTime { get; set; }
    }
}
