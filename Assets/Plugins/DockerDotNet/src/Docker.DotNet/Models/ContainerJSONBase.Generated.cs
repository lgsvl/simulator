using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerJSONBase // (types.ContainerJSONBase)
    {
        [DataMember(Name = "Id", EmitDefaultValue = false)]
        public string ID { get; set; }

        [DataMember(Name = "Created", EmitDefaultValue = false)]
        public DateTime Created { get; set; }

        [DataMember(Name = "Path", EmitDefaultValue = false)]
        public string Path { get; set; }

        [DataMember(Name = "Args", EmitDefaultValue = false)]
        public IList<string> Args { get; set; }

        [DataMember(Name = "State", EmitDefaultValue = false)]
        public ContainerState State { get; set; }

        [DataMember(Name = "Image", EmitDefaultValue = false)]
        public string Image { get; set; }

        [DataMember(Name = "ResolvConfPath", EmitDefaultValue = false)]
        public string ResolvConfPath { get; set; }

        [DataMember(Name = "HostnamePath", EmitDefaultValue = false)]
        public string HostnamePath { get; set; }

        [DataMember(Name = "HostsPath", EmitDefaultValue = false)]
        public string HostsPath { get; set; }

        [DataMember(Name = "LogPath", EmitDefaultValue = false)]
        public string LogPath { get; set; }

        [DataMember(Name = "Node", EmitDefaultValue = false)]
        public ContainerNode Node { get; set; }

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "RestartCount", EmitDefaultValue = false)]
        public long RestartCount { get; set; }

        [DataMember(Name = "Driver", EmitDefaultValue = false)]
        public string Driver { get; set; }

        [DataMember(Name = "Platform", EmitDefaultValue = false)]
        public string Platform { get; set; }

        [DataMember(Name = "MountLabel", EmitDefaultValue = false)]
        public string MountLabel { get; set; }

        [DataMember(Name = "ProcessLabel", EmitDefaultValue = false)]
        public string ProcessLabel { get; set; }

        [DataMember(Name = "AppArmorProfile", EmitDefaultValue = false)]
        public string AppArmorProfile { get; set; }

        [DataMember(Name = "ExecIDs", EmitDefaultValue = false)]
        public IList<string> ExecIDs { get; set; }

        [DataMember(Name = "HostConfig", EmitDefaultValue = false)]
        public HostConfig HostConfig { get; set; }

        [DataMember(Name = "GraphDriver", EmitDefaultValue = false)]
        public GraphDriverData GraphDriver { get; set; }

        [DataMember(Name = "SizeRw", EmitDefaultValue = false)]
        public long? SizeRw { get; set; }

        [DataMember(Name = "SizeRootFs", EmitDefaultValue = false)]
        public long? SizeRootFs { get; set; }
    }
}
