using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerInspectResponse // (types.ContainerJSON)
    {
        public ContainerInspectResponse()
        {
        }

        public ContainerInspectResponse(ContainerJSONBase ContainerJSONBase)
        {
            if (ContainerJSONBase != null)
            {
                this.ID = ContainerJSONBase.ID;
                this.Created = ContainerJSONBase.Created;
                this.Path = ContainerJSONBase.Path;
                this.Args = ContainerJSONBase.Args;
                this.State = ContainerJSONBase.State;
                this.Image = ContainerJSONBase.Image;
                this.ResolvConfPath = ContainerJSONBase.ResolvConfPath;
                this.HostnamePath = ContainerJSONBase.HostnamePath;
                this.HostsPath = ContainerJSONBase.HostsPath;
                this.LogPath = ContainerJSONBase.LogPath;
                this.Node = ContainerJSONBase.Node;
                this.Name = ContainerJSONBase.Name;
                this.RestartCount = ContainerJSONBase.RestartCount;
                this.Driver = ContainerJSONBase.Driver;
                this.Platform = ContainerJSONBase.Platform;
                this.MountLabel = ContainerJSONBase.MountLabel;
                this.ProcessLabel = ContainerJSONBase.ProcessLabel;
                this.AppArmorProfile = ContainerJSONBase.AppArmorProfile;
                this.ExecIDs = ContainerJSONBase.ExecIDs;
                this.HostConfig = ContainerJSONBase.HostConfig;
                this.GraphDriver = ContainerJSONBase.GraphDriver;
                this.SizeRw = ContainerJSONBase.SizeRw;
                this.SizeRootFs = ContainerJSONBase.SizeRootFs;
            }
        }

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

        [DataMember(Name = "Mounts", EmitDefaultValue = false)]
        public IList<MountPoint> Mounts { get; set; }

        [DataMember(Name = "Config", EmitDefaultValue = false)]
        public Config Config { get; set; }

        [DataMember(Name = "NetworkSettings", EmitDefaultValue = false)]
        public NetworkSettings NetworkSettings { get; set; }
    }
}
