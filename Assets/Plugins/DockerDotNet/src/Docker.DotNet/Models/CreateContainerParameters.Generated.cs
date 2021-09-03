using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class CreateContainerParameters // (main.CreateContainerParameters)
    {
        public CreateContainerParameters()
        {
        }

        public CreateContainerParameters(Config Config)
        {
            if (Config != null)
            {
                this.Hostname = Config.Hostname;
                this.Domainname = Config.Domainname;
                this.User = Config.User;
                this.AttachStdin = Config.AttachStdin;
                this.AttachStdout = Config.AttachStdout;
                this.AttachStderr = Config.AttachStderr;
                this.ExposedPorts = Config.ExposedPorts;
                this.Tty = Config.Tty;
                this.OpenStdin = Config.OpenStdin;
                this.StdinOnce = Config.StdinOnce;
                this.Env = Config.Env;
                this.Cmd = Config.Cmd;
                this.Healthcheck = Config.Healthcheck;
                this.ArgsEscaped = Config.ArgsEscaped;
                this.Image = Config.Image;
                this.Volumes = Config.Volumes;
                this.WorkingDir = Config.WorkingDir;
                this.Entrypoint = Config.Entrypoint;
                this.NetworkDisabled = Config.NetworkDisabled;
                this.MacAddress = Config.MacAddress;
                this.OnBuild = Config.OnBuild;
                this.Labels = Config.Labels;
                this.StopSignal = Config.StopSignal;
                this.StopTimeout = Config.StopTimeout;
                this.Shell = Config.Shell;
            }
        }

        [QueryStringParameter("name", false)]
        public string Name { get; set; }

        [DataMember(Name = "Hostname", EmitDefaultValue = false)]
        public string Hostname { get; set; }

        [DataMember(Name = "Domainname", EmitDefaultValue = false)]
        public string Domainname { get; set; }

        [DataMember(Name = "User", EmitDefaultValue = false)]
        public string User { get; set; }

        [DataMember(Name = "AttachStdin", EmitDefaultValue = false)]
        public bool AttachStdin { get; set; }

        [DataMember(Name = "AttachStdout", EmitDefaultValue = false)]
        public bool AttachStdout { get; set; }

        [DataMember(Name = "AttachStderr", EmitDefaultValue = false)]
        public bool AttachStderr { get; set; }

        [DataMember(Name = "ExposedPorts", EmitDefaultValue = false)]
        public IDictionary<string, EmptyStruct> ExposedPorts { get; set; }

        [DataMember(Name = "Tty", EmitDefaultValue = false)]
        public bool Tty { get; set; }

        [DataMember(Name = "OpenStdin", EmitDefaultValue = false)]
        public bool OpenStdin { get; set; }

        [DataMember(Name = "StdinOnce", EmitDefaultValue = false)]
        public bool StdinOnce { get; set; }

        [DataMember(Name = "Env", EmitDefaultValue = false)]
        public IList<string> Env { get; set; }

        [DataMember(Name = "Cmd", EmitDefaultValue = false)]
        public IList<string> Cmd { get; set; }

        [DataMember(Name = "Healthcheck", EmitDefaultValue = false)]
        public HealthConfig Healthcheck { get; set; }

        [DataMember(Name = "ArgsEscaped", EmitDefaultValue = false)]
        public bool ArgsEscaped { get; set; }

        [DataMember(Name = "Image", EmitDefaultValue = false)]
        public string Image { get; set; }

        [DataMember(Name = "Volumes", EmitDefaultValue = false)]
        public IDictionary<string, EmptyStruct> Volumes { get; set; }

        [DataMember(Name = "WorkingDir", EmitDefaultValue = false)]
        public string WorkingDir { get; set; }

        [DataMember(Name = "Entrypoint", EmitDefaultValue = false)]
        public IList<string> Entrypoint { get; set; }

        [DataMember(Name = "NetworkDisabled", EmitDefaultValue = false)]
        public bool NetworkDisabled { get; set; }

        [DataMember(Name = "MacAddress", EmitDefaultValue = false)]
        public string MacAddress { get; set; }

        [DataMember(Name = "OnBuild", EmitDefaultValue = false)]
        public IList<string> OnBuild { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "StopSignal", EmitDefaultValue = false)]
        public string StopSignal { get; set; }

        [DataMember(Name = "StopTimeout", EmitDefaultValue = false)]
        [JsonConverter(typeof(TimeSpanSecondsConverter))]
        public TimeSpan? StopTimeout { get; set; }

        [DataMember(Name = "Shell", EmitDefaultValue = false)]
        public IList<string> Shell { get; set; }

        [DataMember(Name = "HostConfig", EmitDefaultValue = false)]
        public HostConfig HostConfig { get; set; }

        [DataMember(Name = "NetworkingConfig", EmitDefaultValue = false)]
        public NetworkingConfig NetworkingConfig { get; set; }
    }
}
