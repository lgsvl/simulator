using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginConfig // (types.PluginConfig)
    {
        [DataMember(Name = "Args", EmitDefaultValue = false)]
        public PluginConfigArgs Args { get; set; }

        [DataMember(Name = "Description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "DockerVersion", EmitDefaultValue = false)]
        public string DockerVersion { get; set; }

        [DataMember(Name = "Documentation", EmitDefaultValue = false)]
        public string Documentation { get; set; }

        [DataMember(Name = "Entrypoint", EmitDefaultValue = false)]
        public IList<string> Entrypoint { get; set; }

        [DataMember(Name = "Env", EmitDefaultValue = false)]
        public IList<PluginEnv> Env { get; set; }

        [DataMember(Name = "Interface", EmitDefaultValue = false)]
        public PluginConfigInterface Interface { get; set; }

        [DataMember(Name = "IpcHost", EmitDefaultValue = false)]
        public bool IpcHost { get; set; }

        [DataMember(Name = "Linux", EmitDefaultValue = false)]
        public PluginConfigLinux Linux { get; set; }

        [DataMember(Name = "Mounts", EmitDefaultValue = false)]
        public IList<PluginMount> Mounts { get; set; }

        [DataMember(Name = "Network", EmitDefaultValue = false)]
        public PluginConfigNetwork Network { get; set; }

        [DataMember(Name = "PidHost", EmitDefaultValue = false)]
        public bool PidHost { get; set; }

        [DataMember(Name = "PropagatedMount", EmitDefaultValue = false)]
        public string PropagatedMount { get; set; }

        [DataMember(Name = "User", EmitDefaultValue = false)]
        public PluginConfigUser User { get; set; }

        [DataMember(Name = "WorkDir", EmitDefaultValue = false)]
        public string WorkDir { get; set; }

        [DataMember(Name = "rootfs", EmitDefaultValue = false)]
        public PluginConfigRootfs Rootfs { get; set; }
    }
}
