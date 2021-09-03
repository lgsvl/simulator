using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImageInspectResponse // (types.ImageInspect)
    {
        [DataMember(Name = "Id", EmitDefaultValue = false)]
        public string ID { get; set; }

        [DataMember(Name = "RepoTags", EmitDefaultValue = false)]
        public IList<string> RepoTags { get; set; }

        [DataMember(Name = "RepoDigests", EmitDefaultValue = false)]
        public IList<string> RepoDigests { get; set; }

        [DataMember(Name = "Parent", EmitDefaultValue = false)]
        public string Parent { get; set; }

        [DataMember(Name = "Comment", EmitDefaultValue = false)]
        public string Comment { get; set; }

        [DataMember(Name = "Created", EmitDefaultValue = false)]
        public DateTime Created { get; set; }

        [DataMember(Name = "Container", EmitDefaultValue = false)]
        public string Container { get; set; }

        [DataMember(Name = "ContainerConfig", EmitDefaultValue = false)]
        public Config ContainerConfig { get; set; }

        [DataMember(Name = "DockerVersion", EmitDefaultValue = false)]
        public string DockerVersion { get; set; }

        [DataMember(Name = "Author", EmitDefaultValue = false)]
        public string Author { get; set; }

        [DataMember(Name = "Config", EmitDefaultValue = false)]
        public Config Config { get; set; }

        [DataMember(Name = "Architecture", EmitDefaultValue = false)]
        public string Architecture { get; set; }

        [DataMember(Name = "Variant", EmitDefaultValue = false)]
        public string Variant { get; set; }

        [DataMember(Name = "Os", EmitDefaultValue = false)]
        public string Os { get; set; }

        [DataMember(Name = "OsVersion", EmitDefaultValue = false)]
        public string OsVersion { get; set; }

        [DataMember(Name = "Size", EmitDefaultValue = false)]
        public long Size { get; set; }

        [DataMember(Name = "VirtualSize", EmitDefaultValue = false)]
        public long VirtualSize { get; set; }

        [DataMember(Name = "GraphDriver", EmitDefaultValue = false)]
        public GraphDriverData GraphDriver { get; set; }

        [DataMember(Name = "RootFS", EmitDefaultValue = false)]
        public RootFS RootFS { get; set; }

        [DataMember(Name = "Metadata", EmitDefaultValue = false)]
        public ImageMetadata Metadata { get; set; }
    }
}
