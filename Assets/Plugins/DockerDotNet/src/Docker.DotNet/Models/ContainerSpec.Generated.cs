using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerSpec // (swarm.ContainerSpec)
    {
        [DataMember(Name = "Image", EmitDefaultValue = false)]
        public string Image { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "Command", EmitDefaultValue = false)]
        public IList<string> Command { get; set; }

        [DataMember(Name = "Args", EmitDefaultValue = false)]
        public IList<string> Args { get; set; }

        [DataMember(Name = "Hostname", EmitDefaultValue = false)]
        public string Hostname { get; set; }

        [DataMember(Name = "Env", EmitDefaultValue = false)]
        public IList<string> Env { get; set; }

        [DataMember(Name = "Dir", EmitDefaultValue = false)]
        public string Dir { get; set; }

        [DataMember(Name = "User", EmitDefaultValue = false)]
        public string User { get; set; }

        [DataMember(Name = "Groups", EmitDefaultValue = false)]
        public IList<string> Groups { get; set; }

        [DataMember(Name = "Privileges", EmitDefaultValue = false)]
        public Privileges Privileges { get; set; }

        [DataMember(Name = "Init", EmitDefaultValue = false)]
        public bool? Init { get; set; }

        [DataMember(Name = "StopSignal", EmitDefaultValue = false)]
        public string StopSignal { get; set; }

        [DataMember(Name = "TTY", EmitDefaultValue = false)]
        public bool TTY { get; set; }

        [DataMember(Name = "OpenStdin", EmitDefaultValue = false)]
        public bool OpenStdin { get; set; }

        [DataMember(Name = "ReadOnly", EmitDefaultValue = false)]
        public bool ReadOnly { get; set; }

        [DataMember(Name = "Mounts", EmitDefaultValue = false)]
        public IList<Mount> Mounts { get; set; }

        [DataMember(Name = "StopGracePeriod", EmitDefaultValue = false)]
        public long? StopGracePeriod { get; set; }

        [DataMember(Name = "Healthcheck", EmitDefaultValue = false)]
        public HealthConfig Healthcheck { get; set; }

        [DataMember(Name = "Hosts", EmitDefaultValue = false)]
        public IList<string> Hosts { get; set; }

        [DataMember(Name = "DNSConfig", EmitDefaultValue = false)]
        public DNSConfig DNSConfig { get; set; }

        [DataMember(Name = "Secrets", EmitDefaultValue = false)]
        public IList<SecretReference> Secrets { get; set; }

        [DataMember(Name = "Configs", EmitDefaultValue = false)]
        public IList<SwarmConfigReference> Configs { get; set; }

        [DataMember(Name = "Isolation", EmitDefaultValue = false)]
        public string Isolation { get; set; }

        [DataMember(Name = "Sysctls", EmitDefaultValue = false)]
        public IDictionary<string, string> Sysctls { get; set; }

        [DataMember(Name = "CapabilityAdd", EmitDefaultValue = false)]
        public IList<string> CapabilityAdd { get; set; }

        [DataMember(Name = "CapabilityDrop", EmitDefaultValue = false)]
        public IList<string> CapabilityDrop { get; set; }

        [DataMember(Name = "Ulimits", EmitDefaultValue = false)]
        public IList<Ulimit> Ulimits { get; set; }
    }
}
